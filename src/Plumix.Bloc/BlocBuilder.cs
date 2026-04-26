// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class BlocBuilder<TBloc, TState> : StatefulWidget
    where TBloc : class, IBlocBase<TState>
{
    public BlocBuilder(
        Func<BuildContext, TState, Widget> builder,
        TBloc? bloc = null,
        Func<TState, TState, bool>? buildWhen = null,
        Key? key = null)
        : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Bloc = bloc;
        BuildWhen = buildWhen;
    }

    public Func<BuildContext, TState, Widget> Builder { get; }
    public Func<TState, TState, bool>? BuildWhen { get; }
    public TBloc? Bloc { get; }

    public override State CreateState() => new BlocBuilderState();

    private sealed class BlocBuilderState : State
    {
        private readonly Action _listener;
        private TBloc? _bloc;
        private TState _state = default!;
        private bool _hasState;
        private bool _disposed;

        public BlocBuilderState()
        {
            _listener = HandleBlocUpdate;
        }

        private BlocBuilder<TBloc, TState> TypedWidget => (BlocBuilder<TBloc, TState>)StateWidget;

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

            if (!_hasState)
            {
                throw new InvalidOperationException("BlocBuilder has not resolved initial state.");
            }

            return TypedWidget.Builder(context, _state);
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
            _state = bloc.State;
            _hasState = true;
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

            var nextState = _bloc.State;
            if (!_hasState)
            {
                SetState(() =>
                {
                    _state = nextState;
                    _hasState = true;
                });
                return;
            }

            var previousState = _state;
            var shouldBuild = TypedWidget.BuildWhen?.Invoke(previousState, nextState) ?? true;
            _state = nextState;

            if (shouldBuild)
            {
                SetState(() =>
                {
                });
            }
        }

        private static TBloc ResolveBloc(BuildContext context, TBloc? explicitBloc)
            => explicitBloc ?? BlocProvider<TBloc>.ReadOf(context);
    }
}
