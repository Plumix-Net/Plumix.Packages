# Plumix.Packages

Ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix) — a Flutter-inspired UI framework for .NET.

## Packages

| Package | NuGet | Flutter equivalent | Status |
|---------|-------|--------------------|--------|
| `Plumix.Provider` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Provider)](https://www.nuget.org/packages/Plumix.Provider) | [provider](https://pub.dev/packages/provider) | alpha |
| `Plumix.Bloc` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc) | [bloc](https://pub.dev/packages/bloc) + [flutter_bloc](https://pub.dev/packages/flutter_bloc) | alpha |

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

## Local development

The solution auto-detects the sibling `Plumix` repo and switches to a project reference when found:

```
<workspace>/
├── Plumix/          ← main framework repo
└── Plumix.Packages/ ← this repo
```

To force the published NuGet package instead, pass `-p:UsePlumixLocal=false`.
CI always builds against the pinned NuGet version from `src/Directory.Packages.props`.

Build and test:

```
dotnet build src/Plumix.Packages.slnx -c Release
dotnet test src/Plumix.Packages.slnx -c Release
```

## Publishing

Packages are published to NuGet.org automatically when a `v*.*.*` tag is pushed
(the version is taken from the tag; symbol packages are pushed alongside).
Set the `NUGET_API_KEY` secret in repository settings.
