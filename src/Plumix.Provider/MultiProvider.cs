// Dart parity source (reference): pub.dev/packages/provider (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Provider;

/// <summary>
/// Stacks multiple provider widgets, removing the need to nest them manually.
///
/// Equivalent to Flutter's <c>MultiProvider</c>.
///
/// Each factory in <paramref name="providers"/> receives the next widget as its child,
/// building the nesting from bottom to top.
///
/// Usage:
/// <code>
/// new MultiProvider(
///     child: new MyApp(),
///     providers:
///     [
///         child => new ChangeNotifierProvider&lt;CounterModel&gt;(_counter, child),
///         child => new ChangeNotifierProvider&lt;ThemeModel&gt;(_theme, child),
///         child => new Provider&lt;AppConfig&gt;(_config, child),
///     ])
/// </code>
/// </summary>
public sealed class MultiProvider : StatelessWidget
{
    private readonly Widget _child;
    private readonly IReadOnlyList<Func<Widget, Widget>> _providers;

    public MultiProvider(Widget child, IReadOnlyList<Func<Widget, Widget>> providers, Key? key = null)
        : base(key)
    {
        _child = child;
        _providers = providers;
    }

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
