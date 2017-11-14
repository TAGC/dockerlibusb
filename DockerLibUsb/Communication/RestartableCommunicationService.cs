using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using PommaLabs.Thrower;
using Serilog;

namespace DockerLibUsb.Communication
{
    /// <inheritdoc />
    public abstract class RestartableCommunicationService : IRestartableCommunicationService
    {
        private readonly Func<ICommunicationService> _factory;
        private readonly ILogger _logger;
        private readonly AsyncLock _mutex;

        private volatile CommunicationState _communicationState;
        private volatile ICommunicationService _currentDelegate;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartableCommunicationService" /> class.
        /// </summary>
        /// <param name="factory">A factory to produce instances of the delegate communication service.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        protected RestartableCommunicationService(
            Func<ICommunicationService> factory,
            ILogger logger)
        {
            Raise.ArgumentNullException.IfIsNull(logger, nameof(logger));

            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger.ForContext<RestartableCommunicationService>();
            _mutex = new AsyncLock();
            _communicationState = CommunicationState.Down;
        }

        /// <inheritdoc />
        public event EventHandler<CommunicationStateChangedEventArgs> CommunicationStateChanged;

        /// <inheritdoc />
        public event EventHandler<ReceivedMessageEventArgs> MessageReceived;

        /// <inheritdoc />
        public CommunicationState CommunicationState
        {
            get => _communicationState;
            private set
            {
                if (_communicationState == value)
                {
                    return;
                }

                _communicationState = value;
                CommunicationStateChanged?.Invoke(this, new CommunicationStateChangedEventArgs(value));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Callers should be aware that deadlocks may occur if calling this method and waiting synchronously for it to
        /// complete, i.e. <c>comms.SendMessage(...).Wait()</c>.
        /// <para></para>
        /// This can happen if this communication service delegates transmission to another communication service that
        /// raises an event within its own implementation of <c>SendMessage</c>, and if this event has registered
        /// handlers that attempt to perform other operations using this communication service.
        /// <para></para>
        /// This situation should be avoidable by awaiting completion of calls to this method asynchronously i.e.
        /// <c>await comms.SendMessage(...)</c>.
        /// </remarks>
        public async Task SendMessage(GenericMessage message, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            // Potential deadlock occurs if "_mutex.Lock(cancellationToken)", as this blocks thread.
            // Locking asynchronously avoids this.
            using (await _mutex.LockAsync(cancellationToken))
            {
                if (CommunicationState != CommunicationState.Up)
                {
                    throw new InvalidOperationException("Message cannot be sent as communication is not established.");
                }

                try
                {
                    await _currentDelegate.SendMessage(message, cancellationToken);
                }
                catch (Exception)
                {
                    TerminateCommunication();
                }
            }
        }

        /// <inheritdoc />
        public virtual Task Start(CancellationToken cancellationToken = default) =>
            TryRestartCommunication(true, cancellationToken);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            TerminateCommunication();
            _disposed = true;
        }

        /// <summary>
        /// Terminates communication with the device. If communication is already down, this method returns immediately.
        /// </summary>
        /// <remarks>
        /// Subclasses can invoke this method to signal communication failure.
        /// </remarks>
        protected void TerminateCommunication()
        {
            if (CommunicationState == CommunicationState.Down)
            {
                return;
            }

            CommunicationState = CommunicationState.Terminating;
            _currentDelegate.MessageReceived -= MessageReceived;
            _currentDelegate.Dispose();
            _currentDelegate = null;
            CommunicationState = CommunicationState.Down;
        }

        /// <summary>
        /// Attempts to restart communication by creating and initializing a new instance of the delegate
        /// <c>ICommunicationService</c>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to abort the attempt to restart communication.</param>
        /// <returns><c>true</c> if communication was restarted successfully; otherwise <c>false</c>.</returns>
        protected Task<bool> TryRestartCommunication(CancellationToken cancellationToken = default) =>
            TryRestartCommunication(false, cancellationToken);

        private void AssertCommunicationStateIsStable()
        {
            var stableStates = new[] { CommunicationState.Up, CommunicationState.Down };
            Debug.Assert(stableStates.Contains(CommunicationState), "Communication state should be stable");
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private async Task<bool> TryRestartCommunication(bool initialStart, CancellationToken cancellationToken)
        {
            CheckDisposed();

            _logger.Debug("Attempting to start/restart communication.");

            using (await _mutex.LockAsync())
            {
                AssertCommunicationStateIsStable();

                if (initialStart)
                {
                    CommunicationState = CommunicationState.Starting;
                }
                else
                {
                    TerminateCommunication();
                    CommunicationState = CommunicationState.Restarting;
                }

                try
                {
                    _currentDelegate = _factory();
                    await _currentDelegate.Start(cancellationToken);
                    _currentDelegate.MessageReceived += MessageReceived;
                    CommunicationState = CommunicationState.Up;
                    return true;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to start/restart communication.");
                    TerminateCommunication();
                    return false;
                }
                finally
                {
                    AssertCommunicationStateIsStable();
                }
            }
        }
    }
}