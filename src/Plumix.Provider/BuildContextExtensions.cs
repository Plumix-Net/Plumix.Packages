// Dart parity source (reference): pub.dev/packages/provider (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Provider;

/// <summary>
/// Fluent extensions on <see cref="BuildContext"/> that mirror Flutter provider's
/// <c>context.watch&lt;T&gt;()</c> and <c>context.read&lt;T&gt;()</c> API.
/// </summary>
public static class BuildContextExtensions
{
    /// <summary>
    /// Subscribes to a <see cref="ChangeNotifierProvider{T}"/> and returns its notifier.
    /// The calling widget rebuilds whenever the notifier calls <c>NotifyListeners</c>.
    ///
    /// Equivalent to Flutter's <c>context.watch&lt;T&gt;()</c>.
    ///
    /// Must be called inside <c>Build</c>.
    /// </summary>
    public static T Watch<T>(this BuildContext context) where T : ChangeNotifier
        => ChangeNotifierProvider<T>.Of(context);

    /// <summary>
    /// Returns the <see cref="ChangeNotifier"/> without subscribing to rebuilds.
    /// Use this inside event handlers and callbacks — not inside <c>Build</c>.
    ///
    /// Equivalent to Flutter's <c>context.read&lt;T&gt;()</c>.
    /// </summary>
    public static T Read<T>(this BuildContext context) where T : ChangeNotifier
        => ChangeNotifierProvider<T>.ReadOf(context);

    /// <summary>
    /// Subscribes to a <see cref="Provider{T}"/> and returns its value.
    /// The calling widget rebuilds whenever the value changes.
    ///
    /// Equivalent to Flutter's <c>context.watch&lt;T&gt;()</c> for plain providers.
    /// </summary>
    public static T WatchValue<T>(this BuildContext context)
        => Provider<T>.Of(context);

    /// <summary>
    /// Returns the value from a <see cref="Provider{T}"/> without subscribing.
    /// Equivalent to Flutter's <c>context.read&lt;T&gt;()</c> for plain providers.
    /// </summary>
    public static T ReadValue<T>(this BuildContext context)
        => Provider<T>.ReadOf(context);
}
