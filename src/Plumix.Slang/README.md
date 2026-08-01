# Plumix.Slang

[![NuGet](https://img.shields.io/nuget/v/Plumix.Slang)](https://www.nuget.org/packages/Plumix.Slang)

Context-based translations for [Plumix](https://github.com/Plumix-Net/Plumix) — a port of Flutter's [slang_flutter](https://pub.dev/packages/slang_flutter) widget layer on top of [Slang.Net](https://www.nuget.org/packages/Slang.Net).

Slang.Net generates a static translations class, so reading `Strings.Instance.Root` inside `Build` gives you the strings of whatever culture was active at that moment — and nothing repaints when the culture changes later. `TranslationProvider` puts the active translations into the widget tree, so a locale switch rebuilds every widget that read them.

## Installation

```bash
dotnet add package Plumix.Slang
```

## Core API

| Plumix.Slang | Flutter equivalent | Description |
|---|---|---|
| `ITranslations<TSelf>` | generated `AppLocaleUtils` glue | Bridge to the Slang.Net generated class |
| `TranslationProvider<T>` | `TranslationProvider` | Publishes translations, rebuilds the subtree on switch |
| `Translations<T>.Of(context)` | `Translations.of(context)` | Reads translations and subscribes to switches |
| `Translations<T>.ReadOf(context)` | — | Reads without subscribing (event handlers) |
| `LocaleSettings<T>` | `LocaleSettings` | `SetLocale`, `SetLocaleRaw`, `UseDeviceLocale`, `CurrentCulture` |
| `context.Translations<T>()` | `context.t` | Shorthand for `Translations<T>.Of(context)` |

## Usage

### 1. Implement the bridge on the generated class

Slang.Net emits its API as static members, so the contract uses static abstract members — no
reflection, trimming/AOT safe. Only two members need glue; the rest are already generated with
matching signatures:

```csharp
using System.ComponentModel;
using Plumix.Slang;
using Slang;

[Translations(InputFileName = "strings")]
public partial class Strings : ITranslations<Strings>
{
    static Strings ITranslations<Strings>.CurrentTranslations => Instance.Root;
    static INotifyPropertyChanged ITranslations<Strings>.ChangeSource => Instance;
}
```

### 2. Wrap the app

```csharp
new TranslationProvider<Strings>(
    child: new MyApp())
```

### 3. Read translations through the context

```csharp
public override Widget Build(BuildContext context)
{
    var t = Translations<Strings>.Of(context);   // or context.Translations<Strings>()

    return new Text(t.App.Title);
}
```

Widgets that keep reading `Strings.Instance.Root` still work — they just will not repaint on a
switch. Anything that should follow the locale has to go through the context.

### 4. Switch the locale

```csharp
LocaleSettings<Strings>.SetLocale(new CultureInfo("en-US"));
LocaleSettings<Strings>.SetLocaleRaw("ru");
LocaleSettings<Strings>.UseDeviceLocale();
```

Unsupported cultures fall back the same way the generated `Instance.Root` does: exact name, then
any culture with the same language, then `BaseCulture`. `LocaleSettings<T>.Resolve` exposes that
mapping, `IsSupported` checks for an exact match.

By default only `CultureInfo.CurrentUICulture` moves, so number and date formatting keeps following
the system — pass `uiOnly: false` to switch `CultureInfo.CurrentCulture` as well.

Call these from the UI thread: the rebuild they trigger runs synchronously on the caller.

## See also

- [Plumix](https://github.com/Plumix-Net/Plumix) — the core framework
- [Slang.Net](https://www.nuget.org/packages/Slang.Net) — the translations source generator
