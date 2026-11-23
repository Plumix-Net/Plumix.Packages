# Plumix.Bloc

[![NuGet](https://img.shields.io/nuget/v/Plumix.Bloc)](https://www.nuget.org/packages/Plumix.Bloc)

Widgets that make [Plumix.Bloc.Core](https://www.nuget.org/packages/Plumix.Bloc.Core) (`Cubit`, `Bloc`) work with
[Plumix](https://github.com/Plumix-Net/Plumix) — a port of Flutter's [flutter_bloc](https://pub.dev/packages/flutter_bloc)
package. Types stay in the `Plumix.Bloc` namespace, so one `using Plumix.Bloc;` covers both packages,
and referencing this package brings the core with it.

The package version is the version of `flutter_bloc` it ports; `-preview.N` until the port matches it in full.

## Installation

```bash
dotnet add package Plumix.Bloc
```

## Widgets

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

- [Plumix.Bloc.Core](https://www.nuget.org/packages/Plumix.Bloc.Core) — `Cubit`, `Bloc` and the rest of the core (port of `bloc`)
- [Plumix.Provider](https://www.nuget.org/packages/Plumix.Provider) — simpler notifier-based state management
