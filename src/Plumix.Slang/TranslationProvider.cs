// Dart parity source (reference): pub.dev/packages/slang_flutter — TranslationProvider (approximate)

using System.ComponentModel;
using System.Globalization;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Slang;

/// <summary>
/// Publishes the active translations to the subtree and rebuilds it whenever the culture changes.
///
/// Equivalent to slang_flutter's <c>TranslationProvider</c>. Without it, widgets that read the
/// generated <c>Instance.Root</c> directly keep showing the strings of the culture that was active
/// when they were last built — switching the locale would not repaint anything.
///
/// Place it above the app's navigator:
/// <code>
/// new TranslationProvider&lt;Strings&gt;(
///     child: new MyApp())
/// </code>
///
/// and read the translations through the context, so the reader is rebuilt on every switch:
/// <code>
/// var t = Translations&lt;Strings&gt;.Of(context);
/// LocaleSettings&lt;Strings&gt;.SetLocale(new CultureInfo("en-US"));
/// </code>
/// </summary>
/// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
public sealed class TranslationProvider<T> : StatefulWidget
    where T : class, ITranslations<T>
{
    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="child">The subtree that gains access to the translations.</param>
    /// <param name="key">The widget key.</param>
    public TranslationProvider(Widget child, Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    /// <summary>The subtree that gains access to the translations.</summary>
    public Widget Child { get; }

    /// <inheritdoc />
    public override State CreateState() => new TranslationProviderState<T>();
}

/// <summary>
/// Holds the subscription to the generated change source and republishes the scope on every switch.
/// </summary>
/// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
internal sealed class TranslationProviderState<T> : State
    where T : class, ITranslations<T>
{
    private INotifyPropertyChanged? _changeSource;

    /// <inheritdoc />
    public override void InitState()
    {
        base.InitState();
        _changeSource = T.ChangeSource;
        _changeSource.PropertyChanged += OnCultureChanged;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_changeSource is not null)
        {
            _changeSource.PropertyChanged -= OnCultureChanged;
            _changeSource = null;
        }

        base.Dispose();
    }

    /// <inheritdoc />
    public override Widget Build(BuildContext context)
    {
        var provider = (TranslationProvider<T>)StateWidget;

        return new TranslationScope<T>(
            translations: T.CurrentTranslations,
            culture: CultureInfo.CurrentUICulture,
            child: provider.Child);
    }

    // Slang.Net raises the event on whichever thread called SetCulture; SetState requires the UI
    // thread, so LocaleSettings<T>.SetLocale must be called from it (see the package README).
    private void OnCultureChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Mounted)
            SetState(() => { });
    }
}

/// <summary>
/// The inherited widget <see cref="TranslationProvider{T}"/> rebuilds; readers depend on it through
/// <see cref="Translations{T}"/>. Not meant to be constructed directly.
/// </summary>
/// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
public sealed class TranslationScope<T> : InheritedWidget
    where T : class, ITranslations<T>
{
    internal TranslationScope(T translations, CultureInfo culture, Widget child, Key? key = null)
        : base(key)
    {
        Translations = translations;
        Culture = culture;
        Child = child;
    }

    /// <summary>The translations tree for <see cref="Culture"/>.</summary>
    public T Translations { get; }

    /// <summary>The culture that was active when this scope was published.</summary>
    public CultureInfo Culture { get; }

    /// <summary>The subtree that gains access to the translations.</summary>
    public Widget Child { get; }

    /// <inheritdoc />
    public override Widget Build(BuildContext context) => Child;

    /// <inheritdoc />
    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var old = (TranslationScope<T>)oldWidget;

        return !ReferenceEquals(old.Translations, Translations)
               || !string.Equals(old.Culture.Name, Culture.Name, StringComparison.Ordinal);
    }
}
