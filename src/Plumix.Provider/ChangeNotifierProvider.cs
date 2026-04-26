// Dart parity source (reference): pub.dev/packages/provider (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Provider;

/// <summary>
/// Provides a <see cref="ChangeNotifier"/> of type <typeparamref name="T"/> to the widget subtree
/// and automatically rebuilds descendants when the notifier calls <see cref="ChangeNotifier.NotifyListeners"/>.
///
/// Equivalent to Flutter's <c>ChangeNotifierProvider&lt;T&gt;</c>.
///
/// Usage:
/// <code>
/// new ChangeNotifierProvider&lt;CounterModel&gt;(
///     notifier: _counter,
///     child: new MyApp())
/// </code>
///
/// Subscribe from a descendant:
/// <code>
/// var counter = context.Watch&lt;CounterModel&gt;();
/// </code>
///
/// Read without subscribing:
/// <code>
/// var counter = context.Read&lt;CounterModel&gt;();
/// counter.Increment();
/// </code>
/// </summary>
public sealed class ChangeNotifierProvider<T> : InheritedNotifier<T> where T : ChangeNotifier
{
    public ChangeNotifierProvider(T notifier, Widget child, Key? key = null)
        : base(notifier, child, key)
    {
    }

    /// <summary>
    /// Subscribes the current widget to changes and returns the notifier.
    /// Throws if no provider is found in the ancestor tree.
    /// </summary>
    public static T Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No ChangeNotifierProvider<{typeof(T).Name}> found in the widget tree. " +
               $"Make sure to place a ChangeNotifierProvider<{typeof(T).Name}> above this widget.");

    /// <summary>Returns the notifier or <c>null</c> if no provider is in scope.</summary>
    public static T? MaybeOf(BuildContext context)
        => context.DependOnInherited<ChangeNotifierProvider<T>>()?.Notifier;

    /// <summary>
    /// Returns the notifier without subscribing to rebuilds.
    /// Useful for calling methods on the notifier in event handlers.
    /// </summary>
    public static T ReadOf(BuildContext context)
        => ReadMaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No ChangeNotifierProvider<{typeof(T).Name}> found in the widget tree.");

    /// <summary>Reads the notifier without subscribing, or returns <c>null</c>.</summary>
    public static T? ReadMaybeOf(BuildContext context)
        => context.GetInherited<ChangeNotifierProvider<T>>()?.Notifier;
}
