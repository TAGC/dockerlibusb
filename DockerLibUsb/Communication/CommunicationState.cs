namespace DockerLibUsb.Communication
{
    /// <summary>
    /// Represents the state of communication between a(n) <see cref="ICommunicationService" /> and a device.
    /// </summary>
    public enum CommunicationState
    {
        /// <summary>
        /// Indicates that communication is not currently established with the device. This is a stable state and the
        /// default communication state.
        /// </summary>
        Down = 1,

        /// <summary>
        /// Indicates that communication is currently established with the device. This is a stable state.
        /// </summary>
        Up,

        /// <summary>
        /// Indicates that communication with the device is being started. This is a transitional state.
        /// </summary>
        Starting,

        /// <summary>
        /// Indicates that communication with the device is being restarted. This is a transitional state.
        /// </summary>
        Restarting,

        /// <summary>
        /// Indicates that communication with the device is being terminated. This is a transitional state.
        /// </summary>
        Terminating
    }
}