using Xunit;
namespace Plumix.InAppPurchase.Tests;

public class InAppPurchaseTests
{
    [Theory]
    [InlineData("P1M", 1, SubscriptionPeriodUnit.Month)]
    [InlineData("P1Y", 1, SubscriptionPeriodUnit.Year)]
    [InlineData("P1W", 1, SubscriptionPeriodUnit.Week)]
    [InlineData("P3D", 3, SubscriptionPeriodUnit.Day)]
    [InlineData("P6M", 6, SubscriptionPeriodUnit.Month)]
    public void BillingPeriods_ParsesIso8601(string iso, int count, SubscriptionPeriodUnit unit) =>
        Assert.Equal(new SubscriptionPeriod(count, unit), BillingPeriods.Parse(iso));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1M")]
    [InlineData("PM")]
    [InlineData("P0M")]
    [InlineData("P1X")]
    public void BillingPeriods_RejectsTheRest(string? iso) => Assert.Null(BillingPeriods.Parse(iso));

    [Fact]
    public void Broadcast_StartsOnFirstListenerAndDeliversToCurrentListeners()
    {
        var starts = 0;
        var broadcast = new PurchaseBroadcast(() => starts++);
        var first = new List<IReadOnlyList<PurchaseDetails>>();
        var second = new List<IReadOnlyList<PurchaseDetails>>();

        var a = broadcast.Subscribe(new Observer(first.Add));
        using var b = broadcast.Subscribe(new Observer(second.Add));
        Assert.Equal(1, starts);

        var purchase = new PurchaseDetails("t1", "pro.year", new PurchaseVerificationData("l", "s", "app_store"), "0",
            PurchaseStatus.Purchased);
        broadcast.Add([purchase]);
        a.Dispose();
        broadcast.Add([purchase]);
        broadcast.Add([]);

        Assert.Single(first);
        Assert.Equal(2, second.Count);
    }

    [Fact]
    public async Task OnPlainDotNet_TheStoreIsUnavailable()
    {
        Assert.False(await InAppPurchase.Instance.IsAvailableAsync());

        var response = await InAppPurchase.Instance.QueryProductDetailsAsync(new HashSet<string> { "pro.year" });
        Assert.Empty(response.ProductDetails);
        Assert.Equal(["pro.year"], response.NotFoundIDs);
        Assert.NotNull(response.Error);

        var product = new ProductDetails("pro.year", "Pro", "", "$29", 29, "USD");
        await Assert.ThrowsAsync<InAppPurchaseException>(() =>
            InAppPurchase.Instance.BuyNonConsumableAsync(new PurchaseParam(product)));
    }

    private sealed class Observer(Action<IReadOnlyList<PurchaseDetails>> next) : IObserver<IReadOnlyList<PurchaseDetails>>
    {
        public void OnNext(IReadOnlyList<PurchaseDetails> value) => next(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
