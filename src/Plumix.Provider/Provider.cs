// Dart parity source (reference): pub.dev/packages/provider (approximate)

using System.Diagnostics.CodeAnalysis;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Provider;

/// <summary>
/// Exposes an immutable value of type <typeparamref name="T"/> to the widget subtree.
///
/// Equivalent to Flutter's <c>Provider&lt;T&gt;</c>.
/// The widget rebuilds dependents when the value changes (by reference/equality).
///
/// Usage:
/// <code>
/// new Provider&lt;ThemeData&gt;(
///     value: myTheme,
///     child: new MyApp())
/// </code>
///
/// Read from a descendant:
/// <code>
/// var theme = Provider&lt;ThemeData&gt;.Of(context);
/// // or
/// var theme = context.Watch&lt;ThemeData&gt;();
/// </code>
/// </summary>
public sealed class Provider<T> : InheritedWidget
{
    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="value">The value exposed to descendants.</param>
    /// <param name="child">The subtree that gains access to the value.</param>
    /// <param name="key">The widget key.</param>
    public Provider(T value, Widget child, Key? key = null) : base(child, key)
    {
        Value = value;
    }

    /// <summary>The value exposed to descendants.</summary>
    public T Value { get; }

    /// <inheritdoc />
    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
        => !EqualityComparer<T>.Default.Equals(((Provider<T>)oldWidget).Value, Value);

    /// <summary>
    /// Reads the nearest <see cref="Provider{T}"/> and subscribes to changes.
    /// Throws if no provider is found in the ancestor tree.
    /// </summary>
    public static T Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No Provider<{typeof(T).Name}> found in the widget tree. " +
               $"Make sure to place a Provider<{typeof(T).Name}> above this widget.");

    /// <summary>Returns the value or <c>default</c> if no provider is in scope.</summary>
    [return: MaybeNull]
    public static T MaybeOf(BuildContext context)
    {
        var provider = context.DependOnInheritedWidgetOfExactType<Provider<T>>();
        return provider is not null ? provider.Value : default;
    }

    /// <summary>
    /// Reads the value without subscribing to changes.
    /// Equivalent to Flutter's <c>Provider.of&lt;T&gt;(context, listen: false)</c>.
    /// </summary>
    public static T ReadOf(BuildContext context)
    {
        var value = ReadMaybeOf(context);
        if (value is null)
            throw new InvalidOperationException($"No Provider<{typeof(T).Name}> found in the widget tree.");
        return value;
    }

    /// <summary>Reads the value without subscribing, or returns <c>default</c>.</summary>
    [return: MaybeNull]
    public static T ReadMaybeOf(BuildContext context)
    {
        var provider = context.GetInheritedWidgetOfExactType<Provider<T>>();
        return provider is not null ? provider.Value : default;
    }
}
