// Dart parity source (reference): pub.dev/packages/slang — LocaleSettings (approximate)

using System.Globalization;

namespace Plumix.Slang;

/// <summary>
/// Runtime locale control for a Slang.Net generated translations class.
///
/// Equivalent to slang's <c>LocaleSettings</c>. Every switch goes through the generated
/// <c>SetCulture</c>, so a <see cref="TranslationProvider{T}"/> mounted above the app rebuilds
/// the subtree and the new strings appear immediately.
///
/// Call these from the UI thread — the rebuild they trigger runs synchronously on the caller.
/// </summary>
/// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
public static class LocaleSettings<T>
    where T : class, ITranslations<T>
{
    /// <summary>The culture currently used to resolve translations.</summary>
    public static CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    /// <summary>The translations tree for <see cref="CurrentCulture"/>, without a widget context.</summary>
    public static T CurrentTranslations => T.CurrentTranslations;

    /// <summary>Every culture the generated class carries translations for.</summary>
    public static IReadOnlyList<CultureInfo> SupportedCultures => T.SupportedCultures;

    /// <summary>The culture used when nothing else matches.</summary>
    public static CultureInfo BaseCulture => T.BaseCulture;

    /// <summary>
    /// Switches to <paramref name="culture"/>, or to the closest supported culture
    /// (see <see cref="Resolve"/>).
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <param name="uiOnly">
    /// When <c>true</c> (the default), only <see cref="CultureInfo.CurrentUICulture"/> changes and
    /// number/date formatting keeps following the system culture — this mirrors slang, which only
    /// switches translations. Pass <c>false</c> to move <see cref="CultureInfo.CurrentCulture"/> too.
    /// </param>
    /// <returns>The culture that ended up active.</returns>
    public static CultureInfo SetLocale(CultureInfo culture, bool uiOnly = true)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var resolved = Resolve(culture);
        T.SetCulture(resolved, uiOnly);
        return resolved;
    }

    /// <summary>
    /// Like <see cref="SetLocale"/>, but takes a culture name such as <c>"en-US"</c> or <c>"ru"</c>.
    /// </summary>
    /// <param name="cultureName">The requested culture name.</param>
    /// <param name="uiOnly">See <see cref="SetLocale"/>.</param>
    /// <returns>The culture that ended up active.</returns>
    public static CultureInfo SetLocaleRaw(string cultureName, bool uiOnly = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);

        return SetLocale(CultureInfo.GetCultureInfo(cultureName), uiOnly);
    }

    /// <summary>
    /// Switches to the closest supported culture for the device's own UI culture.
    /// </summary>
    /// <param name="uiOnly">See <see cref="SetLocale"/>.</param>
    /// <returns>The culture that ended up active.</returns>
    public static CultureInfo UseDeviceLocale(bool uiOnly = true)
        => SetLocale(CultureInfo.InstalledUICulture, uiOnly);

    /// <summary>
    /// Maps a requested culture onto a supported one: exact name first, then any culture sharing
    /// the language (<c>"de-AT"</c> → <c>"de-DE"</c>), and finally <see cref="BaseCulture"/>.
    /// This is the same fallback the generated <c>Instance.Root</c> applies.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    public static CultureInfo Resolve(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var supported = T.SupportedCultures;

        foreach (var candidate in supported)
        {
            if (string.Equals(candidate.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        foreach (var candidate in supported)
        {
            if (string.Equals(
                    candidate.TwoLetterISOLanguageName,
                    culture.TwoLetterISOLanguageName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return T.BaseCulture;
    }

    /// <summary>Whether <paramref name="culture"/> resolves to itself rather than to a fallback.</summary>
    /// <param name="culture">The culture to check.</param>
    public static bool IsSupported(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        foreach (var candidate in T.SupportedCultures)
        {
            if (string.Equals(candidate.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
