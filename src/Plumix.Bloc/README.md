# Plumix.Bloc

[![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc)

Stream-based state management for [Plumix](https://github.com/Plumix-Net/Plumix) — a port of Flutter's [bloc](https://pub.dev/packages/bloc) package.

> **Status: planned.** API design is in progress. See [Bloc.cs](Bloc.cs) for the full design spec.

## Planned API

### Core types

| Plumix.Bloc | Flutter equivalent | Description |
|---|---|---|
| `Cubit<TState>` | `Cubit<State>` | Emits new states directly (no events) |
| `Bloc<TEvent, TState>` | `Bloc<Event, State>` | Reacts to typed events and emits states |

### Widget layer

| Plumix.Bloc | Flutter equivalent | Description |
|---|---|---|
| `BlocProvider<T>` | `BlocProvider<T>` | Provides a Bloc/Cubit to the widget subtree |
| `BlocBuilder<T, TState>` | `BlocBuilder<T, S>` | Rebuilds on state change |
| `BlocListener<T, TState>` | `BlocListener<T, S>` | Side-effect listener, does not rebuild |
| `BlocConsumer<T, TState>` | `BlocConsumer<T, S>` | Combines builder + listener |
| `BlocSelector<T, TState, R>` | `BlocSelector<T, S, R>` | Rebuilds only when a derived slice changes |
| `MultiBlocProvider` | `MultiBlocProvider` | Stacks multiple `BlocProvider`s |

### BuildContext extensions

```csharp
// Read the bloc without subscribing
var counter = context.Read<CounterBloc>();

// Read the latest state and subscribe
var state = context.Watch<CounterBloc, CounterState>();

// Select a derived slice
var count = context.Select<CounterBloc, CounterState, int>(s => s.Count);
```

### Event transformers

```csharp
// Process events sequentially (default)
Sequential()

// Cancel in-flight handler when a new event arrives
Restartable()

// Ignore new events while one is in progress
Droppable()

// Process all events concurrently
Concurrent()
```

## See also

- [Plumix](https://github.com/Plumix-Net/Plumix) — the core framework
- [Plumix.Provider](https://www.nuget.org/packages/Plumix.Provider) — simpler state management via `ChangeNotifier`
