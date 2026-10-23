// Dart parity source (reference): pub.dev/packages/flutter_bloc (approximate)

using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Bloc;

/// <summary>
/// Provides a bloc or cubit to the widget subtree so descendants can obtain it via
/// <see cref="Of"/>, <see cref="ReadOf"/> or the <c>context.Watch</c>/<c>context.Read</c> extensions.
///
/// Equivalent to flutter_bloc's <c>BlocProvider&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="TBloc">The provided bloc/cubit type.</typeparam>
public sealed class BlocProvider<TBloc> : StatefulWidget where TBloc : class, IBlocBase
{
    private readonly TBloc? _bloc;
    private readonly Func<BuildContext, TBloc>? _create;

    /// <summary>
    /// Provides an existing bloc/cubit instance. Ownership remains external; this provider will not dispose it.
    /// </summary>
    public BlocProvider(TBloc bloc, Widget child, Key? key = null) : base(key)
    {
        _bloc = bloc ?? throw new ArgumentNullException(nameof(bloc));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>
    /// Creates and provides a bloc/cubit instance. The created instance is automatically closed on unmount.
    /// </summary>
    public BlocProvider(Func<BuildContext, TBloc> create, Widget child, Key? key = null) : base(key)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>The subtree that gains access to the bloc.</summary>
    public Widget Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new BlocProviderState();

    private bool HasCreate => _create is not null;
    private TBloc ExistingBloc => _bloc ?? throw new InvalidOperationException("Expected an existing bloc instance.");
    private Func<BuildContext, TBloc> CreateBloc => _create ?? throw new InvalidOperationException("Expected a create delegate.");

    /// <summary>
    /// Returns the nearest provided bloc and subscribes the calling widget to rebuilds.
    /// Throws if no provider is found in the ancestor tree.
    /// </summary>
    public static TBloc Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No BlocProvider<{typeof(TBloc).Name}> found in the widget tree. " +
               $"Place BlocProvider<{typeof(TBloc).Name}> above this widget.");

    /// <summary>Like <see cref="Of"/>, but returns <c>null</c> when no provider is in scope.</summary>
    public static TBloc? MaybeOf(BuildContext context)
        => context.DependOnInheritedWidgetOfExactType<BlocProviderScope<TBloc>>()?.Notifier;

    /// <summary>
    /// Returns the nearest provided bloc without subscribing to rebuilds.
    /// Use inside event handlers and callbacks.
    /// </summary>
    public static TBloc ReadOf(BuildContext context)
        => ReadMaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No BlocProvider<{typeof(TBloc).Name}> found in the widget tree.");

    /// <summary>Like <see cref="ReadOf"/>, but returns <c>null</c> when no provider is in scope.</summary>
    public static TBloc? ReadMaybeOf(BuildContext context)
        => context.GetInheritedWidgetOfExactType<BlocProviderScope<TBloc>>()?.Notifier;

    private sealed class BlocProviderState : State
    {
        private TBloc? _bloc;
        private bool _ownsBloc;

        private BlocProvider<TBloc> TypedWidget => (BlocProvider<TBloc>)StateWidget;

        public override void InitState()
        {
            base.InitState();
            ResolveInitialBloc();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);

            var previousWidget = (BlocProvider<TBloc>)oldWidget;
            var nextWidget = TypedWidget;

            if (!previousWidget.HasCreate && !nextWidget.HasCreate)
            {
                if (!ReferenceEquals(previousWidget.ExistingBloc, nextWidget.ExistingBloc))
                {
                    _bloc = nextWidget.ExistingBloc;
                }

                _ownsBloc = false;
                return;
            }

            if (previousWidget.HasCreate && !nextWidget.HasCreate)
            {
                DisposeOwnedBloc();
                _bloc = nextWidget.ExistingBloc;
                _ownsBloc = false;
                return;
            }

            if (!previousWidget.HasCreate && nextWidget.HasCreate)
            {
                _bloc = nextWidget.CreateBloc(Context);
                _ownsBloc = true;
            }

            // create->create keeps the current bloc instance to match flutter_bloc behavior.
        }

        public override Widget Build(BuildContext context)
        {
            return new BlocProviderScope<TBloc>(
                bloc: RequireBloc(),
                child: TypedWidget.Child);
        }

        public override void Dispose()
        {
            DisposeOwnedBloc();
            base.Dispose();
        }

        private void ResolveInitialBloc()
        {
            if (TypedWidget.HasCreate)
            {
                _bloc = TypedWidget.CreateBloc(Context);
                _ownsBloc = true;
                return;
            }

            _bloc = TypedWidget.ExistingBloc;
            _ownsBloc = false;
        }

        private TBloc RequireBloc()
            => _bloc ?? throw new InvalidOperationException($"BlocProvider<{typeof(TBloc).Name}> has not initialized a bloc.");

        private void DisposeOwnedBloc()
        {
            if (!_ownsBloc || _bloc is null)
            {
                return;
            }

            _bloc.Close();
            _ownsBloc = false;
        }
    }
}

internal sealed class BlocProviderScope<TBloc> : InheritedNotifier<TBloc> where TBloc : class, IBlocBase
{
    public BlocProviderScope(TBloc bloc, Widget child, Key? key = null) : base(bloc, child, key)
    {
    }
}
