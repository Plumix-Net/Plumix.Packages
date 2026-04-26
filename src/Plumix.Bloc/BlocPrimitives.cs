// Dart parity source (reference): pub.dev/packages/bloc and pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;

namespace Plumix.Bloc;

public interface IBlocBase : IListenable, IDisposable
{
    object? StateObject { get; }
    bool IsClosed { get; }
    void Close();
}

public interface IBlocBase<out TState> : IBlocBase
{
    TState State { get; }
    IObservable<TState> Stream { get; }
}

public readonly record struct Change<TState>(TState CurrentState, TState NextState);

public readonly record struct Transition<TEvent, TState>(TState CurrentState, TEvent Event, TState NextState);

public interface IEmitter<in TState>
{
    bool IsDone { get; }
    void Emit(TState state);
}

public delegate ValueTask BlocEventHandler<in TEvent, TState>(
    TEvent @event,
    IEmitter<TState> emitter,
    CancellationToken cancellationToken);

public enum EventConcurrency
{
    Sequential,
    Concurrent,
    Restartable,
    Droppable
}

public delegate EventConcurrency EventTransformer<in TEvent>(TEvent @event);

public static class EventTransformers
{
    public static EventTransformer<TEvent> Sequential<TEvent>() => static _ => EventConcurrency.Sequential;
    public static EventTransformer<TEvent> Concurrent<TEvent>() => static _ => EventConcurrency.Concurrent;
    public static EventTransformer<TEvent> Restartable<TEvent>() => static _ => EventConcurrency.Restartable;
    public static EventTransformer<TEvent> Droppable<TEvent>() => static _ => EventConcurrency.Droppable;
}

public interface IBlocObserver
{
    void OnCreate(object bloc);
    void OnEvent(object bloc, object @event);
    void OnChange(object bloc, object change);
    void OnTransition(object bloc, object transition);
    void OnError(object bloc, Exception error);
    void OnClose(object bloc);
}

public abstract class BlocObserver : IBlocObserver
{
    public virtual void OnCreate(object bloc)
    {
    }

    public virtual void OnEvent(object bloc, object @event)
    {
    }

    public virtual void OnChange(object bloc, object change)
    {
    }

    public virtual void OnTransition(object bloc, object transition)
    {
    }

    public virtual void OnError(object bloc, Exception error)
    {
    }

    public virtual void OnClose(object bloc)
    {
    }
}

public sealed class BlocUnhandledErrorException : Exception
{
    public BlocUnhandledErrorException(Type blocType, object? @event, Exception innerException)
        : base($"Unhandled exception in {blocType.Name} while processing {@event?.GetType().Name ?? "event"}.", innerException)
    {
        BlocType = blocType;
        Event = @event;
    }

    public Type BlocType { get; }
    public object? Event { get; }
}

public static class Bloc
{
    private static IBlocObserver _observer = new NoopBlocObserver();

    public static IBlocObserver Observer
    {
        get => _observer;
        set => _observer = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static EventTransformer<TEvent> Sequential<TEvent>() => EventTransformers.Sequential<TEvent>();
    public static EventTransformer<TEvent> Concurrent<TEvent>() => EventTransformers.Concurrent<TEvent>();
    public static EventTransformer<TEvent> Restartable<TEvent>() => EventTransformers.Restartable<TEvent>();
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

public static class ObservableExtensions
{
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
