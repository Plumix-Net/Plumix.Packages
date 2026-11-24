# Plumix.Packages

Ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix) — a Flutter-inspired UI framework for .NET.

## Packages

| Package | NuGet | Flutter equivalent | Status |
|---------|-------|--------------------|--------|
| `Plumix.Provider` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Provider)](https://www.nuget.org/packages/Plumix.Provider) | [provider](https://pub.dev/packages/provider) | beta |
| `Plumix.Bloc` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc) | [flutter_bloc](https://pub.dev/packages/flutter_bloc) | beta |
| `Plumix.Bloc.Core` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc.Core)](https://www.nuget.org/packages/Plumix.Bloc.Core) | [bloc](https://pub.dev/packages/bloc) | beta |
| `Plumix.Slang` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Slang)](https://www.nuget.org/packages/Plumix.Slang) | [slang_flutter](https://pub.dev/packages/slang_flutter) | beta |
| `Plumix.GoogleSignIn` | [![NuGet](https://img.shields.io/nuget/v/Plumix.GoogleSignIn)](https://www.nuget.org/packages/Plumix.GoogleSignIn) | [google_sign_in](https://pub.dev/packages/google_sign_in) | preview |
| `Plumix.SignInWithApple` | [![NuGet](https://img.shields.io/nuget/v/Plumix.SignInWithApple)](https://www.nuget.org/packages/Plumix.SignInWithApple) | [sign_in_with_apple](https://pub.dev/packages/sign_in_with_apple) | preview |
| `Plumix.InAppPurchase` | [![NuGet](https://img.shields.io/nuget/v/Plumix.InAppPurchase)](https://www.nuget.org/packages/Plumix.InAppPurchase) | [in_app_purchase](https://pub.dev/packages/in_app_purchase) | preview |

Each package carries the version of the pub package it ports (`Plumix.Provider 6.1.5`, `Plumix.Bloc 9.1.1`,
`Plumix.Bloc.Core 9.2.1`, `Plumix.Slang 4.19.0`); a `-preview.N` suffix means the port has not reached full
parity with that release yet ([versioning policy](https://github.com/Plumix-Net/Plumix/blob/main/docs/VERSIONING.md)).
The minimum supported Plumix version is the `Plumix` dependency listed for each package on NuGet.

The last three are **platform plugins**: the OS does the work, so they have no Plumix dependency and work in any .NET
Android / iOS app. One package carries `net11.0` (the API; reports the platform as unsupported), `net11.0-android`
and `net11.0-ios` — reference it from a shared `net11.0` project and the heads get the platform build. Their usage is
in each package's README: [GoogleSignIn](src/Plumix.GoogleSignIn/README.md),
[SignInWithApple](src/Plumix.SignInWithApple/README.md), [InAppPurchase](src/Plumix.InAppPurchase/README.md).

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

## Plumix.Bloc and Plumix.Bloc.Core

State management with `Cubit<TState>` and `Bloc<TEvent, TState>`, split like the Dart packages:
`Plumix.Bloc.Core` ports `bloc` and has no UI dependency; `Plumix.Bloc` ports `flutter_bloc` —
`BlocProvider`, `BlocBuilder`, `BlocListener`, `BlocConsumer`, `BlocSelector`, `RepositoryProvider` —
and brings the core with it. Both use the `Plumix.Bloc` namespace; a Plumix app references `Plumix.Bloc`.

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

Versions live in `src/Versions.props`. Packages are published to NuGet.org automatically when a
release tag is pushed — `provider-v*`, `bloc-core-v*`, `bloc-v*` or `slang-v*`, matching
`Versions.props` (symbol packages are pushed alongside). Publishing uses NuGet trusted publishing
(OIDC); the only secret is `NUGET_USER`.
