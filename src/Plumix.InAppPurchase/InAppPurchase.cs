// Dart parity source (reference): pub.dev/packages/in_app_purchase 3.3.1, lib/in_app_purchase.dart;
// in_app_purchase_platform_interface 1.4.1, lib/src/in_app_purchase_platform.dart (approximate)

namespace Plumix.InAppPurchase;

/// <summary>
/// In-app purchases over the platform's store: the App Store (StoreKit) on iOS, Google Play Billing on Android.
///
/// Listen to <see cref="PurchaseStream"/> early (it delivers purchases that finished while the app was closed),
/// query products with <see cref="QueryProductDetailsAsync"/>, buy with <see cref="BuyNonConsumableAsync"/>
/// (subscriptions too) or <see cref="BuyConsumableAsync"/>, verify each purchase on your server, deliver, then call
/// <see cref="CompletePurchaseAsync"/>.
///
/// Equivalent to in_app_purchase's <c>InAppPurchase</c>. Deviations: the iOS side runs on StoreKit 1 (the Dart
/// package's StoreKit 2 path is Swift-only API that .NET cannot bind) — <see cref="PurchaseDetails.PurchaseID"/> is the
/// transaction id the App Store Server API takes, <see cref="PurchaseVerificationData.ServerVerificationData"/> the
/// app receipt; platform additions (<c>getPlatformAddition</c>) are not ported. Other platforms report the store as
/// unavailable.
/// </summary>
#pragma warning disable CA1822 // instance members on purpose: the API is InAppPurchase.Instance.X, as in Dart
public sealed class InAppPurchase
{
    private InAppPurchase()
    {
    }

    /// <summary>The single instance, as <c>InAppPurchase.instance</c>.</summary>
    public static InAppPurchase Instance { get; } = new();

    /// <summary>
    /// Purchase updates: new purchases, their state changes, restored purchases. Subscribing starts observing the
    /// store; unfinished purchases arrive right away.
    /// </summary>
    public IObservable<IReadOnlyList<PurchaseDetails>> PurchaseStream => InAppPurchasePlatform.Instance.PurchaseStream;

    /// <summary>Whether the store can be used (signed in, payments allowed, billing service reachable).</summary>
    public Task<bool> IsAvailableAsync() => InAppPurchasePlatform.Instance.IsAvailableAsync();

    /// <summary>Asks the store for products by id.</summary>
    public Task<ProductDetailsResponse> QueryProductDetailsAsync(ISet<string> identifiers) =>
        InAppPurchasePlatform.Instance.QueryProductDetailsAsync(identifiers);

    /// <summary>
    /// Starts buying a product bought once (a non-consumable or a subscription). The result comes through
    /// <see cref="PurchaseStream"/>.
    /// </summary>
    /// <returns>Whether the purchase flow started.</returns>
    public Task<bool> BuyNonConsumableAsync(PurchaseParam purchaseParam) =>
        InAppPurchasePlatform.Instance.BuyNonConsumableAsync(purchaseParam);

    /// <summary>Starts buying a consumable; with <paramref name="autoConsume"/> Play consumes it on completion.</summary>
    /// <returns>Whether the purchase flow started.</returns>
    public Task<bool> BuyConsumableAsync(PurchaseParam purchaseParam, bool autoConsume = true) =>
        InAppPurchasePlatform.Instance.BuyConsumableAsync(purchaseParam, autoConsume);

    /// <summary>
    /// Finishes a delivered purchase (App Store: finishes the transaction; Play: acknowledges it). Call it for every
    /// purchase with <see cref="PurchaseDetails.PendingCompletePurchase"/>, after delivering the content.
    /// </summary>
    public Task CompletePurchaseAsync(PurchaseDetails purchase) =>
        InAppPurchasePlatform.Instance.CompletePurchaseAsync(purchase);

    /// <summary>
    /// Re-delivers the user's past purchases to <see cref="PurchaseStream"/> with <see cref="PurchaseStatus.Restored"/>.
    /// </summary>
    public Task RestorePurchasesAsync(string? applicationUserName = null) =>
        InAppPurchasePlatform.Instance.RestorePurchasesAsync(applicationUserName);

    /// <summary>
    /// The store's country: ISO 3166-1 alpha-3 on iOS (<c>USA</c>), alpha-2 on Android (<c>US</c>), as in Dart.
    /// </summary>
    public Task<string> CountryCodeAsync() => InAppPurchasePlatform.Instance.CountryCodeAsync();
}
#pragma warning restore CA1822

/// <summary>The platform side of <see cref="InAppPurchase"/>; replaceable for tests.</summary>
public abstract class InAppPurchasePlatform
{
    /// <summary>The implementation in use.</summary>
    public static InAppPurchasePlatform Instance { get; set; } = CreateDefault();

    /// <summary>Purchase updates.</summary>
    public abstract IObservable<IReadOnlyList<PurchaseDetails>> PurchaseStream { get; }

    /// <summary>Whether the store can be used.</summary>
    public abstract Task<bool> IsAvailableAsync();

    /// <summary>Queries products.</summary>
    public abstract Task<ProductDetailsResponse> QueryProductDetailsAsync(ISet<string> identifiers);

    /// <summary>Buys a non-consumable or a subscription.</summary>
    public abstract Task<bool> BuyNonConsumableAsync(PurchaseParam purchaseParam);

    /// <summary>Buys a consumable.</summary>
    public abstract Task<bool> BuyConsumableAsync(PurchaseParam purchaseParam, bool autoConsume);

    /// <summary>Completes a purchase.</summary>
    public abstract Task CompletePurchaseAsync(PurchaseDetails purchase);

    /// <summary>Restores past purchases.</summary>
    public abstract Task RestorePurchasesAsync(string? applicationUserName);

    /// <summary>The store's country.</summary>
    public abstract Task<string> CountryCodeAsync();

#pragma warning disable CA1859 // the return type is the abstraction on purpose: each TFM returns another class
    private static InAppPurchasePlatform CreateDefault() =>
#if ANDROID
        new AndroidInAppPurchase();
#elif IOS
        new StoreKitInAppPurchase();
#else
        new UnsupportedInAppPurchase();
#endif
#pragma warning restore CA1859
}
