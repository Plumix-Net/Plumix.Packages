// Dart parity source (reference): pub.dev/packages/bloc (approximate)

namespace Plumix.Bloc;

/// <summary>
/// An event-driven <see cref="Cubit{TState}"/>: events added via <see cref="Add"/> are dispatched
/// to handlers registered with <see cref="On{THandledEvent}"/>, which emit new states.
///
/// Equivalent to bloc's <c>Bloc&lt;Event, State&gt;</c>.
/// </summary>
/// <typeparam name="TEvent">The base type of events this bloc handles.</typeparam>
/// <typeparam name="TState">The type of the managed state.</typeparam>
public class Bloc<TEvent, TState> : Cubit<TState>
{
    private readonly object _registrationsGate = new();
    private readonly List<IEventRegistration> _registrations = [];
    private readonly CancellationTokenSource _shutdown = new();

    /// <summary>Creates the bloc with its initial state.</summary>
    public Bloc(TState initialState) : base(initialState)
    {
    }

    /// <summary>
    /// Adds an event, dispatching it to every matching handler registered with
    /// <see cref="On{THandledEvent}"/>. Handlers run asynchronously.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The bloc is closed.</exception>
    public void Add(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ThrowIfClosed();

        Bloc.NotifyObserver(observer => observer.OnEvent(this, @event!));

        IEventRegistration[] registrations;
        lock (_registrationsGate)
        {
            registrations = _registrations.ToArray();
        }

        foreach (var registration in registrations)
        {
            if (registration.CanHandle(@event))
            {
                registration.Dispatch(@event);
            }
        }
    }

    /// <summary>
    /// Registers a handler for events of type <typeparamref name="THandledEvent"/>.
    /// Call from the constructor, once per event type.
    /// </summary>
    /// <param name="handler">Processes the event and emits resulting states.</param>
    /// <param name="transformer">
    /// Controls concurrency for this handler; defaults to
    /// <see cref="EventTransformers.Sequential{TEvent}"/>.
    /// </param>
    protected void On<THandledEvent>(
        BlocEventHandler<THandledEvent, TState> handler,
        EventTransformer<THandledEvent>? transformer = null)
        where THandledEvent : TEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        transformer ??= EventTransformers.Sequential<THandledEvent>();

        lock (_registrationsGate)
        {
            ThrowIfClosed();
            _registrations.Add(new EventRegistration<THandledEvent>(this, handler, transformer, _shutdown.Token));
        }
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (IsClosed)
        {
            return;
        }

        _shutdown.Cancel();

        IEventRegistration[] registrations;
        lock (_registrationsGate)
        {
            registrations = _registrations.ToArray();
            _registrations.Clear();
        }

        foreach (var registration in registrations)
        {
            registration.Dispose();
        }

