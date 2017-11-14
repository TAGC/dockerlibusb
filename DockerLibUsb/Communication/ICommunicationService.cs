using System;
using System.Threading;
using System.Threading.Tasks;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// A communication service that is responsible for handling communication between .NET applications and external
    /// devices using a particular protocol.
    /// </summary>
    public interface ICommunicationService : IDisposable
    {
        /// <summary>
        /// Occurs when a message is received by this <c>ICommunicationService</c>.
        /// </summary>
        event EventHandler<ReceivedMessageEventArgs> MessageReceived;

        /// <summary>
        /// Sends the provided message to the target.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort the transmission.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        Task SendMessage(GenericMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the <c>ICommunicationService</c>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to abort initialization.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        Task Start(CancellationToken cancellationToken = default);
    }
}