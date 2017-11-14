using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using LibUsbDotNet.DeviceNotify;
using PommaLabs.Thrower;
using Serilog;

namespace DockerLibUsb.Monitoring
{
    /// <summary>
    /// A type of USB fileSystem event notifier that monitors the <c>/dev/bus/usb</c> directory (or its equivalent) for
    /// USB device arrivals and removals within a Linux environment.
    /// </summary>
    public sealed class DevMonitor : IDeviceNotifier
    {
        private const string DefaultDevDirectoryPath = "/dev/bus/usb";

        private readonly IDictionary<string, (int vendorId, int productId)> _connectedDevices;
        private readonly IDeviceFileParser _deviceFileParser;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly FileSystemWatcherBase _watcher;

        private bool _enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevMonitor" /> class using a standard strategy for parsing
        /// device files.
        /// </summary>
        /// <param name="fileSystem">An abstraction of the file system the application is running on.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="devDirectoryPath">The directory to monitor.</param>
        public DevMonitor(
            IWatchableFileSystem fileSystem,
            ILogger logger,
            string devDirectoryPath = DefaultDevDirectoryPath)
            : this(fileSystem, DeviceFileParser.Instance, logger, devDirectoryPath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DevMonitor" /> class.
        /// </summary>
        /// <param name="fileSystem">An abstraction of the file system the application is running on.</param>
        /// <param name="deviceFileParser">
        /// The strategy for parsing device files created under the <c>/dev</c> directory as USB interfaces.
        /// </param>
        /// <param name="logger">The logger.</param>
        /// <param name="devDirectoryPath">The directory to monitor.</param>
        public DevMonitor(
            IWatchableFileSystem fileSystem,
            IDeviceFileParser deviceFileParser,
            ILogger logger,
            string devDirectoryPath = DefaultDevDirectoryPath)
        {
            Raise.ArgumentNullException.IfIsNull(fileSystem, nameof(fileSystem));
            Raise.ArgumentNullException.IfIsNull(deviceFileParser, nameof(deviceFileParser));
            Raise.ArgumentNullException.IfIsNull(logger, nameof(logger));
            Raise.ArgumentNullException.IfIsNull(devDirectoryPath, nameof(devDirectoryPath));
            Raise.ArgumentException.IfNot(fileSystem.Directory.Exists(devDirectoryPath), nameof(devDirectoryPath));

            _fileSystem = fileSystem;
            _deviceFileParser = deviceFileParser;
            _logger = logger.ForContext<DevMonitor>();
            _connectedDevices = new Dictionary<string, (int, int)>();
            _watcher = fileSystem.CreateWatcher();

            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            _watcher.Path = devDirectoryPath;
        }

        /// <inheritdoc />
        public event EventHandler<DeviceNotifyEventArgs> OnDeviceNotify;

        /// <inheritdoc />
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value)
                {
                    StartMonitoring();
                }
                else
                {
                    StopMonitoring();
                }

                _enabled = value;
            }
        }

        private void AddConnectedDevice(string path, int vendorId, int productId)
        {
            var deviceConnectedAtPath = _connectedDevices.ContainsKey(path);
            Debug.Assert(!deviceConnectedAtPath, "A device should not already be connected at this path");

            _connectedDevices[path] = (vendorId, productId);
        }

        private string GetCanonicalPath(string path) => new Uri(_fileSystem.Path.GetFullPath(path)).LocalPath;

        private void OnDeviceFileCreated(object sender, FileSystemEventArgs e)
        {
            var path = GetCanonicalPath(e.FullPath);
            var deviceFile = _fileSystem.FileInfo.FromFileName(path);

            if (!_deviceFileParser.TryParseAsDeviceInterface(deviceFile, out var vendorId, out var productId))
            {
                return;
            }

            _logger.Verbose("USB device connected (VID: {VendorId}, PID: {ProductId})", vendorId, productId);
            AddConnectedDevice(path, vendorId, productId);

            var eventArgs = new PartialDeviceNotifyEventArgs(EventType.DeviceArrival, vendorId, productId);
            PublishDeviceNotification(eventArgs);
        }

        private void OnDeviceFileDeleted(object sender, FileSystemEventArgs e)
        {
            var path = GetCanonicalPath(e.FullPath);

            if (!_connectedDevices.ContainsKey(path))
            {
                return;
            }

            var (vendorId, productId) = _connectedDevices[path];
            _connectedDevices.Remove(path);
            _logger.Verbose("USB device removed (VID: {VendorId}, PID: {ProductId})", vendorId, productId);

            var eventArgs = new PartialDeviceNotifyEventArgs(EventType.DeviceRemoveComplete, vendorId, productId);
            PublishDeviceNotification(eventArgs);
        }

        private void PopulateConnectedDevices()
        {
            _logger.Debug("Populating connected USB devices under {DevDirectorPath}", _watcher.Path);
            _connectedDevices.Clear();

            foreach (var path in _fileSystem.Directory.EnumerateFiles(_watcher.Path, "*", SearchOption.AllDirectories))
            {
                var deviceFile = _fileSystem.FileInfo.FromFileName(path);

                if (!_deviceFileParser.TryParseAsDeviceInterface(deviceFile, out var vendorId, out var productId))
                {
                    continue;
                }

                _logger.Verbose("Discovered USB device (VID: {VendorId}, PID: {ProductId})", vendorId, productId);
                AddConnectedDevice(GetCanonicalPath(path), vendorId, productId);
            }
        }

        private void PublishDeviceNotification(DeviceNotifyEventArgs e)
        {
            OnDeviceNotify?.Invoke(this, e);
        }

        private void StartMonitoring()
        {
            _watcher.Created += OnDeviceFileCreated;
            _watcher.Deleted += OnDeviceFileDeleted;

            PopulateConnectedDevices();
        }

        private void StopMonitoring()
        {
            _watcher.Created -= OnDeviceFileCreated;
            _watcher.Deleted -= OnDeviceFileDeleted;
        }
    }
}