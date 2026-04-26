# Plumix.Provider

[![NuGet](https://img.shields.io/nuget/v/Plumix.Provider)](https://www.nuget.org/packages/Plumix.Provider)

State management for [Plumix](https://github.com/Plumix-Net/Plumix) via `InheritedWidget` — a port of Flutter's [provider](https://pub.dev/packages/provider) package.

## Installation

```
dotnet add package Plumix.Provider
```

## Core concepts

| Plumix.Provider | Flutter equivalent | Description |
|---|---|---|
| `ChangeNotifierProvider<T>` | `ChangeNotifierProvider<T>` | Provides a `ChangeNotifier`, rebuilds descendants on change |
| `Provider<T>` | `Provider<T>` | Provides an immutable value |
| `Consumer<T>` | `Consumer<T>` | Widget that rebuilds when the notifier changes |
| `Consumer2<TA, TB>` | `Consumer2<A, B>` | Subscribes to two notifiers |
| `MultiProvider` | `MultiProvider` | Stacks multiple providers without nesting |
| `context.Watch<T>()` | `context.watch<T>()` | Subscribe and read (use in `Build`) |
| `context.Read<T>()` | `context.read<T>()` | Read without subscribing (use in callbacks) |

## Usage

### 1. Declare a model

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

### 2. Provide at the root

```csharp
using Plumix.Provider;

new ChangeNotifierProvider<CounterModel>(
    notifier: new CounterModel(),
    child: new MyApp())
```

Combine multiple providers with `MultiProvider`:

```csharp
new MultiProvider(
    child: new MyApp(),
    providers:
    [
        child => new ChangeNotifierProvider<CounterModel>(new CounterModel(), child),
        child => new Provider<AppConfig>(AppConfig.Default, child),
    ])
```

### 3. Read in a descendant

```csharp
// In Build — subscribes, widget rebuilds on change
var counter = context.Watch<CounterModel>();

// In a callback — reads once, no rebuild subscription
context.Read<CounterModel>().Increment();
```

### 4. Consumer widget

```csharp
new Consumer<CounterModel>(
    builder: (context, counter) =>
        new Text(counter.Count.ToString()))
```

### 5. Plain value provider

```csharp
// Provide
new Provider<AppConfig>(value: AppConfig.Default, child: ...)

// Read (subscribes)
var config = context.WatchValue<AppConfig>();

// Read without subscribing
var config = context.ReadValue<AppConfig>();
```

## See also

- [Plumix](https://github.com/Plumix-Net/Plumix) — the core framework
- [Plumix.Bloc](https://www.nuget.org/packages/Plumix.Bloc) — stream-based state management (planned)
