// Dart parity source (reference): pub.dev/packages/slang_flutter — Translations.of(context) (approximate)

using System.Globalization;
using Plumix.Widgets;

namespace Plumix.Slang;

/// <summary>
/// Context-based access to the translations published by <see cref="TranslationProvider{T}"/>.
///
/// Equivalent to slang_flutter's <c>Translations.of(context)</c>.
/// </summary>
/// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
public static class Translations<T>
    where T : class, ITranslations<T>
{
    /// <summary>
    /// Returns the active translations and subscribes the calling widget to culture changes.
    /// Throws when no <see cref="TranslationProvider{T}"/> is in scope.
    /// </summary>
    /// <param name="context">The build context of the reading widget.</param>
    public static T Of(BuildContext context)
        => MaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No TranslationProvider<{typeof(T).Name}> found in the widget tree. " +
               $"Wrap the app in a TranslationProvider<{typeof(T).Name}> to switch locales at runtime.");

    /// <summary>Like <see cref="Of"/>, but returns <c>null</c> when no provider is in scope.</summary>
    /// <param name="context">The build context of the reading widget.</param>
    public static T? MaybeOf(BuildContext context)
        => context.DependOnInheritedWidgetOfExactType<TranslationScope<T>>()?.Translations;

    /// <summary>
    /// Returns the active translations without subscribing to culture changes.
    /// Useful in event handlers, where a rebuild subscription would be pointless.
    /// </summary>
    /// <param name="context">The build context of the reading widget.</param>
    public static T ReadOf(BuildContext context)
        => ReadMaybeOf(context)
           ?? throw new InvalidOperationException(
               $"No TranslationProvider<{typeof(T).Name}> found in the widget tree.");

    /// <summary>Like <see cref="ReadOf"/>, but returns <c>null</c> when no provider is in scope.</summary>
    /// <param name="context">The build context of the reading widget.</param>
    public static T? ReadMaybeOf(BuildContext context)
        => context.GetInheritedWidgetOfExactType<TranslationScope<T>>()?.Translations;

    /// <summary>
    /// Returns the culture the nearest provider currently publishes and subscribes the calling
    /// widget to further changes. Throws when no provider is in scope.
    /// </summary>
    /// <param name="context">The build context of the reading widget.</param>
    public static CultureInfo CultureOf(BuildContext context)
        => context.DependOnInheritedWidgetOfExactType<TranslationScope<T>>()?.Culture
           ?? throw new InvalidOperationException(
               $"No TranslationProvider<{typeof(T).Name}> found in the widget tree.");
}
