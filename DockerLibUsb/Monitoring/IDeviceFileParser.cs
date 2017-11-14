using System.IO.Abstractions;

namespace DockerLibUsb.Monitoring
{
    /// <summary>
    /// Represents a strategy for extracting USB system information from device files.
    /// </summary>
    public interface IDeviceFileParser
    {
        /// <summary>
        /// Tries to parse <paramref name="deviceFile" /> as a USB device interface.
        /// </summary>
        /// <param name="deviceFile">The device file.</param>
        /// <param name="vendorId">If parsing succeeds, this is set to the vendor ID of the USB device.</param>
        /// <param name="productId">If parsing succeeds, this is set to the product ID of the USB device.</param>
        /// <returns>
        /// <c>true</c> if the specified device file can be parsed as a USB device file; otherwise returns <c>false</c>.
        /// </returns>
        bool TryParseAsDeviceInterface(FileInfoBase deviceFile, out int vendorId, out int productId);
    }
}