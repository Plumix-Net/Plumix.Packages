namespace Plumix.InAppPurchase;

/// <summary>The net11.0 build (desktop, tests): no store. Unavailable; queries find nothing; buying fails.</summary>
internal sealed class UnsupportedInAppPurchase : InAppPurchasePlatform
{
    private const string Source = "unsupported";

    private readonly PurchaseBroadcast _stream = new(() => { });

    public override IObservable<IReadOnlyList<PurchaseDetails>> PurchaseStream => _stream;

    public override Task<bool> IsAvailableAsync() => Task.FromResult(false);

    public override Task<ProductDetailsResponse> QueryProductDetailsAsync(ISet<string> identifiers) =>
        Task.FromResult(new ProductDetailsResponse([], [.. identifiers],
            new IAPError(Source, "store_unavailable", "In-app purchases work on Android and iOS only.")));

    public override Task<bool> BuyNonConsumableAsync(PurchaseParam purchaseParam) => Unavailable<bool>();

    public override Task<bool> BuyConsumableAsync(PurchaseParam purchaseParam, bool autoConsume) => Unavailable<bool>();

    public override Task CompletePurchaseAsync(PurchaseDetails purchase) => Task.CompletedTask;

    public override Task RestorePurchasesAsync(string? applicationUserName) => Task.CompletedTask;

    public override Task<string> CountryCodeAsync() => Task.FromResult("");

    private static Task<T> Unavailable<T>() => Task.FromException<T>(
        new InAppPurchaseException(Source, "store_unavailable", "In-app purchases work on Android and iOS only."));
}
