// Dart parity source (reference): pub.dev/packages/bloc (approximate)

using Plumix.Foundation;

namespace Plumix.Bloc;

public class Cubit<TState> : IBlocBase<TState>
{
    private readonly object _listenersGate = new();
    private readonly List<Action> _listeners = [];
    private readonly StateObservable<TState> _stream = new();
    private TState _state;
    private bool _isClosed;

    public Cubit(TState initialState)
    {
        _state = initialState;
        Bloc.NotifyObserver(observer => observer.OnCreate(this));
    }

    public TState State => _state;
    public IObservable<TState> Stream => _stream;
    public bool IsClosed => _isClosed;
    object? IBlocBase.StateObject => _state;

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

    protected virtual void OnChange(Change<TState> change)
    {
        Bloc.NotifyObserver(observer => observer.OnChange(this, change));
    }

    protected virtual void OnError(Exception error)
    {
        Bloc.NotifyObserver(observer => observer.OnError(this, error));
    }

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

    protected void ReportError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        OnError(error);
    }

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

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    protected void ThrowIfClosed()
    {
        if (_isClosed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
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
