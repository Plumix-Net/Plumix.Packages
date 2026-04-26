// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using System.Diagnostics.CodeAnalysis;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class RepositoryProvider<T> : InheritedWidget
{
    public RepositoryProvider(T value, Widget child, Key? key = null) : base(key)
    {
        Value = value;
        Child = child;
    }

    public T Value { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        => !EqualityComparer<T>.Default.Equals(((RepositoryProvider<T>)oldWidget).Value, Value);

    public static T Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No RepositoryProvider<{typeof(T).Name}> found in the widget tree.");

    [return: MaybeNull]
    public static T MaybeOf(BuildContext context)
    {
        var provider = context.DependOnInherited<RepositoryProvider<T>>();
        return provider is not null ? provider.Value : default;
    }

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

    [return: MaybeNull]
    public static T ReadMaybeOf(BuildContext context)
    {
        var provider = context.GetInherited<RepositoryProvider<T>>();
        return provider is not null ? provider.Value : default;
    }
}