        _shutdown.Dispose();
        base.Close();
    }

    private void EmitFromHandler(TEvent @event, TState nextState)
    {
        if (EqualityComparer<TState>.Default.Equals(State, nextState))
        {
            return;
        }

        var transition = new Transition<TEvent, TState>(State, @event, nextState);
        OnTransition(transition);
        Emit(nextState);
    }

    private void ReportHandlerError(TEvent @event, Exception error)
    {
        var wrapped = error is BlocUnhandledErrorException
            ? error
            : new BlocUnhandledErrorException(GetType(), @event, error);
        ReportError(wrapped);
    }

    /// <summary>
    /// Called on every event-driven state transition before the state is emitted.
    /// The base implementation forwards the transition to <see cref="Bloc.Observer"/>.
    /// </summary>
    protected virtual void OnTransition(Transition<TEvent, TState> transition)
    {
        Bloc.NotifyObserver(observer => observer.OnTransition(this, transition));
    }

    private interface IEventRegistration : IDisposable
    {
        bool CanHandle(TEvent @event);
        void Dispatch(TEvent @event);
    }

    private sealed class EventRegistration<THandledEvent>(
        Bloc<TEvent, TState> owner,
        BlocEventHandler<THandledEvent, TState> handler,
        EventTransformer<THandledEvent> transformer,
        CancellationToken shutdownToken) : IEventRegistration
        where THandledEvent : TEvent
    {
        private readonly object _gate = new();
        private readonly SemaphoreSlim _sequential = new(1, 1);
        private CancellationTokenSource? _restartableToken;
        private int _runningHandlers;
        private bool _disposed;

        public bool CanHandle(TEvent @event) => @event is THandledEvent;

        public void Dispatch(TEvent @event)
        {
            if (_disposed || shutdownToken.IsCancellationRequested || @event is not THandledEvent typedEvent)
            {
                return;
            }

            var mode = transformer(typedEvent);
            switch (mode)
            {
                case EventConcurrency.Sequential:
                    StartTask(typedEvent, sequential: true, CancellationToken.None);
                    break;
                case EventConcurrency.Concurrent:
                    StartTask(typedEvent, sequential: false, CancellationToken.None);
                    break;
                case EventConcurrency.Restartable:
                    StartRestartable(typedEvent);
                    break;
                case EventConcurrency.Droppable:
                    StartDroppable(typedEvent);
                    break;
                default:
                    StartTask(typedEvent, sequential: true, CancellationToken.None);
                    break;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _restartableToken?.Cancel();
                _restartableToken?.Dispose();
                _restartableToken = null;
            }
        }

        private void StartRestartable(THandledEvent @event)
        {
            CancellationToken token;

            lock (_gate)
            {
                if (_disposed || shutdownToken.IsCancellationRequested)
                {
                    return;
                }

                _restartableToken?.Cancel();
                _restartableToken?.Dispose();
                _restartableToken = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
                token = _restartableToken.Token;
            }

            StartTask(@event, sequential: false, token);
        }

        private void StartDroppable(THandledEvent @event)
        {
            lock (_gate)
            {
                if (_disposed || shutdownToken.IsCancellationRequested || _runningHandlers > 0)
                {
                    return;
                }

                StartTask(@event, sequential: false, CancellationToken.None);
            }
        }

        private void StartTask(THandledEvent @event, bool sequential, CancellationToken token)
        {
            if (_disposed || shutdownToken.IsCancellationRequested)
            {
                return;
            }

            Interlocked.Increment(ref _runningHandlers);
            var task = sequential
                ? RunSequentialAsync(@event, token)
                : RunConcurrentAsync(@event, token);
            _ = task.ContinueWith(
                static (completedTask, state) =>
                {
                    _ = Interlocked.Decrement(ref ((EventRegistration<THandledEvent>)state!)._runningHandlers);
                },
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private async Task RunSequentialAsync(THandledEvent @event, CancellationToken token)
        {
            try
            {
                await _sequential.WaitAsync(shutdownToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await ExecuteHandlerAsync(@event, token).ConfigureAwait(false);
            }
            finally
            {
                _sequential.Release();
            }
        }

        private Task RunConcurrentAsync(THandledEvent @event, CancellationToken token)
            => ExecuteHandlerAsync(@event, token);

        private async Task ExecuteHandlerAsync(THandledEvent @event, CancellationToken token)
        {
            CancellationTokenSource? linkedTokenSource = null;
            var effectiveToken = shutdownToken;
            if (token.CanBeCanceled)
            {
                linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, token);
                effectiveToken = linkedTokenSource.Token;
            }

            var emitter = new EventEmitter(owner, @event, effectiveToken);
            try
            {
                await handler(@event, emitter, effectiveToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                owner.ReportHandlerError(@event, error);
            }
            finally
            {
                emitter.Complete();
                linkedTokenSource?.Dispose();
            }
        }
    }

    private sealed class EventEmitter(
        Bloc<TEvent, TState> owner,
        TEvent @event,
        CancellationToken cancellationToken) : IEmitter<TState>
    {
        private int _done;

        public bool IsDone
            => cancellationToken.IsCancellationRequested || Volatile.Read(ref _done) == 1;

        public void Emit(TState state)
        {
            if (IsDone)
            {
                return;
            }

            owner.EmitFromHandler(@event, state);
        }

        public void Complete()
        {
            _ = Interlocked.Exchange(ref _done, 1);
        }
    }
}
