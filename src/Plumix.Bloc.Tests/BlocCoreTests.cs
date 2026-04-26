using System.Diagnostics;
using Xunit;

// Dart parity source (reference): pub.dev/packages/bloc; pub.dev/packages/flutter_bloc (core behavior regression tests)

namespace Plumix.Bloc.Tests;

public sealed class BlocCoreTests
{
    [Fact]
    public void Cubit_Emit_UpdatesStateAndNotifiesListeners()
    {
        using var cubit = new TestCounterCubit();

        var listenerNotifications = 0;
        var streamValues = new List<int>();

        cubit.AddListener(() => listenerNotifications += 1);
        using var subscription = cubit.Stream.Subscribe(value => streamValues.Add(value));

        cubit.Increment();
        cubit.Increment();
        cubit.EmitSameValue();

        Assert.Equal(2, cubit.State);
        Assert.Equal(2, listenerNotifications);
        Assert.Equal([1, 2], streamValues);
    }

    [Fact]
    public void Cubit_Close_DisposesAndPreventsFurtherEmits()
    {
        using var cubit = new TestCounterCubit();
        cubit.Close();

        Assert.True(cubit.IsClosed);
        Assert.Throws<ObjectDisposedException>(() => cubit.Increment());
        Assert.Throws<ObjectDisposedException>(() => cubit.AddListener(static () => { }));
    }

    [Fact]
    public async Task Bloc_ProcessesEventsAndUpdatesState()
    {
        using var bloc = new CounterBloc();

        bloc.Add(new AddEvent(2));
        bloc.Add(new AddEvent(3));

        await WaitUntilAsync(() => bloc.State == 5);
        Assert.Equal(5, bloc.State);
    }

    [Fact]
    public async Task EventTransformers_Sequential_ProcessesEventsOneByOne()
    {
        using var bloc = new TimedSetStateBloc();
        var states = new List<int>();

        using var subscription = bloc.Stream.Subscribe(value => states.Add(value));

        bloc.Add(new TimedSetStateEvent(1, DelayMs: 120));
        bloc.Add(new TimedSetStateEvent(2, DelayMs: 10));

        await WaitUntilAsync(() => states.Count == 2);
        Assert.Equal([1, 2], states);
    }

    [Fact]
    public async Task EventTransformers_Concurrent_ProcessesEventsInParallel()
    {
        using var bloc = new TimedSetStateBloc(transformer: EventTransformers.Concurrent<TimedSetStateEvent>());
        var states = new List<int>();

        using var subscription = bloc.Stream.Subscribe(value => states.Add(value));

        bloc.Add(new TimedSetStateEvent(1, DelayMs: 120));
        bloc.Add(new TimedSetStateEvent(2, DelayMs: 10));

        await WaitUntilAsync(() => states.Count == 2);
        Assert.Equal([2, 1], states);
    }

    [Fact]
    public async Task EventTransformers_Restartable_CancelsPreviousInFlightHandler()
    {
        using var bloc = new TimedSetStateBloc(transformer: EventTransformers.Restartable<TimedSetStateEvent>());
        var states = new List<int>();

        using var subscription = bloc.Stream.Subscribe(value => states.Add(value));

        bloc.Add(new TimedSetStateEvent(1, DelayMs: 120));
        await Task.Delay(20);
        bloc.Add(new TimedSetStateEvent(2, DelayMs: 10));

        await WaitUntilAsync(() => states.Count == 1);
        Assert.Equal([2], states);
    }

    [Fact]
    public async Task EventTransformers_Droppable_DropsNewEventsWhileHandlerRuns()
    {
        using var bloc = new TimedSetStateBloc(transformer: EventTransformers.Droppable<TimedSetStateEvent>());
        var states = new List<int>();

        using var subscription = bloc.Stream.Subscribe(value => states.Add(value));

        bloc.Add(new TimedSetStateEvent(1, DelayMs: 120));
        bloc.Add(new TimedSetStateEvent(2, DelayMs: 10));

        await WaitUntilAsync(() => states.Count == 1);
        Assert.Equal([1], states);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.ElapsedMilliseconds > timeoutMs)
            {
                throw new TimeoutException($"Condition was not met in {timeoutMs}ms.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class TestCounterCubit : Cubit<int>
    {
        public TestCounterCubit() : base(0)
        {
        }

        public void Increment()
        {
            Emit(State + 1);
        }

        public void EmitSameValue()
        {
            Emit(State);
        }
    }

    private sealed class CounterBloc : Bloc<AddEvent, int>
    {
        public CounterBloc() : base(0)
        {
            On<AddEvent>(HandleAdd);
        }

        private ValueTask HandleAdd(AddEvent @event, IEmitter<int> emitter, CancellationToken cancellationToken)
        {
            emitter.Emit(State + @event.Value);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TimedSetStateBloc : Bloc<TimedSetStateEvent, int>
    {
        public TimedSetStateBloc(EventTransformer<TimedSetStateEvent>? transformer = null) : base(0)
        {
            On<TimedSetStateEvent>(HandleTimedSetStateAsync, transformer);
        }

        private static async ValueTask HandleTimedSetStateAsync(
            TimedSetStateEvent @event,
            IEmitter<int> emitter,
            CancellationToken cancellationToken)
        {
            await Task.Delay(@event.DelayMs, cancellationToken);
            emitter.Emit(@event.NextState);
        }
    }

    private sealed record AddEvent(int Value);
    private sealed record TimedSetStateEvent(int NextState, int DelayMs);
}
