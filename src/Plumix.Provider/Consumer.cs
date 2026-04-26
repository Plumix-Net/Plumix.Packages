// Dart parity source (reference): pub.dev/packages/provider (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Provider;

/// <summary>
/// A widget that subscribes to a <see cref="ChangeNotifierProvider{T}"/> and rebuilds
/// whenever the notifier changes.
///
/// Equivalent to Flutter's <c>Consumer&lt;T&gt;</c>.
///
/// Usage:
/// <code>
/// new Consumer&lt;CounterModel&gt;(
///     builder: (context, counter) => new Text(counter.Count.ToString()))
/// </code>
/// </summary>
public sealed class Consumer<T> : StatelessWidget where T : ChangeNotifier
{
    public Consumer(Func<BuildContext, T, Widget> builder, Key? key = null) : base(key)
    {
        Builder = builder;
    }

    public Func<BuildContext, T, Widget> Builder { get; }

    public override Widget Build(BuildContext context)
    {
        var notifier = context.Watch<T>();
        return Builder(context, notifier);
    }
}

/// <summary>
/// A widget that subscribes to two <see cref="ChangeNotifier"/>s and rebuilds when either changes.
///
/// Equivalent to Flutter's <c>Consumer2&lt;A, B&gt;</c>.
/// </summary>
public sealed class Consumer2<TA, TB> : StatelessWidget
    where TA : ChangeNotifier
    where TB : ChangeNotifier
{
    public Consumer2(Func<BuildContext, TA, TB, Widget> builder, Key? key = null) : base(key)
    {
        Builder = builder;
    }

    public Func<BuildContext, TA, TB, Widget> Builder { get; }

    public override Widget Build(BuildContext context)
    {
        var a = context.Watch<TA>();
        var b = context.Watch<TB>();
        return Builder(context, a, b);
    }
}
