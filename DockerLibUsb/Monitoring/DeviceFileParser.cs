using System.IO.Abstractions;
using System.Runtime.InteropServices;
using LibUsbDotNet.Descriptors;

namespace DockerLibUsb.Monitoring
{
    /// <summary>
    /// Represents the standard strategy for parsing device files on modern Linux systems.
    /// </summary>
    internal sealed class DeviceFileParser : IDeviceFileParser
    {
        /// <summary>
        /// Gets an instance of this strategy. Instances of this strategy are stateless so are inherently safe
        /// to share and reuse.
        /// </summary>
        public static DeviceFileParser Instance { get; } = new DeviceFileParser();

        /// <inheritdoc />
        public bool TryParseAsDeviceInterface(FileInfoBase deviceFile, out int vendorId, out int productId)
        {
            vendorId = 0;
            productId = 0;

            using (var stream = deviceFile.OpenRead())
            {
                var descriptorBytes = new byte[UsbDeviceDescriptorBase.Size];
                var bytesRead = stream.Read(descriptorBytes, 0, UsbDeviceDescriptorBase.Size);

                if (bytesRead != UsbDeviceDescriptorBase.Size)
                {
                    return false;
                }

                var deviceDescriptor = new UsbDeviceDescriptor();
                var gcFileDescriptor = GCHandle.Alloc(deviceDescriptor, GCHandleType.Pinned);
                var descriptorSize = Marshal.SizeOf(deviceDescriptor);

                Marshal.Copy(descriptorBytes, 0, gcFileDescriptor.AddrOfPinnedObject(), descriptorSize);
                vendorId = deviceDescriptor.VendorID;
                productId = deviceDescriptor.ProductID;
                return true;
            }
        }
    }
}