using System;
using System.Linq;
using PommaLabs.Thrower;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// A struct representation of a USB message.
    /// </summary>
    public struct UsbMessage : IEquatable<UsbMessage>
    {
        /// <summary>
        /// The 'raw' USB message data.
        /// </summary>
        /// <remarks>
        /// The data within instances of this struct is in a raw format and requires processing to convert it into the
        /// logical payload and identifier of a <see cref="GenericMessage" />.
        /// </remarks>
        public readonly byte[] Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbMessage" /> struct.
        /// </summary>
        /// <param name="data">The USB data.</param>
        public UsbMessage(byte[] data)
        {
            Raise.ArgumentNullException.IfIsNull(data, nameof(data));
            Raise.ArgumentException.If(data.Length > 64, nameof(data), "The USB data can be at most 64 bytes.");

            Length = data.Length;
            Data = new byte[64];
            data.CopyTo(Data, 0);
        }

        /// <summary>
        /// Gets the length of the USB data (in bytes).
        /// </summary>
        public int Length { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is UsbMessage))
            {
                return false;
            }

            return Equals((UsbMessage)obj);
        }

        /// <inheritdoc />
        public bool Equals(UsbMessage other) => Data.SequenceEqual(other.Data);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.Aggregate(17, (current, value) => (31 * current) + value.GetHashCode());
        }

        /// <inheritdoc />
        public override string ToString() => $"Data: {BitConverter.ToString(Data)}";

        /// <summary>
        /// Deconstructs this instance.
        /// </summary>
        /// <param name="data">The USB data.</param>
        /// <param name="length">The length of the USB data in bytes.</param>
        public void Deconstruct(out byte[] data, out int length)
        {
            data = Data;
            length = Length;
        }
    }
}