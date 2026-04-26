// Dart parity source (reference): pub.dev/packages/bloc (approximate)

// TODO: Implement Plumix.Bloc — a port of Flutter's bloc package.
//
// ── OVERVIEW ──────────────────────────────────────────────────────────────────
//
// Flutter's bloc package separates UI from business logic using streams.
// Two core types:
//   • Cubit<TState>  — emits new states directly (no events)
//   • Bloc<TEvent, TState> — reacts to typed events and emits new states
//
// ── DESIGN DECISIONS TO RESOLVE ───────────────────────────────────────────────
//
// 1. Stream backend
//    Flutter bloc uses Dart streams. In .NET, choose between:
//    a) IObservable<T> (System.Reactive / Rx.NET) — most idiomatic for streams
//    b) System.Threading.Channels — lightweight, no external deps
//    c) Custom subject pattern (like ChangeNotifier but push-based)
//    Recommendation: IObservable<T> with an optional System.Reactive dep.
//
// 2. Async event handling
//    Flutter bloc supports async event handlers (EventHandler<TEvent, TState>).
//    In .NET this maps to Func<TEvent, IEmitter<TState>, CancellationToken, Task>.
//    Need to decide on concurrency semantics (sequential, concurrent, restartable).
//    See: BlocBase.on<TEvent>(EventHandler, transformer?)
//
// 3. BlocObserver
//    Flutter bloc has a global BlocObserver for logging/analytics.
//    In .NET: static Bloc.Observer property returning IBlocObserver.
//
// ── TYPES TO IMPLEMENT ────────────────────────────────────────────────────────
//
// Core:
//   TODO: IBlocBase<TState>        — shared interface (State, Stream, Close)
//   TODO: Cubit<TState>            — simple state emitter, extends ChangeNotifier or IListenable
//   TODO: Bloc<TEvent, TState>     — event-driven state machine
//   TODO: IEmitter<TState>         — callback passed into event handlers to emit states
//   TODO: IBlocObserver            — global hook: OnCreate, OnEvent, OnTransition, OnError, OnClose
//
// Widget layer:
//   TODO: BlocProvider<T>          — InheritedNotifier that provides a Bloc/Cubit
//   TODO: BlocBuilder<T, TState>   — rebuilds on state change (like Consumer for bloc)
//   TODO: BlocListener<T, TState>  — side-effect listener (does not rebuild)
//   TODO: BlocConsumer<T, TState>  — combines BlocBuilder + BlocListener
//   TODO: BlocSelector<T, TState, TSelected> — rebuilds only when selected slice changes
//   TODO: MultiBlocProvider        — stacks multiple BlocProviders (like MultiProvider)
//   TODO: RepositoryProvider<T>    — provides a plain repository (no streams, like Provider<T>)
//
// BuildContext extensions:
//   TODO: context.Read<T>()        — returns Bloc/Cubit without subscribing
//   TODO: context.Watch<T>()       — returns latest state and subscribes to rebuilds
//   TODO: context.Select<T, R>()   — subscribes to a derived slice of state
//
// ── TRANSITION / EVENT TYPES ──────────────────────────────────────────────────
//
//   TODO: Transition<TEvent, TState>  — { CurrentState, Event, NextState }
//   TODO: Change<TState>              — { CurrentState, NextState }
//   TODO: BlocUnhandledErrorException — wraps errors thrown inside event handlers
//
// ── CONCURRENCY TRANSFORMERS ──────────────────────────────────────────────────
//
//   TODO: EventTransformer<TEvent>    — Func<IObservable<TEvent>, IObservable<TEvent>>
//   TODO: Sequential()                — process one event at a time (default)
//   TODO: Concurrent()                — process all events in parallel
//   TODO: Restartable()               — cancel previous handler when new event arrives
//   TODO: Droppable()                 — ignore new events while one is in progress
