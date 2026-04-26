// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class BlocSelector<TBloc, TState, TSelected> : StatefulWidget
    where TBloc : class, IBlocBase<TState>
{
    public BlocSelector(
        Func<TState, TSelected> selector,
        Func<BuildContext, TSelected, Widget> builder,
        TBloc? bloc = null,
        Key? key = null)
        : base(key)
    {
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Bloc = bloc;
    }

    public Func<TState, TSelected> Selector { get; }
    public Func<BuildContext, TSelected, Widget> Builder { get; }
    public TBloc? Bloc { get; }

    public override State CreateState() => new BlocSelectorState();

    private sealed class BlocSelectorState : State
    {
        private readonly Action _listener;
        private TBloc? _bloc;
        private TSelected _selected = default!;
        private bool _hasSelection;
        private bool _disposed;

        public BlocSelectorState()
        {
            _listener = HandleBlocUpdate;
        }

        private BlocSelector<TBloc, TState, TSelected> TypedWidget
            => (BlocSelector<TBloc, TState, TSelected>)StateWidget;

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
                return;
            }

            var nextSelected = TypedWidget.Selector(bloc.State);
            if (!_hasSelection || !EqualityComparer<TSelected>.Default.Equals(_selected, nextSelected))
            {
                SetState(() =>
                {
                    _selected = nextSelected;
                    _hasSelection = true;
                });
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

            if (!_hasSelection)
            {
                throw new InvalidOperationException("BlocSelector has not resolved initial selection.");
            }

            return TypedWidget.Builder(context, _selected);
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
            _selected = TypedWidget.Selector(bloc.State);
            _hasSelection = true;
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

            var nextSelected = TypedWidget.Selector(_bloc.State);
            if (_hasSelection && EqualityComparer<TSelected>.Default.Equals(_selected, nextSelected))
            {
                return;
            }

            SetState(() =>
            {
                _selected = nextSelected;
                _hasSelection = true;
            });
        }

        private static TBloc ResolveBloc(BuildContext context, TBloc? explicitBloc)
            => explicitBloc ?? BlocProvider<TBloc>.ReadOf(context);
    }
}
