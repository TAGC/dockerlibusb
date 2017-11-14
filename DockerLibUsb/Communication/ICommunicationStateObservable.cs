using System;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// Represents a service that can be observed for changes in communication state.
    /// </summary>
    public interface ICommunicationStateObservable
    {
        /// <summary>
        /// Occurs when the state of communication with the device changes.
        /// </summary>
        event EventHandler<CommunicationStateChangedEventArgs> CommunicationStateChanged;

        /// <summary>
        /// Gets the current state of communication with the device.
        /// </summary>
        CommunicationState CommunicationState { get; }
    }
}