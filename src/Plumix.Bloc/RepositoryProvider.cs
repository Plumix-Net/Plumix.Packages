// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using System.Diagnostics.CodeAnalysis;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

/// <summary>
/// Exposes a repository (or any plain value) of type <typeparamref name="T"/> to the widget subtree.
///
/// Equivalent to flutter_bloc's <c>RepositoryProvider&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The provided value type.</typeparam>
public sealed class RepositoryProvider<T> : InheritedWidget
{
    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="value">The value exposed to descendants.</param>
    /// <param name="child">The subtree that gains access to the value.</param>
    /// <param name="key">The widget key.</param>
    public RepositoryProvider(T value, Widget child, Key? key = null) : base(key)
    {
        Value = value;
        Child = child;
    }

    /// <summary>The value exposed to descendants.</summary>
    public T Value { get; }

    /// <summary>The subtree that gains access to the value.</summary>
    public Widget Child { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => Child;

    /// <inheritdoc />
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        => !EqualityComparer<T>.Default.Equals(((RepositoryProvider<T>)oldWidget).Value, Value);

    /// <summary>
    /// Returns the nearest provided value and subscribes the calling widget to changes.
    /// Throws if no provider is found in the ancestor tree.
    /// </summary>
    public static T Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No RepositoryProvider<{typeof(T).Name}> found in the widget tree.");

    /// <summary>Like <see cref="Of"/>, but returns <c>default</c> when no provider is in scope.</summary>
    [return: MaybeNull]
    public static T MaybeOf(BuildContext context)
    {
        var provider = context.DependOnInherited<RepositoryProvider<T>>();
        return provider is not null ? provider.Value : default;
    }

    /// <summary>Returns the nearest provided value without subscribing to changes.</summary>
    public static T ReadOf(BuildContext context)
    {
        var value = ReadMaybeOf(context);
        if (value is null)
        {
            throw new InvalidOperationException(
                $"No RepositoryProvider<{typeof(T).Name}> found in the widget tree.");
        }

        return value;
    }

    /// <summary>Like <see cref="ReadOf"/>, but returns <c>default</c> when no provider is in scope.</summary>
    [return: MaybeNull]
    public static T ReadMaybeOf(BuildContext context)
    {
        var provider = context.GetInherited<RepositoryProvider<T>>();
        return provider is not null ? provider.Value : default;
    }
}
