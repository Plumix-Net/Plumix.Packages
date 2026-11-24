// Dart parity source (reference): pub.dev/packages/in_app_purchase_platform_interface 1.4.1, lib/src/types/*.dart and
// lib/src/errors/*.dart; in_app_purchase_android 0.5.5 lib/src/types (GooglePlayProductDetails,
// GooglePlayPurchaseDetails, GooglePlayPurchaseParam); in_app_purchase_storekit 0.4.16 lib/src/types
// (AppStoreProductDetails, AppStorePurchaseDetails) (approximate)

namespace Plumix.InAppPurchase;

/// <summary>A product as the store sells it. Equivalent to <c>ProductDetails</c>.</summary>
public class ProductDetails
{
    /// <summary>Creates the details.</summary>
    public ProductDetails(string id, string title, string description, string price, double rawPrice,
        string currencyCode, string currencySymbol = "")
    {
        Id = id;
        Title = title;
        Description = description;
        Price = price;
        RawPrice = rawPrice;
        CurrencyCode = currencyCode;
        CurrencySymbol = currencySymbol;
    }

    /// <summary>The product id configured in App Store Connect / Play Console.</summary>
    public string Id { get; }

    /// <summary>The product's title.</summary>
    public string Title { get; }

    /// <summary>The product's description.</summary>
    public string Description { get; }

    /// <summary>The price formatted for the user's store and locale (<c>4,99 €</c>).</summary>
    public string Price { get; }

    /// <summary>The price as a number in <see cref="CurrencyCode"/>.</summary>
    public double RawPrice { get; }

    /// <summary>The ISO 4217 currency code.</summary>
    public string CurrencyCode { get; }

    /// <summary>The currency symbol, when the platform gives it.</summary>
    public string CurrencySymbol { get; }
}

/// <summary>A subscription's billing period.</summary>
/// <param name="Count">How many units.</param>
/// <param name="Unit">The unit.</param>
public sealed record SubscriptionPeriod(int Count, SubscriptionPeriodUnit Unit);

/// <summary>The unit of a <see cref="SubscriptionPeriod"/>.</summary>
public enum SubscriptionPeriodUnit
{
    /// <summary>Days.</summary>
    Day,

    /// <summary>Weeks.</summary>
    Week,

    /// <summary>Months.</summary>
    Month,

    /// <summary>Years.</summary>
    Year
}

/// <summary>
/// An App Store product (StoreKit's <c>SKProduct</c>). Equivalent to <c>AppStoreProductDetails</c>.
/// </summary>
public sealed class AppStoreProductDetails(string id, string title, string description, string price, double rawPrice,
    string currencyCode, string currencySymbol, SubscriptionPeriod? subscriptionPeriod)
    : ProductDetails(id, title, description, price, rawPrice, currencyCode, currencySymbol)
{
    /// <summary>The renewal period of an auto-renewable subscription; null for other products.</summary>
    public SubscriptionPeriod? SubscriptionPeriod { get; } = subscriptionPeriod;

    /// <summary>The platform product (an <c>SKProduct</c> on iOS).</summary>
    internal object? Native { get; init; }
}

/// <summary>The kind of a Google Play product.</summary>
public enum ProductType
{
    /// <summary>A one-time product (<c>inapp</c>).</summary>
    InApp,

    /// <summary>A subscription (<c>subs</c>).</summary>
    Subs
}

/// <summary>
/// A Google Play product. A subscription comes once per offer (base plan and its offers), each with its
/// <see cref="OfferToken"/>; the price is the offer's first pricing phase. Equivalent to
/// <c>GooglePlayProductDetails</c>.
/// </summary>
public sealed class GooglePlayProductDetails(string id, string title, string description, string price,
    double rawPrice, string currencyCode, string currencySymbol, ProductType productType, int? subscriptionIndex,
    string? basePlanId, string? offerId, string? offerToken, SubscriptionPeriod? subscriptionPeriod)
    : ProductDetails(id, title, description, price, rawPrice, currencyCode, currencySymbol)
{
    /// <summary>One-time product or subscription.</summary>
    public ProductType ProductType { get; } = productType;

    /// <summary>The index of the offer among the subscription's offers; null for a one-time product.</summary>
    public int? SubscriptionIndex { get; } = subscriptionIndex;

    /// <summary>The subscription's base plan.</summary>
    public string? BasePlanId { get; } = basePlanId;

    /// <summary>The offer on the base plan, when this is one (a trial, an introductory price).</summary>
    public string? OfferId { get; } = offerId;

    /// <summary>The token that buys this offer.</summary>
    public string? OfferToken { get; } = offerToken;

    /// <summary>The billing period of the offer's last (recurring) pricing phase.</summary>
    public SubscriptionPeriod? SubscriptionPeriod { get; } = subscriptionPeriod;

    /// <summary>The platform product (a <c>ProductDetails</c> of Play Billing).</summary>
    internal object? Native { get; init; }
}

