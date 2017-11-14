using System;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// Contains and provides the received message when a message is received.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ReceivedMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedMessageEventArgs" /> class.
        /// </summary>
        /// <param name="message">The received message.</param>
        public ReceivedMessageEventArgs(GenericMessage message) => Message = message;

        /// <summary>
        /// Gets the received message.
        /// </summary>
        public GenericMessage Message { get; }
    }
}