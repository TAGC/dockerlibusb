using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// A message is a type of object that represents a message consisting of an identifier and payload.
    /// </summary>
    public struct GenericMessage : IEquatable<GenericMessage>
    {
        private readonly ReadOnlyCollection<byte> _payload;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage" /> struct.
        /// </summary>
        /// <param name="id">The numeric identifier of the message.</param>
        /// <param name="payload">
        /// The payload of the message. This may be <c>null</c> or empty. If <c>null</c>, the message will have an empty payload.
        /// </param>
        [JsonConstructor]
        public GenericMessage(int id, IEnumerable<byte> payload = null)
        {
            Id = id;
            _payload = new ReadOnlyCollection<byte>(payload?.ToList() ?? new List<byte>());
        }

        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        /// <remarks>This will typically be used in interpreting its payload.</remarks>
        public int Id { get; }

        /// <summary>
        /// Gets the payload of the message, which is an arbitrarily-long read-only collection of bytes.
        /// </summary>
        public ReadOnlyCollection<byte> Payload => _payload ?? new ReadOnlyCollection<byte>(new byte[] { });

        /// <summary>
        /// Gets the size of the payload in bytes.
        /// </summary>
        public int Size => Payload.Count;

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="messageA">The first message to compare.</param>
        /// <param name="messageB">The second message to compare.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(GenericMessage messageA, GenericMessage messageB) => messageA.Equals(messageB);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="messageA">The first message to compare.</param>
        /// <param name="messageB">The second message to compare.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(GenericMessage messageA, GenericMessage messageB) => !(messageA == messageB);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is GenericMessage))
            {
                return false;
            }

            return Equals((GenericMessage)obj);
        }

        /// <inheritdoc />
        public bool Equals(GenericMessage other) => (Id == other.Id) && Payload.SequenceEqual(other.Payload);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var result = 17;

            result = (31 * result) + Id.GetHashCode();
            result = Payload.Aggregate(result, (current, value) => (31 * current) + value.GetHashCode());

            return result;
        }

        /// <inheritdoc />
        public override string ToString() => $"Id: {Id}, Payload: {string.Join(", ", Payload)}";

        /// <summary>
        /// Deconstructs this instance.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="payload">The payload.</param>
        public void Deconstruct(out int id, out ReadOnlyCollection<byte> payload)
        {
            id = Id;
            payload = Payload;
        }
    }
}