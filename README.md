# Plumix.Packages

Ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix) — a Flutter-inspired UI framework for .NET.

## Packages

| Package | NuGet | Flutter equivalent | Status |
|---------|-------|--------------------|--------|
| `Plumix.Provider` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Provider)](https://www.nuget.org/packages/Plumix.Provider) | [provider](https://pub.dev/packages/provider) | alpha |
| `Plumix.Bloc` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc) | [bloc](https://pub.dev/packages/bloc) + [flutter_bloc](https://pub.dev/packages/flutter_bloc) | alpha |
| `Plumix.Slang` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Slang)](https://www.nuget.org/packages/Plumix.Slang) | [slang_flutter](https://pub.dev/packages/slang_flutter) | alpha |

All packages share one version line, taken from the release tag. The minimum supported
Plumix version is the `Plumix` dependency listed for each package on NuGet.

---

## Plumix.Provider

State management via `InheritedWidget` — a port of Flutter's `provider` package.

### Installation

```
dotnet add package Plumix.Provider
```

### Usage

#### 1. Declare a model

```csharp
using Plumix.Foundation;

public class CounterModel : ChangeNotifier
{
    public int Count { get; private set; }

    public void Increment()
    {
        Count++;
        NotifyListeners();
    }
}
```

#### 2. Provide it at the root

```csharp
using Plumix.Provider;

new ChangeNotifierProvider<CounterModel>(
    notifier: new CounterModel(),
    child: new MyApp())
```

Or combine multiple providers with `MultiProvider`:

```csharp
new MultiProvider(
    child: new MyApp(),
    providers:
    [
        child => new ChangeNotifierProvider<CounterModel>(new CounterModel(), child),
        child => new Provider<AppConfig>(AppConfig.Default, child),
    ])
```

#### 3. Read in a descendant

```csharp
// Subscribe — widget rebuilds when the notifier changes
var counter = context.Watch<CounterModel>();

// Read once — no rebuild subscription (use inside event handlers)
var counter = context.Read<CounterModel>();
counter.Increment();
```

#### 4. Consumer widget

```csharp
new Consumer<CounterModel>(
    builder: (context, counter) =>
        new Text(counter.Count.ToString()))
```

---

## Plumix.Bloc

State management for Plumix with `Cubit<TState>` and `Bloc<TEvent, TState>`, plus widget bindings from `flutter_bloc`: `BlocProvider`, `BlocBuilder`, `BlocListener`, `BlocConsumer`, and `BlocSelector`.

---

## Plumix.Slang

Context-based translations over [Slang.Net](https://www.nuget.org/packages/Slang.Net) — a port of `slang_flutter`.
`TranslationProvider<T>` publishes the active translations into the widget tree, so switching the
locale repaints everything that read them through `Translations<T>.Of(context)`.

### Usage

#### 1. Opt the generated class in

```csharp
[Translations(InputFileName = "strings")]
public partial class Strings : ITranslations<Strings>
{
    static Strings ITranslations<Strings>.CurrentTranslations => Instance.Root;
    static INotifyPropertyChanged ITranslations<Strings>.ChangeSource => Instance;
}
```

#### 2. Wrap the app and read from the context

```csharp
new TranslationProvider<Strings>(child: new MyApp())

var t = Translations<Strings>.Of(context);   // rebuilds on every switch
LocaleSettings<Strings>.SetLocaleRaw("en-US");
```

See [`src/Plumix.Slang/README.md`](src/Plumix.Slang/README.md) for the full API.

---

## Local development

The solution auto-detects the sibling `Plumix` repo and switches to a project reference when found:

```
<workspace>/
├── Plumix/          ← main framework repo
└── Plumix.Packages/ ← this repo
```

To force the published NuGet package instead, pass `-p:UsePlumixLocal=false`
(add `-p:PlumixVersion=<version>` to try a specific Plumix release).
CI always builds against the pinned NuGet version (`PlumixVersion` in `src/Directory.Packages.props`),
re-tests against the Plumix version declared in the packed nuspecs, and runs a non-blocking
canary build against the newest Plumix on NuGet.

Build and test:

```
dotnet build src/Plumix.Packages.slnx -c Release
dotnet test src/Plumix.Packages.slnx -c Release
```

## Publishing

Packages are published to NuGet.org automatically when a `v*.*.*` tag is pushed
(the version is taken from the tag; symbol packages are pushed alongside).
Set the `NUGET_API_KEY` secret in repository settings.
