// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Widgets;

namespace Plumix.Bloc;

/// <summary>
/// Fluent extensions on <see cref="BuildContext"/> that mirror flutter_bloc's
/// <c>context.read&lt;T&gt;()</c>, <c>context.watch&lt;T&gt;()</c> and <c>context.select</c> API.
/// </summary>
public static class BuildContextExtensions
{
    /// <summary>
    /// Returns the nearest provided bloc without subscribing to rebuilds.
    /// Use inside event handlers and callbacks — not inside <c>Build</c>.
    /// </summary>
    public static TBloc Read<TBloc>(this BuildContext context) where TBloc : class, IBlocBase
        => BlocProvider<TBloc>.ReadOf(context);

    /// <summary>
    /// Returns the nearest provided bloc and subscribes the calling widget to rebuilds.
    /// Must be called inside <c>Build</c>.
    /// </summary>
    public static TBloc Watch<TBloc>(this BuildContext context) where TBloc : class, IBlocBase
        => BlocProvider<TBloc>.Of(context);

    /// <summary>
    /// Returns the current state of the nearest provided bloc and subscribes the calling
    /// widget to rebuilds. Must be called inside <c>Build</c>.
    /// </summary>
    public static TState Watch<TBloc, TState>(this BuildContext context)
        where TBloc : class, IBlocBase<TState>
        => BlocProvider<TBloc>.Of(context).State;

    /// <summary>
    /// Returns a value derived from the current state of the nearest provided bloc and
    /// subscribes the calling widget to rebuilds. Must be called inside <c>Build</c>.
    /// </summary>
    public static TSelected Select<TBloc, TState, TSelected>(
        this BuildContext context,
        Func<TState, TSelected> selector)
        where TBloc : class, IBlocBase<TState>
    {
        ArgumentNullException.ThrowIfNull(selector);
        return selector(BlocProvider<TBloc>.Of(context).State);
    }

    /// <summary>
    /// Returns the nearest value provided by a <see cref="RepositoryProvider{T}"/>
    /// without subscribing to changes.
    /// </summary>
    public static TRepository ReadRepository<TRepository>(this BuildContext context)
        => RepositoryProvider<TRepository>.ReadOf(context);

    /// <summary>
    /// Returns the nearest value provided by a <see cref="RepositoryProvider{T}"/>
    /// and subscribes the calling widget to changes.
    /// </summary>
    public static TRepository WatchRepository<TRepository>(this BuildContext context)
        => RepositoryProvider<TRepository>.Of(context);
}
