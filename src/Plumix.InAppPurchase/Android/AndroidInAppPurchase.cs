// Dart parity source (reference): pub.dev/packages/in_app_purchase_android 0.5.5,
// lib/src/in_app_purchase_android_platform.dart, lib/src/types/google_play_product_details.dart,
// lib/src/types/google_play_purchase_details.dart, android/.../InAppPurchasePlugin + MethodCallHandlerImpl.java
// (approximate)

using Android.BillingClient.Api;
using Plumix.Plugins;
using Activity = Android.App.Activity;
using PlayProductDetails = Android.BillingClient.Api.ProductDetails;

namespace Plumix.InAppPurchase;

/// <summary>
/// Android: Google Play Billing. One <c>BillingClient</c> with automatic service reconnection; purchase updates go to
/// the stream. Restore queries the owned one-time products and subscriptions and reports them as restored; completing
/// a purchase acknowledges it (or consumes it, for a consumable bought with auto-consume).
/// </summary>
internal sealed class AndroidInAppPurchase : InAppPurchasePlatform
{
    private const string Source = "google_play";

    private readonly PurchaseBroadcast _stream;
    private readonly Lock _gate = new();
    private readonly HashSet<string> _autoConsume = [];
    private BillingClient? _client;
    private Task<BillingResult>? _connecting;

    public AndroidInAppPurchase() => _stream = new PurchaseBroadcast(() => _ = ConnectAsync());

    public override IObservable<IReadOnlyList<PurchaseDetails>> PurchaseStream => _stream;

    public override async Task<bool> IsAvailableAsync()
    {
        try
        {
            return (await ConnectAsync().ConfigureAwait(false)).ResponseCode == BillingResponseCode.Ok;
        }
        catch (Java.Lang.Exception)
        {
            return false;
        }
    }

    public override async Task<ProductDetailsResponse> QueryProductDetailsAsync(ISet<string> identifiers)
    {
        var client = await ReadyClientAsync().ConfigureAwait(false);
        if (client is null)
            return new ProductDetailsResponse([], [.. identifiers],
                new IAPError(Source, "BillingResponse.serviceUnavailable", "Google Play Billing is not available."));

        var found = new List<ProductDetails>();
        IAPError? error = null;

        // One query per type: Play answers a mixed list only for the type the products have
        foreach (var type in new[] { BillingClient.ProductType.Inapp, BillingClient.ProductType.Subs })
        {
            var products = identifiers.Select(id => QueryProductDetailsParams.Product.NewBuilder()
                .SetProductId(id).SetProductType(type).Build()).ToList();
            var parameters = QueryProductDetailsParams.NewBuilder().SetProductList(products).Build();

            var result = await client.QueryProductDetailsAsync(parameters).ConfigureAwait(false);
            if (result.Result.ResponseCode != BillingResponseCode.Ok)
            {
                error ??= ToError(result.Result);
                continue;
            }

            foreach (var details in result.ProductDetailsList)
                found.AddRange(ToProductDetails(details));
        }

        var foundIds = found.Select(product => product.Id).ToHashSet();
        return new ProductDetailsResponse(found, [.. identifiers.Where(id => !foundIds.Contains(id))],
            found.Count == 0 ? error : null);
    }

    public override Task<bool> BuyNonConsumableAsync(PurchaseParam purchaseParam) => BuyAsync(purchaseParam);

    public override Task<bool> BuyConsumableAsync(PurchaseParam purchaseParam, bool autoConsume)
    {
        lock (_gate)
        {
            if (autoConsume)
                _autoConsume.Add(purchaseParam.ProductDetails.Id);
            else
                _autoConsume.Remove(purchaseParam.ProductDetails.Id);
        }

        return BuyAsync(purchaseParam);
    }

    public override async Task CompletePurchaseAsync(PurchaseDetails purchase)
    {
        if (purchase is not GooglePlayPurchaseDetails play)
            throw new ArgumentException("Not a Google Play purchase.", nameof(purchase));

        if (play.IsAcknowledged)
            return;

        var client = await ReadyClientAsync().ConfigureAwait(false)
            ?? throw new InAppPurchaseException(Source, "BillingResponse.serviceUnavailable");

        bool consume;
        lock (_gate)
            consume = _autoConsume.Contains(play.ProductID);

        var result = consume
            ? (await client.ConsumeAsync(ConsumeParams.NewBuilder().SetPurchaseToken(play.PurchaseToken).Build())
                .ConfigureAwait(false)).BillingResult
            : await client.AcknowledgePurchaseAsync(AcknowledgePurchaseParams.NewBuilder()
                .SetPurchaseToken(play.PurchaseToken).Build()).ConfigureAwait(false);

        if (result.ResponseCode != BillingResponseCode.Ok)
            throw new InAppPurchaseException(Source, Code(result.ResponseCode), result.DebugMessage);

        purchase.PendingCompletePurchase = false;
    }

