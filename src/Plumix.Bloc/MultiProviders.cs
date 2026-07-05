// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

/// <summary>
/// Stacks multiple <see cref="BlocProvider{TBloc}"/> widgets, removing the need to nest them manually.
/// Each factory receives the next widget as its child, building the nesting from bottom to top.
///
/// Equivalent to flutter_bloc's <c>MultiBlocProvider</c>.
/// </summary>
public sealed class MultiBlocProvider : StatelessWidget
{
    private readonly Widget _child;
    private readonly IReadOnlyList<Func<Widget, Widget>> _providers;

    /// <summary>
    /// Creates the widget.
    /// </summary>
    /// <param name="child">The innermost subtree.</param>
    /// <param name="providers">Provider factories, outermost first.</param>
    /// <param name="key">The widget key.</param>
    public MultiBlocProvider(
        Widget child,
        IReadOnlyList<Func<Widget, Widget>> providers,
        Key? key = null)
        : base(key)
    {
        _child = child;
        _providers = providers;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var current = _child;
        for (var i = _providers.Count - 1; i >= 0; i--)
        {
            current = _providers[i](current);
        }

        return current;
    }
}

/// <summary>
/// Stacks multiple <see cref="RepositoryProvider{T}"/> widgets, removing the need to nest them manually.
/// Each factory receives the next widget as its child, building the nesting from bottom to top.
///
/// Equivalent to flutter_bloc's <c>MultiRepositoryProvider</c>.
/// </summary>
public sealed class MultiRepositoryProvider : StatelessWidget
{
    private readonly Widget _child;
    private readonly IReadOnlyList<Func<Widget, Widget>> _providers;

    /// <summary>
    /// Creates the widget.
    /// </summary>
    /// <param name="child">The innermost subtree.</param>
    /// <param name="providers">Provider factories, outermost first.</param>
    /// <param name="key">The widget key.</param>
    public MultiRepositoryProvider(
        Widget child,
        IReadOnlyList<Func<Widget, Widget>> providers,
        Key? key = null)
        : base(key)
    {
        _child = child;
        _providers = providers;
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var current = _child;
        for (var i = _providers.Count - 1; i >= 0; i--)
        {
            current = _providers[i](current);
        }

        return current;
    }
}
