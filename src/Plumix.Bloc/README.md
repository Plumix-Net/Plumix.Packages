# Plumix.Bloc

[![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc)

Stream-based state management for [Plumix](https://github.com/Plumix-Net/Plumix) — a port of Flutter's [bloc](https://pub.dev/packages/bloc) package.
Includes widget bindings inspired by [flutter_bloc](https://pub.dev/packages/flutter_bloc): `BlocProvider`, `BlocBuilder`, `BlocListener`, `BlocConsumer`, and `BlocSelector`.

## Installation

```bash
dotnet add package Plumix.Bloc
```

## Core API

| Plumix.Bloc | Flutter equivalent | Description |
|---|---|---|
| `Cubit<TState>` | `Cubit<State>` | Emits new states directly |
| `Bloc<TEvent, TState>` | `Bloc<Event, State>` | Event-driven state management |
| `Change<TState>` | `Change<State>` | State change payload |
| `Transition<TEvent, TState>` | `Transition<Event, State>` | Event-to-state transition payload |
| `EventTransformers` | `bloc_concurrency`-style behavior | `Sequential`, `Concurrent`, `Restartable`, `Droppable` |

| Plumix.Bloc | Flutter equivalent | Description |
|---|---|---|
| `BlocProvider<T>` | `BlocProvider<T>` | Provides a Bloc/Cubit to the widget subtree |
| `RepositoryProvider<T>` | `RepositoryProvider<T>` | Provides plain repositories/values |
| `BlocBuilder<T, TState>` | `BlocBuilder<T, S>` | Rebuilds UI on state change |
| `BlocListener<T, TState>` | `BlocListener<T, S>` | Side-effect listener, does not rebuild |
| `BlocConsumer<T, TState>` | `BlocConsumer<T, S>` | Combines builder + listener |
| `BlocSelector<T, TState, R>` | `BlocSelector<T, S, R>` | Rebuilds only when a derived slice changes |
| `MultiBlocProvider` | `MultiBlocProvider` | Stacks multiple `BlocProvider`s |
| `MultiRepositoryProvider` | `MultiRepositoryProvider` | Stacks multiple `RepositoryProvider`s |

## Usage

### 1. Define a cubit

```csharp
using Plumix.Bloc;

public sealed class CounterCubit : Cubit<int>
{
    public CounterCubit() : base(0)
    {
    }

    public void Increment() => Emit(State + 1);
}
```

### 2. Provide it

```csharp
new BlocProvider<CounterCubit>(
    bloc: new CounterCubit(),
    child: new AppRoot())
```

Create-and-own variant (auto-closes on unmount):

```csharp
new BlocProvider<CounterCubit>(
    create: _ => new CounterCubit(),
    child: new AppRoot())
```

### 3. Build UI from state

```csharp
new BlocBuilder<CounterCubit, int>(
    builder: (context, count) =>
        new Text($"Count: {count}"))
```

### 4. Read and update in callbacks

```csharp
context.Read<CounterCubit>().Increment();
```

## See also

- [Plumix](https://github.com/Plumix-Net/Plumix) — the core framework
- [Plumix.Provider](https://www.nuget.org/packages/Plumix.Provider) — simpler notifier-based state management
