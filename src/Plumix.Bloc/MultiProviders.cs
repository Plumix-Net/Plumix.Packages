// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class MultiBlocProvider : StatelessWidget
{
    private readonly Widget _child;
    private readonly IReadOnlyList<Func<Widget, Widget>> _providers;

    public MultiBlocProvider(
        Widget child,
        IReadOnlyList<Func<Widget, Widget>> providers,
        Key? key = null)
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

public sealed class MultiRepositoryProvider : StatelessWidget
{
    private readonly Widget _child;
    private readonly IReadOnlyList<Func<Widget, Widget>> _providers;

    public MultiRepositoryProvider(
        Widget child,
        IReadOnlyList<Func<Widget, Widget>> providers,
        Key? key = null)
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
