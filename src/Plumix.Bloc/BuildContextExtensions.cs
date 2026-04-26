// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Widgets;

namespace Plumix.Bloc;

public static class BuildContextExtensions
{
    public static TBloc Read<TBloc>(this BuildContext context) where TBloc : class, IBlocBase
        => BlocProvider<TBloc>.ReadOf(context);

    public static TBloc Watch<TBloc>(this BuildContext context) where TBloc : class, IBlocBase
        => BlocProvider<TBloc>.Of(context);

    public static TState Watch<TBloc, TState>(this BuildContext context)
        where TBloc : class, IBlocBase<TState>
        => BlocProvider<TBloc>.Of(context).State;

    public static TSelected Select<TBloc, TState, TSelected>(
        this BuildContext context,
        Func<TState, TSelected> selector)
        where TBloc : class, IBlocBase<TState>
    {
        ArgumentNullException.ThrowIfNull(selector);
        return selector(BlocProvider<TBloc>.Of(context).State);
    }

    public static TRepository ReadRepository<TRepository>(this BuildContext context)
        => RepositoryProvider<TRepository>.ReadOf(context);

    public static TRepository WatchRepository<TRepository>(this BuildContext context)
        => RepositoryProvider<TRepository>.Of(context);
}