    public override async Task RestorePurchasesAsync(string? applicationUserName)
    {
        var client = await ReadyClientAsync().ConfigureAwait(false)
            ?? throw new InAppPurchaseException(Source, "BillingResponse.serviceUnavailable");

        var restored = new List<PurchaseDetails>();
        foreach (var type in new[] { BillingClient.ProductType.Inapp, BillingClient.ProductType.Subs })
        {
            var result = await client.QueryPurchasesAsync(QueryPurchasesParams.NewBuilder().SetProductType(type).Build())
                .ConfigureAwait(false);
            if (result.Result.ResponseCode != BillingResponseCode.Ok)
                throw new InAppPurchaseException(Source, Code(result.Result.ResponseCode), result.Result.DebugMessage);

            foreach (var purchase in result.Purchases)
                restored.AddRange(ToPurchaseDetails(purchase, restore: true));
        }

        _stream.Add(restored);
    }

    public override async Task<string> CountryCodeAsync()
    {
        var client = await ReadyClientAsync().ConfigureAwait(false)
            ?? throw new InAppPurchaseException(Source, "BillingResponse.serviceUnavailable");

        var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.GetBillingConfigAsync(GetBillingConfigParams.NewBuilder().Build(), new ConfigListener(done));
        return await done.Task.ConfigureAwait(false);
    }

    private async Task<bool> BuyAsync(PurchaseParam purchaseParam)
    {
        if (purchaseParam.ProductDetails is not GooglePlayProductDetails { Native: PlayProductDetails native } details)
            throw new ArgumentException("The product does not come from this store's query.", nameof(purchaseParam));

        var client = await ReadyClientAsync().ConfigureAwait(false)
            ?? throw new InAppPurchaseException(Source, "BillingResponse.serviceUnavailable");
        var activity = ActivityTracker.Current
            ?? throw new InAppPurchaseException(Source, "no_activity", "No activity is available to show the purchase.");

        var productParams = BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(native);
        var offerToken = (purchaseParam as GooglePlayPurchaseParam)?.OfferToken ?? details.OfferToken;
        if (offerToken is not null)
            productParams.SetOfferToken(offerToken);

        var flow = BillingFlowParams.NewBuilder().SetProductDetailsParamsList([productParams.Build()]);
        if (purchaseParam.ApplicationUserName is { } user)
            flow.SetObfuscatedAccountId(user);

        var result = await OnUiThreadAsync(activity, () => client.LaunchBillingFlow(activity, flow.Build()))
            .ConfigureAwait(false);
        return result.ResponseCode == BillingResponseCode.Ok;
    }

    private async Task<BillingClient?> ReadyClientAsync()
    {
        var result = await ConnectAsync().ConfigureAwait(false);
        return result.ResponseCode == BillingResponseCode.Ok ? _client : null;
    }

    private Task<BillingResult> ConnectAsync()
    {
        lock (_gate)
        {
            if (_client is { IsReady: true } && _connecting is { IsCompletedSuccessfully: true } done
                && done.Result.ResponseCode == BillingResponseCode.Ok)
                return done;

            if (_connecting is { IsCompleted: false } running)
                return running;

            _client ??= BillingClient.NewBuilder(Android.App.Application.Context)
                .SetListener(new PurchasesListener(this))
                .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
                .EnableAutoServiceReconnection()
                .Build();

            var connected = new TaskCompletionSource<BillingResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _client.StartConnection(new StateListener(connected));
            return _connecting = connected.Task;
        }
    }

    private void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
    {
        if (result.ResponseCode == BillingResponseCode.Ok && purchases is not null)
        {
            _stream.Add([.. purchases.SelectMany(purchase => ToPurchaseDetails(purchase, restore: false))]);
            return;
        }

        // Cancelled or failed: one update without a product, as the Dart plugin reports it
        var status = result.ResponseCode == BillingResponseCode.UserCancelled
            ? PurchaseStatus.Canceled
            : PurchaseStatus.Error;
        var failed = new GooglePlayPurchaseDetails(null, "", new PurchaseVerificationData("", "", Source), null, status,
            "", false, false, null, "", "")
        {
            Error = status == PurchaseStatus.Error ? ToError(result) : null
        };
        _stream.Add([failed]);
    }

