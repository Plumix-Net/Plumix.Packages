// Dart parity source (reference): pub.dev/packages/bloc and pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;

namespace Plumix.Bloc;

/// <summary>
/// Non-generic base contract shared by all blocs and cubits.
/// Allows widgets such as <see cref="BlocProvider{TBloc}"/> to manage instances
/// without knowing their state type.
/// </summary>
public interface IBlocBase : IListenable, IDisposable
{
    /// <summary>The current state as an untyped object.</summary>
    object? StateObject { get; }

    /// <summary>Whether <see cref="Close"/> has been called.</summary>
    bool IsClosed { get; }

    /// <summary>Closes the bloc: completes the state stream and releases listeners.</summary>
    void Close();
}

/// <summary>
/// Typed contract implemented by <see cref="Cubit{TState}"/> and <see cref="Bloc{TEvent, TState}"/>.
/// Equivalent to bloc's <c>BlocBase&lt;State&gt;</c>.
/// </summary>
/// <typeparam name="TState">The type of the managed state.</typeparam>
public interface IBlocBase<out TState> : IBlocBase
{
    /// <summary>The current state.</summary>
    TState State { get; }

    /// <summary>A stream of state changes. Does not replay the current state on subscription.</summary>
    IObservable<TState> Stream { get; }
}

/// <summary>
/// A state change of a <see cref="Cubit{TState}"/>, reported to <see cref="IBlocObserver.OnChange"/>.
/// Equivalent to bloc's <c>Change&lt;State&gt;</c>.
/// </summary>
/// <typeparam name="TState">The type of the managed state.</typeparam>
/// <param name="CurrentState">The state before the change.</param>
/// <param name="NextState">The state after the change.</param>
public readonly record struct Change<TState>(TState CurrentState, TState NextState);

/// <summary>
/// A state change of a <see cref="Bloc{TEvent, TState}"/> together with the event that caused it,
/// reported to <see cref="IBlocObserver.OnTransition"/>.
/// Equivalent to bloc's <c>Transition&lt;Event, State&gt;</c>.
/// </summary>
/// <typeparam name="TEvent">The type of the event.</typeparam>
/// <typeparam name="TState">The type of the managed state.</typeparam>
/// <param name="CurrentState">The state before the transition.</param>
/// <param name="Event">The event that triggered the transition.</param>
/// <param name="NextState">The state after the transition.</param>
public readonly record struct Transition<TEvent, TState>(TState CurrentState, TEvent Event, TState NextState);

/// <summary>
/// Emits new states from within a <see cref="BlocEventHandler{TEvent, TState}"/>.
/// Equivalent to bloc's <c>Emitter&lt;State&gt;</c>.
/// </summary>
/// <typeparam name="TState">The type of the managed state.</typeparam>
public interface IEmitter<in TState>
{
    /// <summary>
    /// Whether the handler has completed or been cancelled; emitting after this point is a no-op.
    /// </summary>
    bool IsDone { get; }

    /// <summary>Emits a new state.</summary>
    void Emit(TState state);
}

/// <summary>
/// Handles an event dispatched to a <see cref="Bloc{TEvent, TState}"/> and emits resulting states.
/// Equivalent to bloc's <c>EventHandler&lt;Event, State&gt;</c>.
/// </summary>
/// <typeparam name="TEvent">The type of the handled event.</typeparam>
/// <typeparam name="TState">The type of the managed state.</typeparam>
public delegate ValueTask BlocEventHandler<in TEvent, TState>(
    TEvent @event,
    IEmitter<TState> emitter,
    CancellationToken cancellationToken);

/// <summary>
/// How concurrently arriving events are processed by an event handler.
/// </summary>
public enum EventConcurrency
{
    /// <summary>Events are processed one at a time, in order.</summary>
    Sequential,

    /// <summary>Events are processed in parallel.</summary>
    Concurrent,

    /// <summary>A new event cancels the in-flight handler.</summary>
    Restartable,

    /// <summary>New events are ignored while a handler is running.</summary>
    Droppable
}

/// <summary>
/// Chooses the <see cref="EventConcurrency"/> for a given event.
/// Counterpart of bloc_concurrency's event transformers.
/// </summary>
/// <typeparam name="TEvent">The type of the handled event.</typeparam>
public delegate EventConcurrency EventTransformer<in TEvent>(TEvent @event);

