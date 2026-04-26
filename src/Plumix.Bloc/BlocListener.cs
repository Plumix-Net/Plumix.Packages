// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class BlocListener<TBloc, TState> : StatefulWidget
    where TBloc : class, IBlocBase<TState>
{
    public BlocListener(
        Action<BuildContext, TState> listener,
        Widget child,
        TBloc? bloc = null,
        Func<TState, TState, bool>? listenWhen = null,
        Key? key = null)
        : base(key)
    {
        Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Bloc = bloc;
        ListenWhen = listenWhen;
    }

    public Action<BuildContext, TState> Listener { get; }
    public Func<TState, TState, bool>? ListenWhen { get; }
    public Widget Child { get; }
    public TBloc? Bloc { get; }

    public override State CreateState() => new BlocListenerState();

    private sealed class BlocListenerState : State
    {
        private readonly Action _listener;
        private TBloc? _bloc;
        private TState _previousState = default!;
        private bool _hasPreviousState;
        private bool _disposed;

        public BlocListenerState()
        {
            _listener = HandleBlocUpdate;
        }

        private BlocListener<TBloc, TState> TypedWidget => (BlocListener<TBloc, TState>)StateWidget;

        public override void InitState()
        {
            base.InitState();
            var bloc = ResolveBloc(Context, TypedWidget.Bloc);
            Subscribe(bloc);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var bloc = ResolveBloc(Context, TypedWidget.Bloc);
            if (!ReferenceEquals(bloc, _bloc))
            {
                Subscribe(bloc);
            }
        }

        public override Widget Build(BuildContext context)
        {
            if (TypedWidget.Bloc is null)
            {
                var bloc = ResolveBloc(context, null);
                if (!ReferenceEquals(bloc, _bloc))
                {
                    Subscribe(bloc);
                }
            }

            return TypedWidget.Child;
        }

        public override void Dispose()
        {
            _disposed = true;
            Unsubscribe();
            base.Dispose();
        }

        private void Subscribe(TBloc bloc)
        {
            if (_disposed)
            {
                return;
            }

            Unsubscribe();
            _bloc = bloc;
            _previousState = bloc.State;
            _hasPreviousState = true;
            _bloc.AddListener(_listener);
        }

        private void Unsubscribe()
        {
            if (_bloc is null)
            {
                return;
            }

            _bloc.RemoveListener(_listener);
            _bloc = null;
        }

        private void HandleBlocUpdate()
        {
            if (_disposed || _bloc is null)
            {
                return;
            }

            var currentState = _bloc.State;
            var shouldListen = !_hasPreviousState
                || TypedWidget.ListenWhen?.Invoke(_previousState, currentState) != false;

            _previousState = currentState;
            _hasPreviousState = true;

            if (shouldListen)
            {
                TypedWidget.Listener(Context, currentState);
            }
        }

        private static TBloc ResolveBloc(BuildContext context, TBloc? explicitBloc)
            => explicitBloc ?? BlocProvider<TBloc>.ReadOf(context);
    }
}
