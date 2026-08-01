// Dart parity source (reference): pub.dev/packages/slang_flutter — context.t (approximate)

using System.Globalization;
using Plumix.Widgets;

namespace Plumix.Slang;

/// <summary>
/// Shorthands for reading translations off a <see cref="BuildContext"/>.
/// </summary>
public static class BuildContextExtensions
{
    /// <summary>
    /// Returns the active translations and subscribes the calling widget to culture changes —
    /// the terse form of <see cref="Translations{T}.Of"/>, matching slang_flutter's <c>context.t</c>.
    /// </summary>
    /// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
    /// <param name="context">The build context of the reading widget.</param>
    public static T Translations<T>(this BuildContext context)
        where T : class, ITranslations<T>
        => Plumix.Slang.Translations<T>.Of(context);

    /// <summary>
    /// Returns the active translations without subscribing to culture changes —
    /// the terse form of <see cref="Translations{T}.ReadOf"/>.
    /// </summary>
    /// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
    /// <param name="context">The build context of the reading widget.</param>
    public static T ReadTranslations<T>(this BuildContext context)
        where T : class, ITranslations<T>
        => Plumix.Slang.Translations<T>.ReadOf(context);

    /// <summary>
    /// Returns the culture currently published by the nearest <see cref="TranslationProvider{T}"/>
    /// and subscribes the calling widget to further changes.
    /// </summary>
    /// <typeparam name="T">The Slang.Net generated translations class.</typeparam>
    /// <param name="context">The build context of the reading widget.</param>
    public static CultureInfo TranslationCulture<T>(this BuildContext context)
        where T : class, ITranslations<T>
        => Plumix.Slang.Translations<T>.CultureOf(context);
}
