namespace DockerLibUsb.Communication
{
    /// <summary>
    /// Represents a type of <see cref="ICommunicationService" /> that attempts to be self-healing by allowing subclasses
    /// to attempt restarting communication through a delegate communication service factory.
    /// </summary>
    public interface IRestartableCommunicationService : ICommunicationService, ICommunicationStateObservable
    {
    }
}