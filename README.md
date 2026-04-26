# Plumix.Packages

Ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix) — a Flutter-inspired UI framework for .NET.

## Packages

| Package | NuGet | Flutter equivalent | Status |
|---------|-------|--------------------|--------|
| `Plumix.Provider` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Provider)](https://www.nuget.org/packages/Plumix.Provider) | [provider](https://pub.dev/packages/provider) | alpha |
| `Plumix.Bloc` | [![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc) | [bloc](https://pub.dev/packages/bloc) | planned |

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

> **Status: planned.** See [Bloc.cs](src/Plumix.Bloc/Bloc.cs) for the full design TODO.

A port of Flutter's `bloc` package — stream-based state management with `Cubit<TState>` and `Bloc<TEvent, TState>`.

---

## Local development

The solution auto-detects the sibling `Plumix` repo and switches to a project reference when found:

```
~/
├── Plumix/          ← main framework repo
└── Plumix.Packages/ ← this repo
```

## Publishing

Packages are published to NuGet.org automatically when a `v*.*.*` tag is pushed.
Set the `NUGET_API_KEY` secret in repository settings.
