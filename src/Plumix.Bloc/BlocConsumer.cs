// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

/// <summary>
/// Combines <see cref="BlocBuilder{TBloc, TState}"/> and <see cref="BlocListener{TBloc, TState}"/>:
/// rebuilds from <c>builder</c> and invokes <c>listener</c> on each state change.
///
/// Equivalent to flutter_bloc's <c>BlocConsumer&lt;B, S&gt;</c>.
/// </summary>
/// <typeparam name="TBloc">The observed bloc/cubit type.</typeparam>
/// <typeparam name="TState">The type of the bloc's state.</typeparam>
public sealed class BlocConsumer<TBloc, TState> : StatelessWidget
    where TBloc : class, IBlocBase<TState>
{
    /// <summary>
    /// Creates the consumer widget.
    /// </summary>
    /// <param name="builder">Builds the subtree for the current state.</param>
    /// <param name="listener">Invoked with the new state after each change.</param>
    /// <param name="bloc">
    /// The bloc to observe; when <c>null</c>, the nearest <see cref="BlocProvider{TBloc}"/> is used.
    /// </param>
    /// <param name="buildWhen">Optional predicate <c>(previous, next)</c> deciding whether to rebuild.</param>
    /// <param name="listenWhen">Optional predicate <c>(previous, next)</c> deciding whether to invoke the listener.</param>
    /// <param name="key">The widget key.</param>
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

    /// <summary>Builds the subtree for the current state.</summary>
    public Func<BuildContext, TState, Widget> Builder { get; }

    /// <summary>Invoked with the new state after each change.</summary>
    public Action<BuildContext, TState> Listener { get; }

    /// <summary>Optional predicate <c>(previous, next)</c> deciding whether to rebuild.</summary>
    public Func<TState, TState, bool>? BuildWhen { get; }

    /// <summary>Optional predicate <c>(previous, next)</c> deciding whether to invoke the listener.</summary>
    public Func<TState, TState, bool>? ListenWhen { get; }

    /// <summary>The explicitly supplied bloc, or <c>null</c> to resolve from the tree.</summary>
    public TBloc? Bloc { get; }

    /// <inheritdoc />
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
