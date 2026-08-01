using System.ComponentModel;
using System.Globalization;
using Xunit;

// Dart parity source (reference): pub.dev/packages/slang — LocaleSettings fallback behavior
//
// Widget-tree behavior (TranslationProvider rebuilds on a switch) is not covered here:
// mounting a tree requires internal Plumix.Element APIs that are not exposed to external
// assemblies. These tests cover the publicly observable locale-resolution contracts.

namespace Plumix.Slang.Tests;

/// <summary>Stand-in for a Slang.Net generated translations class.</summary>
public sealed class FakeTranslations : ITranslations<FakeTranslations>, INotifyPropertyChanged
{
    private static readonly CultureInfo EnUs = new("en-US");
    private static readonly CultureInfo RuRu = new("ru-RU");
    private static readonly FakeTranslations Shared = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public static CultureInfo? LastRequestedCulture { get; private set; }

    public static bool LastRequestedUiOnly { get; private set; }

    public static FakeTranslations CurrentTranslations => Shared;

    public static INotifyPropertyChanged ChangeSource => Shared;

    public static CultureInfo BaseCulture => RuRu;

    public static IReadOnlyList<CultureInfo> SupportedCultures => [EnUs, RuRu];

    public static void SetCulture(CultureInfo culture, bool uiOnly = false)
    {
        LastRequestedCulture = culture;
        LastRequestedUiOnly = uiOnly;
        Shared.PropertyChanged?.Invoke(Shared, new PropertyChangedEventArgs("Root"));
    }
}

public sealed class LocaleSettingsTests
{
    [Fact]
    public void Resolve_ExactNameWins()
    {
        var resolved = LocaleSettings<FakeTranslations>.Resolve(new CultureInfo("en-US"));

        Assert.Equal("en-US", resolved.Name);
    }

    [Fact]
    public void Resolve_FallsBackToSameLanguage()
    {
        var resolved = LocaleSettings<FakeTranslations>.Resolve(new CultureInfo("en-GB"));

        Assert.Equal("en-US", resolved.Name);
    }

    [Fact]
    public void Resolve_FallsBackToBaseCultureWhenLanguageIsUnknown()
    {
        var resolved = LocaleSettings<FakeTranslations>.Resolve(new CultureInfo("fr-FR"));

        Assert.Equal(LocaleSettings<FakeTranslations>.BaseCulture.Name, resolved.Name);
    }

    [Fact]
    public void IsSupported_OnlyMatchesExactNames()
    {
        Assert.True(LocaleSettings<FakeTranslations>.IsSupported(new CultureInfo("ru-RU")));
        Assert.False(LocaleSettings<FakeTranslations>.IsSupported(new CultureInfo("ru")));
    }

    [Fact]
    public void SetLocale_PassesResolvedCultureToGeneratedSetter()
    {
        var applied = LocaleSettings<FakeTranslations>.SetLocale(new CultureInfo("en-GB"));

        Assert.Equal("en-US", applied.Name);
        Assert.Equal("en-US", FakeTranslations.LastRequestedCulture?.Name);
    }

    [Fact]
    public void SetLocale_DefaultsToUiOnly()
    {
        LocaleSettings<FakeTranslations>.SetLocale(new CultureInfo("ru-RU"));
        Assert.True(FakeTranslations.LastRequestedUiOnly);

        LocaleSettings<FakeTranslations>.SetLocale(new CultureInfo("ru-RU"), uiOnly: false);
        Assert.False(FakeTranslations.LastRequestedUiOnly);
    }

    [Fact]
    public void SetLocaleRaw_AcceptsCultureNames()
    {
        var applied = LocaleSettings<FakeTranslations>.SetLocaleRaw("ru");

        Assert.Equal("ru-RU", applied.Name);
    }

    [Fact]
    public void SetLocale_NotifiesTheChangeSource()
    {
        var notifications = 0;
        void OnChanged(object? sender, PropertyChangedEventArgs e) => notifications++;

        FakeTranslations.ChangeSource.PropertyChanged += OnChanged;
        try
        {
            LocaleSettings<FakeTranslations>.SetLocale(new CultureInfo("en-US"));
        }
        finally
        {
            FakeTranslations.ChangeSource.PropertyChanged -= OnChanged;
        }

        Assert.Equal(1, notifications);
    }

    [Fact]
    public void SetLocale_RejectsNull()
        => Assert.Throws<ArgumentNullException>(
            () => LocaleSettings<FakeTranslations>.SetLocale(null!));

    [Fact]
    public void SetLocaleRaw_RejectsBlankNames()
        => Assert.Throws<ArgumentException>(
            () => LocaleSettings<FakeTranslations>.SetLocaleRaw("  "));
}
