using System;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Linux;
using PommaLabs.Thrower;
using Serilog;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// A type of <see cref="IRestartableCommunicationService" /> that attempts to establish communication with a USB
    /// device on initialization and re-attempt communication if the USB device is reconnected to the host.
    /// </summary>
    public sealed class RestartableUsbCommunicationService : RestartableCommunicationService
    {
        private readonly ILogger _logger;
        private readonly int _productId;
        private readonly IDeviceNotifier _usbDeviceNotifier;
        private readonly int _vendorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartableUsbCommunicationService" /> class.
        /// </summary>
        /// <param name="vendorId">The VID of the USB device to manage.</param>
        /// <param name="productId">The PID of the USB device to manage.</param>
        /// <param name="factory">A factory to produce instances of the delegate USB communication service.</param>
        /// <param name="usbDeviceNotifier">A notifier to use for subscribing to USB system events.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">usbDeviceNotifier</exception>
        public RestartableUsbCommunicationService(
            int vendorId,
            int productId,
            DelegateServiceFactory factory,
            IDeviceNotifier usbDeviceNotifier,
            ILogger logger)
            : base(() => factory(vendorId, productId), logger)
        {
            Raise.ArgumentNullException.IfIsNull(logger, nameof(logger));

            _productId = productId;
            _vendorId = vendorId;
            _logger = logger.ForContext<RestartableUsbCommunicationService>();
            _usbDeviceNotifier = usbDeviceNotifier ?? throw new ArgumentNullException(nameof(usbDeviceNotifier));
        }

        /// <summary>
        /// Represents a factory method to create <c>ICommunicationService</c> instances that handle communication
        /// with a USB device specified by <paramref name="vendorId" /> and <paramref name="productId" />.
        /// </summary>
        /// <param name="vendorId">The VID of the USB device.</param>
        /// <param name="productId">The PID of the USB device.</param>
        /// <returns>A USB communication service.</returns>
        public delegate ICommunicationService DelegateServiceFactory(int vendorId, int productId);

        /// <inheritdoc />
        public override Task Start(CancellationToken cancellationToken = default)
        {
            _usbDeviceNotifier.Enabled = true;
            _usbDeviceNotifier.OnDeviceNotify += OnUsbDeviceEvent;

            if (_usbDeviceNotifier is LinuxDeviceNotifier linuxDeviceNotifier)
            {
                _logger.Debug("Linux device notifier mode set to {DeviceNotifierMode}", linuxDeviceNotifier.Mode);
            }

            return base.Start(cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Debug("Disposing restartable USB communication service");
                _usbDeviceNotifier.Enabled = false;
                _usbDeviceNotifier.OnDeviceNotify -= OnUsbDeviceEvent;
            }

            base.Dispose(disposing);
        }

        private void OnUsbDeviceEvent(object sender, DeviceNotifyEventArgs e)
        {
            if ((e.Device.IdVendor != _vendorId) || (e.Device.IdProduct != _productId))
            {
                return;
            }

            _logger.Debug("USB event concerning device occurred");

            switch (e.EventType)
            {
                case EventType.DeviceRemoveComplete when CommunicationState != CommunicationState.Down:
                    TerminateCommunication();
                    break;

                case EventType.DeviceArrival when CommunicationState == CommunicationState.Down:
                    RepeatedlyTryRestartCommunication().GetAwaiter().GetResult();
                    break;
            }
        }

        private async Task RepeatedlyTryRestartCommunication()
        {
            const int backOffFactor = 2;

            var currentBackOff = 250;
            var remainingAttempts = 4;

            while (remainingAttempts > 0)
            {
                if (await TryRestartCommunication())
                {
                    return;
                }

                await Task.Delay(currentBackOff);
                currentBackOff *= backOffFactor;
                --remainingAttempts;
            }
        }
    }
}