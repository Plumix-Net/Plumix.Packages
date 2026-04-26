// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class BlocConsumer<TBloc, TState> : StatelessWidget
    where TBloc : class, IBlocBase<TState>
{
    public BlocConsumer(
        Func<BuildContext, TState, Widget> builder,
        Action<BuildContext, TState> listener,
        TBloc? bloc = null,
        Func<TState, TState, bool>? buildWhen = null,
        Func<TState, TState, bool>? listenWhen = null,
        Key? key = null)
        : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        Bloc = bloc;
        BuildWhen = buildWhen;
        ListenWhen = listenWhen;
    }

    public Func<BuildContext, TState, Widget> Builder { get; }
    public Action<BuildContext, TState> Listener { get; }
    public Func<TState, TState, bool>? BuildWhen { get; }
    public Func<TState, TState, bool>? ListenWhen { get; }
    public TBloc? Bloc { get; }

    public override Widget Build(BuildContext context)
    {
        return new BlocListener<TBloc, TState>(
            listener: Listener,
            child: new BlocBuilder<TBloc, TState>(
                builder: Builder,
                bloc: Bloc,
                buildWhen: BuildWhen),
            bloc: Bloc,
            listenWhen: ListenWhen);
    }
}
