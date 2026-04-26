// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

public sealed class BlocProvider<TBloc> : InheritedNotifier<TBloc> where TBloc : class, IBlocBase
{
    public BlocProvider(TBloc bloc, Widget child, Key? key = null) : base(bloc, child, key)
    {
    }

    public static TBloc Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No BlocProvider<{typeof(TBloc).Name}> found in the widget tree. " +
               $"Place BlocProvider<{typeof(TBloc).Name}> above this widget.");

    public static TBloc? MaybeOf(BuildContext context)
        => context.DependOnInherited<BlocProvider<TBloc>>()?.Notifier;

    public static TBloc ReadOf(BuildContext context)
        => ReadMaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No BlocProvider<{typeof(TBloc).Name}> found in the widget tree.");

    public static TBloc? ReadMaybeOf(BuildContext context)
        => context.GetInherited<BlocProvider<TBloc>>()?.Notifier;
}