    private static IEnumerable<GooglePlayPurchaseDetails> ToPurchaseDetails(Purchase purchase, bool restore)
    {
        var status = purchase.PurchaseState switch
        {
            PurchaseState.Pending => PurchaseStatus.Pending,
            PurchaseState.Purchased => restore ? PurchaseStatus.Restored : PurchaseStatus.Purchased,
            _ => PurchaseStatus.Error
        };

        // One update per product of the purchase, as the Dart plugin splits multi-product purchases
        foreach (var productId in purchase.Products)
            yield return new GooglePlayPurchaseDetails(purchase.OrderId, productId,
                new PurchaseVerificationData(purchase.OriginalJson, purchase.PurchaseToken, Source),
                purchase.PurchaseTime.ToString(System.Globalization.CultureInfo.InvariantCulture), status,
                purchase.PurchaseToken, purchase.IsAcknowledged, purchase.IsAutoRenewing,
                purchase.AccountIdentifiers?.ObfuscatedAccountId, purchase.OriginalJson, purchase.Signature)
            {
                PendingCompletePurchase = !purchase.IsAcknowledged && status != PurchaseStatus.Pending,
                Error = status == PurchaseStatus.Error
                    ? new IAPError(Source, "purchase_error", "The purchase is in an unknown state.")
                    : null
            };
    }

    private static IEnumerable<GooglePlayProductDetails> ToProductDetails(PlayProductDetails details)
    {
        if (details.ProductType == BillingClient.ProductType.Inapp)
        {
            var offer = details.GetOneTimePurchaseOfferDetails();
            if (offer is null)
                yield break;

            yield return new GooglePlayProductDetails(details.ProductId, details.Title, details.Description,
                offer.FormattedPrice, offer.PriceAmountMicros / 1_000_000.0, offer.PriceCurrencyCode, "",
                ProductType.InApp, null, null, null, offer.OfferToken, null) { Native = details };
            yield break;
        }

        var offers = details.GetSubscriptionOfferDetails() ?? [];
        for (var index = 0; index < offers.Count; index++)
        {
            var offer = offers[index];
            var phases = offer.PricingPhases.PricingPhaseList;
            if (phases.Count == 0)
                continue;

            var first = phases[0];
            yield return new GooglePlayProductDetails(details.ProductId, details.Title, details.Description,
                first.FormattedPrice, first.PriceAmountMicros / 1_000_000.0, first.PriceCurrencyCode, "",
                ProductType.Subs, index, offer.BasePlanId, offer.OfferId, offer.OfferToken,
                BillingPeriods.Parse(phases[^1].BillingPeriod)) { Native = details };
        }
    }

    private static IAPError ToError(BillingResult result) =>
        new(Source, Code(result.ResponseCode), result.DebugMessage ?? "");

    // The Dart plugin's codes: "BillingResponse.<name>"
    private static string Code(BillingResponseCode code) =>
        "BillingResponse." + char.ToLowerInvariant(code.ToString()[0]) + code.ToString()[1..];

    private static Task<T> OnUiThreadAsync<T>(Activity activity, Func<T> action)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        activity.RunOnUiThread(() =>
        {
            try
            {
                done.TrySetResult(action());
            }
            catch (Exception e)
            {
                done.TrySetException(e);
            }
        });
        return done.Task;
    }

    private sealed class PurchasesListener(AndroidInAppPurchase owner) : Java.Lang.Object, IPurchasesUpdatedListener
    {
        public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases) =>
            owner.OnPurchasesUpdated(result, purchases);
    }

    private sealed class StateListener(TaskCompletionSource<BillingResult> connected)
        : Java.Lang.Object, IBillingClientStateListener
    {
        public void OnBillingSetupFinished(BillingResult result) => connected.TrySetResult(result);

        // EnableAutoServiceReconnection reconnects on the next call; a setup still waiting fails now
        public void OnBillingServiceDisconnected() => connected.TrySetResult(BillingResult.NewBuilder()
            .SetResponseCode((int) BillingResponseCode.ServiceDisconnected).Build());
    }

    private sealed class ConfigListener(TaskCompletionSource<string> done)
        : Java.Lang.Object, IBillingConfigResponseListener
    {
        public void OnBillingConfigResponse(BillingResult result, BillingConfig? config)
        {
            if (result.ResponseCode == BillingResponseCode.Ok && config is not null)
                done.TrySetResult(config.CountryCode);
            else
                done.TrySetException(new InAppPurchaseException(Source, Code(result.ResponseCode), result.DebugMessage));
        }
    }
}
