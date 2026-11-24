namespace Plumix.InAppPurchase;

/// <summary>
/// The purchase stream: a broadcast observable like Dart's broadcast <c>StreamController</c> — updates go to whoever
/// listens at the time; the first subscriber starts the store observation (<paramref name="onFirstListener"/>).
/// </summary>
internal sealed class PurchaseBroadcast(Action onFirstListener) : IObservable<IReadOnlyList<PurchaseDetails>>
{
    private readonly Lock _gate = new();
    private readonly List<IObserver<IReadOnlyList<PurchaseDetails>>> _observers = [];
    private bool _started;

    public IDisposable Subscribe(IObserver<IReadOnlyList<PurchaseDetails>> observer)
    {
        bool start;
        lock (_gate)
        {
            _observers.Add(observer);
            start = !_started;
            _started = true;
        }

        if (start)
            onFirstListener();

        return new Subscription(this, observer);
    }

    public void Add(IReadOnlyList<PurchaseDetails> purchases)
    {
        if (purchases.Count == 0)
            return;

        IObserver<IReadOnlyList<PurchaseDetails>>[] observers;
        lock (_gate)
            observers = [.. _observers];

        foreach (var observer in observers)
            observer.OnNext(purchases);
    }

    private sealed class Subscription(PurchaseBroadcast owner, IObserver<IReadOnlyList<PurchaseDetails>> observer)
        : IDisposable
    {
        public void Dispose()
        {
            lock (owner._gate)
                owner._observers.Remove(observer);
        }
    }
}

/// <summary>ISO 8601 durations of store billing periods (<c>P1M</c>, <c>P1Y</c>, <c>P1W</c>, <c>P3D</c>).</summary>
internal static class BillingPeriods
{
    public static SubscriptionPeriod? Parse(string? iso)
    {
        if (string.IsNullOrEmpty(iso) || iso[0] != 'P' || iso.Length < 3)
            return null;

        var unit = iso[^1] switch
        {
            'D' => SubscriptionPeriodUnit.Day,
            'W' => SubscriptionPeriodUnit.Week,
            'M' => SubscriptionPeriodUnit.Month,
            'Y' => SubscriptionPeriodUnit.Year,
            _ => (SubscriptionPeriodUnit?) null
        };

        return unit is { } u && int.TryParse(iso.AsSpan(1, iso.Length - 2), out var count) && count > 0
            ? new SubscriptionPeriod(count, u)
            : null;
    }
}
