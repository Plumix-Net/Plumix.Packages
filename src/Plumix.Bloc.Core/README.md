# Plumix.Bloc.Core

[![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc.Core)](https://www.nuget.org/packages/Plumix.Bloc.Core)

Stream-based state management for .NET — a port of Dart's [bloc](https://pub.dev/packages/bloc) package.
Like the Dart original it has no UI dependency (not even on Plumix); the widget bindings (`BlocProvider`,
`BlocBuilder`, `BlocListener`, ...) are in [Plumix.Bloc](https://www.nuget.org/packages/Plumix.Bloc),
the port of `flutter_bloc`. Types use the `Plumix.Bloc` namespace in both packages.

The package version is the version of `bloc` it ports; `-preview.N` until the port matches it in full.

## Installation

```bash
dotnet add package Plumix.Bloc.Core
```

In a Plumix app, add `Plumix.Bloc` instead — it brings this package with it.

## Core API

| Plumix.Bloc.Core | Flutter equivalent | Description |
|---|---|---|
| `Cubit<TState>` | `Cubit<State>` | Emits new states directly |
| `Bloc<TEvent, TState>` | `Bloc<Event, State>` | Event-driven state management |
| `Change<TState>` | `Change<State>` | State change payload |
| `Transition<TEvent, TState>` | `Transition<Event, State>` | Event-to-state transition payload |
| `EventTransformers` | `bloc_concurrency`-style behavior | `Sequential`, `Concurrent`, `Restartable`, `Droppable` |

## Usage

```csharp
using Plumix.Bloc;

public sealed class CounterCubit : Cubit<int>
{
    public CounterCubit() : base(0)
    {
    }

    public void Increment() => Emit(State + 1);
}

using var cubit = new CounterCubit();
using var subscription = cubit.Stream.Subscribe(count => Console.WriteLine(count));
cubit.Increment();
```

## See also

- [Plumix.Bloc](https://www.nuget.org/packages/Plumix.Bloc) — widget bindings (port of `flutter_bloc`)
- [Plumix](https://github.com/Plumix-Net/Plumix) — the Flutter-inspired UI framework
