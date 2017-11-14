using System;

namespace DockerLibUsb.Communication
{
    /// <summary>
    /// Represents a change in the state of communication between a(n) <see cref="ICommunicationService" /> and a device.
    /// </summary>
    public class CommunicationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationStateChangedEventArgs" /> class.
        /// </summary>
        /// <param name="newState">The new communication state.</param>
        public CommunicationStateChangedEventArgs(CommunicationState newState)
        {
            NewState = newState;
        }

        /// <summary>
        /// Gets the new communication state.
        /// </summary>
        public CommunicationState NewState { get; }
    }
}