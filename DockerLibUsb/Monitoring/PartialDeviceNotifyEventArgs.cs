using System;
using System.Reflection;
using LibUsbDotNet;
using LibUsbDotNet.DeviceNotify;
using LibUsbDotNet.DeviceNotify.Info;
using LibUsbDotNet.Main;

namespace DockerLibUsb.Monitoring
{
    /// <summary>
    /// Contains a limited subset of information related to a USB system event. Only information that is relevant
    /// to this application is provided by instances of this class.
    /// </summary>
    public class PartialDeviceNotifyEventArgs : DeviceNotifyEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialDeviceNotifyEventArgs" /> class.
        /// </summary>
        /// <param name="eventType">The type of the USB event.</param>
        /// <param name="vendorId">The vendor identifier of the USB device associated with the event.</param>
        /// <param name="productId">The product identifier of the USB device associated with the event.</param>
        public PartialDeviceNotifyEventArgs(EventType eventType, int vendorId, int productId)
        {
            var device = new InternalUsbDeviceNotifyInfo { IdVendor = vendorId, IdProduct = productId };
            var baseType = typeof(PartialDeviceNotifyEventArgs);
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            baseType.GetField("mDevice", bindingFlags).SetValue(this, device);
            baseType.GetField("mEventType", bindingFlags).SetValue(this, eventType);
        }

        private class InternalUsbDeviceNotifyInfo : IUsbDeviceNotifyInfo
        {
            public Guid ClassGuid => throw new NotSupportedException();

            public int IdProduct { get; set; }

            public int IdVendor { get; set; }

            public string Name => throw new NotSupportedException();

            public string SerialNumber => throw new NotSupportedException();

            public UsbSymbolicName SymbolicName => throw new NotSupportedException();

            public bool Open(out UsbDevice usbDevice) => throw new NotSupportedException();
        }
    }
}