/// <summary>
/// Built-in event transformers, mirroring the bloc_concurrency package.
/// </summary>
public static class EventTransformers
{
    /// <summary>Processes events one at a time, in order (the default).</summary>
    public static EventTransformer<TEvent> Sequential<TEvent>() => static _ => EventConcurrency.Sequential;

    /// <summary>Processes events in parallel.</summary>
    public static EventTransformer<TEvent> Concurrent<TEvent>() => static _ => EventConcurrency.Concurrent;

    /// <summary>Cancels the in-flight handler when a new event arrives.</summary>
    public static EventTransformer<TEvent> Restartable<TEvent>() => static _ => EventConcurrency.Restartable;

    /// <summary>Ignores new events while a handler is running.</summary>
    public static EventTransformer<TEvent> Droppable<TEvent>() => static _ => EventConcurrency.Droppable;
}

/// <summary>
/// Observes lifecycle, events, changes, transitions and errors of every bloc and cubit.
/// Install via <see cref="Bloc.Observer"/>. Equivalent to bloc's <c>BlocObserver</c>.
/// </summary>
public interface IBlocObserver
{
    /// <summary>Called when a bloc or cubit is instantiated.</summary>
    void OnCreate(object bloc);

    /// <summary>Called when an event is added to a bloc.</summary>
    void OnEvent(object bloc, object @event);

    /// <summary>Called on every state change (see <see cref="Change{TState}"/>).</summary>
    void OnChange(object bloc, object change);

    /// <summary>Called on every event-driven state transition (see <see cref="Transition{TEvent, TState}"/>).</summary>
    void OnTransition(object bloc, object transition);

    /// <summary>Called when a bloc reports an unhandled error.</summary>
    void OnError(object bloc, Exception error);

    /// <summary>Called when a bloc or cubit is closed.</summary>
    void OnClose(object bloc);
}

/// <summary>
/// No-op base class for <see cref="IBlocObserver"/> implementations;
/// override only the hooks you need.
/// </summary>
public abstract class BlocObserver : IBlocObserver
{
    /// <inheritdoc />
    public virtual void OnCreate(object bloc)
    {
    }

    /// <inheritdoc />
    public virtual void OnEvent(object bloc, object @event)
    {
    }

    /// <inheritdoc />
    public virtual void OnChange(object bloc, object change)
    {
    }

    /// <inheritdoc />
    public virtual void OnTransition(object bloc, object transition)
    {
    }

    /// <inheritdoc />
    public virtual void OnError(object bloc, Exception error)
    {
    }

    /// <inheritdoc />
    public virtual void OnClose(object bloc)
    {
    }
}

/// <summary>
/// Wraps an exception thrown by a <see cref="BlocEventHandler{TEvent, TState}"/> so the
/// failing bloc type and event are preserved when reported to <see cref="IBlocObserver.OnError"/>.
/// </summary>
public sealed class BlocUnhandledErrorException : Exception
{
    /// <summary>Creates the wrapper for an error thrown while processing <paramref name="event"/>.</summary>
    public BlocUnhandledErrorException(Type blocType, object? @event, Exception innerException)
        : base($"Unhandled exception in {blocType.Name} while processing {@event?.GetType().Name ?? "event"}.", innerException)
    {
        BlocType = blocType;
        Event = @event;
    }

    /// <summary>The concrete bloc type that failed.</summary>
    public Type BlocType { get; }

    /// <summary>The event being processed when the error occurred, if any.</summary>
    public object? Event { get; }
}

/// <summary>
/// Global bloc configuration: the process-wide <see cref="Observer"/> and
/// shorthand access to the built-in <see cref="EventTransformers"/>.
/// </summary>
public static class Bloc
{
    private static IBlocObserver _observer = new NoopBlocObserver();

