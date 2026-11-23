// Dart parity source (reference): pub.dev/packages/bloc (approximate)

namespace Plumix.Bloc;

/// <summary>
/// Manages a state of type <typeparamref name="TState"/> and exposes it as a
/// <see cref="Stream"/> plus listener callbacks. Subclasses mutate the state by
/// calling <see cref="Emit"/>.
///
/// Equivalent to bloc's <c>Cubit&lt;State&gt;</c>.
/// </summary>
/// <typeparam name="TState">The type of the managed state.</typeparam>
public class Cubit<TState> : IBlocBase<TState>
{
    private readonly object _listenersGate = new();
    private readonly List<Action> _listeners = [];
    private readonly StateObservable<TState> _stream = new();
    private TState _state;
    private bool _isClosed;

    /// <summary>Creates the cubit with its initial state.</summary>
    public Cubit(TState initialState)
    {
        _state = initialState;
        Bloc.NotifyObserver(observer => observer.OnCreate(this));
    }

    /// <inheritdoc />
    public TState State => _state;

    /// <inheritdoc />
    public IObservable<TState> Stream => _stream;

    /// <inheritdoc />
    public bool IsClosed => _isClosed;

    object? IBlocBase.StateObject => _state;

    /// <summary>Registers a callback invoked after every state change.</summary>
    /// <exception cref="ObjectDisposedException">The cubit is closed.</exception>
    public virtual void AddListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ThrowIfClosed();

        lock (_listenersGate)
        {
            ThrowIfClosed();
            _listeners.Add(listener);
        }
    }

    /// <summary>Removes a callback previously registered with <see cref="AddListener"/>.</summary>
    public virtual void RemoveListener(Action listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_listenersGate)
        {
            if (_isClosed)
            {
                return;
            }

            _ = _listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Called on every state change before listeners are notified.
    /// The base implementation forwards the change to <see cref="Bloc.Observer"/>.
    /// </summary>
    protected virtual void OnChange(Change<TState> change)
    {
        Bloc.NotifyObserver(observer => observer.OnChange(this, change));
    }

    /// <summary>
    /// Called when an error is reported via <see cref="ReportError"/>.
    /// The base implementation forwards the error to <see cref="Bloc.Observer"/>.
    /// </summary>
    protected virtual void OnError(Exception error)
    {
        Bloc.NotifyObserver(observer => observer.OnError(this, error));
    }

    /// <summary>
    /// Updates <see cref="State"/> and notifies the stream and listeners.
    /// Emitting a state equal to the current one is a no-op.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The cubit is closed.</exception>
    protected void Emit(TState state)
    {
        ThrowIfClosed();

        if (EqualityComparer<TState>.Default.Equals(_state, state))
        {
            return;
        }

        var change = new Change<TState>(_state, state);
        OnChange(change);
        _state = state;
        _stream.OnNext(state);
        NotifyListeners();
    }

    /// <summary>Reports an error to <see cref="OnError"/> without changing state.</summary>
    protected void ReportError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        OnError(error);
    }

    /// <inheritdoc />
    public virtual void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _stream.OnCompleted();

        lock (_listenersGate)
        {
            _listeners.Clear();
        }

        Bloc.NotifyObserver(observer => observer.OnClose(this));
    }

    /// <summary>Closes the cubit. Equivalent to calling <see cref="Close"/>.</summary>
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    /// <summary>Throws <see cref="ObjectDisposedException"/> if the cubit is closed.</summary>
    protected void ThrowIfClosed()
    {
        ObjectDisposedException.ThrowIf(_isClosed, this);
    }

    private void NotifyListeners()
    {
        Action[] snapshot;
        lock (_listenersGate)
        {
            if (_isClosed)
            {
                return;
            }

            snapshot = _listeners.ToArray();
        }

        foreach (var listener in snapshot)
        {
            listener();
        }
    }
}