/// <summary>The answer of a product query. Equivalent to <c>ProductDetailsResponse</c>.</summary>
/// <param name="ProductDetails">The products found.</param>
/// <param name="NotFoundIDs">Ids the store does not know.</param>
/// <param name="Error">The error of the query, if it failed.</param>
public sealed record ProductDetailsResponse(IReadOnlyList<ProductDetails> ProductDetails,
    IReadOnlyList<string> NotFoundIDs, IAPError? Error = null);

/// <summary>Where a purchase stands. Equivalent to <c>PurchaseStatus</c>.</summary>
public enum PurchaseStatus
{
    /// <summary>Waiting: being processed, deferred (Ask to Buy) or a pending payment.</summary>
    Pending,

    /// <summary>Paid; deliver the product, then <see cref="InAppPurchase.CompletePurchaseAsync"/>.</summary>
    Purchased,

    /// <summary>Failed; see <see cref="PurchaseDetails.Error"/>.</summary>
    Error,

    /// <summary>A purchase made before, returned by a restore.</summary>
    Restored,

    /// <summary>The user cancelled.</summary>
    Canceled
}

/// <summary>What the backend verifies a purchase with. Equivalent to <c>PurchaseVerificationData</c>.</summary>
/// <param name="LocalVerificationData">For checks on the device (Play: the purchase JSON; App Store: the receipt).</param>
/// <param name="ServerVerificationData">For the server (Play: the purchase token; App Store: the app receipt,
/// base64).</param>
/// <param name="Source">Which store: <c>google_play</c> or <c>app_store</c>.</param>
public sealed record PurchaseVerificationData(string LocalVerificationData, string ServerVerificationData, string Source);

/// <summary>A purchase, as the purchase stream reports it. Equivalent to <c>PurchaseDetails</c>.</summary>
public class PurchaseDetails
{
    /// <summary>Creates the details.</summary>
    public PurchaseDetails(string? purchaseID, string productID, PurchaseVerificationData verificationData,
        string? transactionDate, PurchaseStatus status)
    {
        PurchaseID = purchaseID;
        ProductID = productID;
        VerificationData = verificationData;
        TransactionDate = transactionDate;
        Status = status;
    }

    /// <summary>The store's id of the purchase (App Store: the transaction id; Play: the order id).</summary>
    public string? PurchaseID { get; }

    /// <summary>The product bought.</summary>
    public string ProductID { get; }

    /// <summary>What to verify the purchase with.</summary>
    public PurchaseVerificationData VerificationData { get; }

    /// <summary>When it was bought, in milliseconds since the Unix epoch (as a string, as in Dart).</summary>
    public string? TransactionDate { get; }

    /// <summary>Where the purchase stands.</summary>
    public PurchaseStatus Status { get; set; }

    /// <summary>The error, when <see cref="Status"/> is <see cref="PurchaseStatus.Error"/>.</summary>
    public IAPError? Error { get; set; }

    /// <summary>
    /// Whether <see cref="InAppPurchase.CompletePurchaseAsync"/> must still be called: the App Store finishes the
    /// transaction then, Play acknowledges it (unacknowledged Play purchases are refunded after three days).
    /// </summary>
    public bool PendingCompletePurchase { get; set; }
}