    /// <summary>
    /// The observer notified about every bloc/cubit lifecycle event.
    /// Defaults to a no-op observer; never <c>null</c>.
    /// </summary>
    public static IBlocObserver Observer
    {
        get => _observer;
        set => _observer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Shorthand for <see cref="EventTransformers.Sequential{TEvent}"/>.</summary>
    public static EventTransformer<TEvent> Sequential<TEvent>() => EventTransformers.Sequential<TEvent>();

    /// <summary>Shorthand for <see cref="EventTransformers.Concurrent{TEvent}"/>.</summary>
    public static EventTransformer<TEvent> Concurrent<TEvent>() => EventTransformers.Concurrent<TEvent>();

    /// <summary>Shorthand for <see cref="EventTransformers.Restartable{TEvent}"/>.</summary>
    public static EventTransformer<TEvent> Restartable<TEvent>() => EventTransformers.Restartable<TEvent>();

    /// <summary>Shorthand for <see cref="EventTransformers.Droppable{TEvent}"/>.</summary>
    public static EventTransformer<TEvent> Droppable<TEvent>() => EventTransformers.Droppable<TEvent>();

    internal static void NotifyObserver(Action<IBlocObserver> callback)
    {
        try
        {
            callback(Observer);
        }
        catch
        {
            // Observer hooks must never break app logic.
        }
    }

    private sealed class NoopBlocObserver : BlocObserver
    {
    }
}

internal sealed class StateObservable<TState> : IObservable<TState>, IDisposable
{
    private readonly object _gate = new();
    private List<IObserver<TState>> _observers = [];
    private bool _isStopped;
    private Exception? _terminalError;

    public IDisposable Subscribe(IObserver<TState> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_gate)
        {
            if (_isStopped)
            {
                if (_terminalError is not null)
                {
                    observer.OnError(_terminalError);
                }
                else
                {
                    observer.OnCompleted();
                }

                return EmptySubscription.Instance;
            }

            _observers.Add(observer);
            return new Subscription(this, observer);
        }
    }

    public void OnNext(TState value)
    {
        IObserver<TState>[] snapshot;
        lock (_gate)
        {
            if (_isStopped)
            {
                return;
            }

            snapshot = _observers.ToArray();
        }

        foreach (var observer in snapshot)
        {
            observer.OnNext(value);
        }
    }

    public void OnCompleted()
    {
        IObserver<TState>[] snapshot;
        lock (_gate)
        {
            if (_isStopped)
            {
                return;
            }

            _isStopped = true;
            snapshot = _observers.ToArray();
            _observers.Clear();
        }

        foreach (var observer in snapshot)
        {
            observer.OnCompleted();
        }
    }

    public void OnError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IObserver<TState>[] snapshot;
        lock (_gate)
        {
            if (_isStopped)
            {
                return;
            }

            _isStopped = true;
            _terminalError = error;
            snapshot = _observers.ToArray();
            _observers.Clear();
        }

        foreach (var observer in snapshot)
        {
            observer.OnError(error);
        }
    }

    public void Dispose()
    {
        OnCompleted();
    }

    private void Unsubscribe(IObserver<TState> observer)
    {
        lock (_gate)
        {
            if (_isStopped)
            {
                return;
            }

            _ = _observers.Remove(observer);
        }
    }

    private sealed class Subscription(StateObservable<TState> owner, IObserver<TState> observer) : IDisposable
    {
        private StateObservable<TState>? _owner = owner;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _owner, null);
            current?.Unsubscribe(observer);
        }
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static readonly EmptySubscription Instance = new();

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Convenience extensions for subscribing to state streams with plain delegates.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Subscribes to <paramref name="source"/> with callback delegates instead of an
    /// <see cref="IObserver{T}"/> implementation.
    /// </summary>
    /// <returns>A subscription; dispose it to stop receiving notifications.</returns>
    public static IDisposable Subscribe<TState>(
        this IObservable<TState> source,
        Action<TState> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(onNext);
        return source.Subscribe(new AnonymousObserver<TState>(onNext, onError, onCompleted));
    }

    private sealed class AnonymousObserver<TState>(
        Action<TState> onNext,
        Action<Exception>? onError,
        Action? onCompleted) : IObserver<TState>
    {
        public void OnNext(TState value)
        {
            onNext(value);
        }

        public void OnError(Exception error)
        {
            if (onError is null)
            {
                return;
            }

            onError(error);
        }

        public void OnCompleted()
        {
            onCompleted?.Invoke();
        }
    }
}
