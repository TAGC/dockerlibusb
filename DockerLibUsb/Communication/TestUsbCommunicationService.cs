using System;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using PommaLabs.Thrower;
using Serilog;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// A type of <see cref="ICommunicationService" /> that facilitates communication between .NET applications and USB
    /// devices and can be run on multiple operating systems.
    /// </summary>
    /// <remarks>
    /// For this experiment, this class has been modified to only test connecting and disconnecting from the device -
    /// actual sending/receiving of messages is not supported.
    /// </remarks>
    public sealed class TestUsbCommunicationService : ICommunicationService
    {
        private const string DeviceName = "TestDevice";

        private readonly ILogger _logger;
        private readonly int _productId;
        private readonly int _vendorId;

        private UsbDevice _device;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestUsbCommunicationService" /> class.
        /// </summary>
        /// <param name="vendorId">The VID of the test USB device.</param>
        /// <param name="productId">The PID of the test USB device.</param>
        /// <param name="logger">The logger.</param>
        public TestUsbCommunicationService(
            int vendorId,
            int productId,
            ILogger logger)
        {
            Raise.ArgumentNullException.IfIsNull(logger, nameof(logger));

            _logger = logger.ForContext<TestUsbCommunicationService>();
            _vendorId = vendorId;
            _productId = productId;
        }

        /// <inheritdoc />
        public event EventHandler<ReceivedMessageEventArgs> MessageReceived;

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <inheritdoc />
        public Task SendMessage(GenericMessage message, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Actual communication with test device not necessary for experiment.");

        /// <inheritdoc />
        public async Task Start(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            var deviceFinder = new UsbDeviceFinder(_vendorId, _productId);
            _device = await Task.Run(() => UsbDevice.OpenUsbDevice(deviceFinder), cancellationToken);

            // If using a "whole" usb device (libusb-win32, linux libusb-1.0), the configuration and interface must be selected.
            switch (_device)
            {
                case null:
                    _logger.Error("Cannot find {Device}", DeviceName);
                    throw new InvalidOperationException($"Cannot find {DeviceName}");
                case IUsbDevice wholeUsbDevice:
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                    break;
            }

            _logger.Information("Successfully initiated communication with {Device}", DeviceName);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if ((_device != null) && _device.IsOpen)
                {
                    // If using a "whole" usb device (libusb-win32, linux libusb-1.0), the interface should be released.
                    if (_device is IUsbDevice wholeUsbDevice)
                    {
                        wholeUsbDevice.ReleaseInterface(0);

                        // Resetting the USB device may resolve issues on Linux.
                        _logger.Information("Resetting {Device} as USB device", DeviceName);
                        wholeUsbDevice.ResetDevice();
                    }

                    _device.Close();
                    _device = null;
                    _logger.Information("Terminated communication with {Device}", DeviceName);
                }
            }

            _disposed = true;
        }
    }
}