/// <summary>An App Store purchase (an <c>SKPaymentTransaction</c>). Equivalent to <c>AppStorePurchaseDetails</c>.</summary>
public sealed class AppStorePurchaseDetails(string? purchaseID, string productID,
    PurchaseVerificationData verificationData, string? transactionDate, PurchaseStatus status,
    string? originalTransactionId, string? applicationUsername)
    : PurchaseDetails(purchaseID, productID, verificationData, transactionDate, status)
{
    /// <summary>The original purchase's transaction id (renewals and restores point to it).</summary>
    public string? OriginalTransactionId { get; } = originalTransactionId;

    /// <summary>The payment's application username (a UUID becomes <c>appAccountToken</c> on the server).</summary>
    public string? ApplicationUsername { get; } = applicationUsername;

    /// <summary>The platform transaction (an <c>SKPaymentTransaction</c>).</summary>
    internal object? Native { get; init; }
}

/// <summary>A Google Play purchase. Equivalent to <c>GooglePlayPurchaseDetails</c>.</summary>
public sealed class GooglePlayPurchaseDetails(string? purchaseID, string productID,
    PurchaseVerificationData verificationData, string? transactionDate, PurchaseStatus status,
    string purchaseToken, bool isAcknowledged, bool isAutoRenewing, string? obfuscatedAccountId, string originalJson,
    string signature)
    : PurchaseDetails(purchaseID, productID, verificationData, transactionDate, status)
{
    /// <summary>The token the Play Developer API knows the purchase by.</summary>
    public string PurchaseToken { get; } = purchaseToken;

    /// <summary>Whether it was acknowledged (by the app or the server).</summary>
    public bool IsAcknowledged { get; } = isAcknowledged;

    /// <summary>Whether a subscription renews.</summary>
    public bool IsAutoRenewing { get; } = isAutoRenewing;

    /// <summary>The account id the purchase was made with (<see cref="PurchaseParam.ApplicationUserName"/>).</summary>
    public string? ObfuscatedAccountId { get; } = obfuscatedAccountId;

    /// <summary>The purchase as Play's JSON.</summary>
    public string OriginalJson { get; } = originalJson;

    /// <summary>The signature of <see cref="OriginalJson"/> with the app's license key.</summary>
    public string Signature { get; } = signature;
}

/// <summary>What to buy. Equivalent to <c>PurchaseParam</c>.</summary>
public class PurchaseParam
{
    /// <summary>Creates the parameter.</summary>
    public PurchaseParam(ProductDetails productDetails, string? applicationUserName = null)
    {
        ProductDetails = productDetails;
        ApplicationUserName = applicationUserName;
    }

    /// <summary>The product (from <see cref="InAppPurchase.QueryProductDetailsAsync"/>).</summary>
    public ProductDetails ProductDetails { get; }

    /// <summary>
    /// The app's id of the user, attached to the purchase so the server can match it: App Store's
    /// <c>applicationUsername</c> (a UUID string becomes the transaction's <c>appAccountToken</c>), Play's obfuscated
    /// account id (at most 64 characters).
    /// </summary>
    public string? ApplicationUserName { get; }
}

/// <summary>A Google Play purchase with an explicit offer. Equivalent to <c>GooglePlayPurchaseParam</c>.</summary>
public sealed class GooglePlayPurchaseParam(ProductDetails productDetails, string? applicationUserName = null,
    string? offerToken = null) : PurchaseParam(productDetails, applicationUserName)
{
    /// <summary>The offer to buy; defaults to the <see cref="GooglePlayProductDetails.OfferToken"/>.</summary>
    public string? OfferToken { get; } = offerToken;
}

/// <summary>An error the store reported. Equivalent to <c>IAPError</c>.</summary>
/// <param name="Source">Which store.</param>
/// <param name="Code">The store's code.</param>
/// <param name="Message">The store's message.</param>
/// <param name="Details">More, when there is.</param>
public sealed record IAPError(string Source, string Code, string Message, object? Details = null);

/// <summary>A failed store call. Equivalent to <c>InAppPurchaseException</c>.</summary>
public sealed class InAppPurchaseException : Exception
{
    /// <summary>Creates the exception.</summary>
    public InAppPurchaseException(string source, string code, string? message = null)
        : base($"InAppPurchaseException({code}, {message}, {source})")
    {
        Source = source;
        Code = code;
        Reason = message;
    }

    /// <summary>Which store.</summary>
    public new string Source { get; }

    /// <summary>The store's code.</summary>
    public string Code { get; }

    /// <summary>The store's message.</summary>
    public string? Reason { get; }
}
