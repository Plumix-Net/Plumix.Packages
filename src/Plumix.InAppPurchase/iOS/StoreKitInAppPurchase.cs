// Dart parity source (reference): pub.dev/packages/in_app_purchase_storekit 0.4.16 — the StoreKit 1 path:
// lib/src/in_app_purchase_storekit_platform.dart (_TransactionObserver), lib/src/types/app_store_product_details.dart,
// lib/src/types/app_store_purchase_details.dart, darwin/.../InAppPurchasePlugin.swift (approximate)

#pragma warning disable CA1422 // StoreKit 1 is deprecated from iOS 18 yet supported; StoreKit 2 is Swift-only API

using Foundation;
using StoreKit;

namespace Plumix.InAppPurchase;

/// <summary>
/// iOS: StoreKit 1 — <c>SKProductsRequest</c> for products, <c>SKPaymentQueue</c> for payments, one transaction
/// observer for the purchase stream (added when the stream gets its first listener; StoreKit then re-delivers
/// unfinished transactions). The verification data is the app receipt; the purchase id is the transaction id.
/// </summary>
#pragma warning disable CA1001 // the observer lives as long as the process: the platform instance is never disposed
internal sealed class StoreKitInAppPurchase : InAppPurchasePlatform
#pragma warning restore CA1001
{
    private const string Source = "app_store";

    private readonly PurchaseBroadcast _stream;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, SKProduct> _products = [];
    private readonly HashSet<ProductsRequest> _requests = [];
    private TransactionObserver? _observer;
    private TaskCompletionSource? _restoring;

    public StoreKitInAppPurchase() => _stream = new PurchaseBroadcast(StartObserving);

    public override IObservable<IReadOnlyList<PurchaseDetails>> PurchaseStream => _stream;

    public override Task<bool> IsAvailableAsync() => Task.FromResult(SKPaymentQueue.CanMakePayments);

    public override Task<ProductDetailsResponse> QueryProductDetailsAsync(ISet<string> identifiers)
    {
        var request = new ProductsRequest(identifiers, this);
        lock (_gate)
            _requests.Add(request);
        return request.Task;
    }

    public override Task<bool> BuyNonConsumableAsync(PurchaseParam purchaseParam) => BuyAsync(purchaseParam);

    public override Task<bool> BuyConsumableAsync(PurchaseParam purchaseParam, bool autoConsume)
    {
        // StoreKit has no consume step: a consumable is finished like any purchase
        return BuyAsync(purchaseParam);
    }

    public override Task CompletePurchaseAsync(PurchaseDetails purchase)
    {
        if (purchase is not AppStorePurchaseDetails { Native: SKPaymentTransaction transaction })
            throw new ArgumentException("Not an App Store purchase.", nameof(purchase));

        // A purchasing transaction cannot be finished (StoreKit throws): the Dart plugin skips it too
        if (transaction.TransactionState != SKPaymentTransactionState.Purchasing)
            SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);

        purchase.PendingCompletePurchase = false;
        return Task.CompletedTask;
    }

    public override Task RestorePurchasesAsync(string? applicationUserName)
    {
        StartObserving();

        var restoring = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
            _restoring = restoring;

        if (applicationUserName is null)
            SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions();
        else
            SKPaymentQueue.DefaultQueue.RestoreCompletedTransactions(applicationUserName);

        return restoring.Task;
    }

    public override Task<string> CountryCodeAsync() =>
        Task.FromResult(SKPaymentQueue.DefaultQueue.Storefront?.CountryCode ?? "");

    private Task<bool> BuyAsync(PurchaseParam purchaseParam)
    {
        SKProduct? product;
        lock (_gate)
            _products.TryGetValue(purchaseParam.ProductDetails.Id, out product);

        product ??= (purchaseParam.ProductDetails as AppStoreProductDetails)?.Native as SKProduct;
        if (product is null)
            throw new ArgumentException("The product does not come from this store's query.", nameof(purchaseParam));

        StartObserving();

        var payment = SKMutablePayment.PaymentWithProduct(product);
        if (purchaseParam.ApplicationUserName is { } user)
            payment.ApplicationUsername = user;
        SKPaymentQueue.DefaultQueue.AddPayment(payment);
        return Task.FromResult(true);
    }

    private void StartObserving()
    {
        lock (_gate)
        {
            if (_observer is not null)
                return;
            _observer = new TransactionObserver(this);
        }

        SKPaymentQueue.DefaultQueue.AddTransactionObserver(_observer);
    }

    private void OnTransactions(SKPaymentTransaction[] transactions)
    {
        var receipt = Receipt();
        _stream.Add([.. transactions.Select(transaction => ToPurchaseDetails(transaction, receipt))]);
    }

    private void OnRestoreFinished(NSError? error)
    {
        TaskCompletionSource? restoring;
        lock (_gate)
        {
            restoring = _restoring;
            _restoring = null;
        }

        if (error is null)
            restoring?.TrySetResult();
        else
            restoring?.TrySetException(new InAppPurchaseException(Source, error.Domain + "#" + error.Code,
                error.LocalizedDescription));
    }

    private static AppStorePurchaseDetails ToPurchaseDetails(SKPaymentTransaction transaction, string receipt)
    {
        var status = transaction.TransactionState switch
        {
            SKPaymentTransactionState.Purchasing or SKPaymentTransactionState.Deferred => PurchaseStatus.Pending,
            SKPaymentTransactionState.Purchased => PurchaseStatus.Purchased,
            SKPaymentTransactionState.Restored => PurchaseStatus.Restored,
            SKPaymentTransactionState.Failed when transaction.Error?.Code == (long) SKError.PaymentCancelled =>
                PurchaseStatus.Canceled,
            _ => PurchaseStatus.Error
        };

        var date = transaction.TransactionDate is { } at
            ? ((long) (at.SecondsSinceReferenceDate * 1000 + 978_307_200_000)).ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            : null;

        return new AppStorePurchaseDetails(transaction.TransactionIdentifier, transaction.Payment.ProductIdentifier,
            new PurchaseVerificationData(receipt, receipt, Source), date, status,
            transaction.OriginalTransaction?.TransactionIdentifier ?? transaction.TransactionIdentifier,
            transaction.Payment.ApplicationUsername)
        {
            Native = transaction,
            PendingCompletePurchase = transaction.TransactionState != SKPaymentTransactionState.Purchasing,
            Error = transaction.Error is { } error && status == PurchaseStatus.Error
                ? new IAPError(Source, error.Domain + "#" + error.Code, error.LocalizedDescription,
                    error.UserInfo?.ToString())
                : null
        };
    }

    /// <summary>The app receipt, base64 (empty when there is none yet — a fresh sandbox install).</summary>
    private static string Receipt()
    {
        var url = NSBundle.MainBundle.AppStoreReceiptUrl;
        var data = url is null ? null : NSData.FromUrl(url);
        return data?.GetBase64EncodedString(NSDataBase64EncodingOptions.None) ?? "";
    }

    private static AppStoreProductDetails ToProductDetails(SKProduct product)
    {
        var formatter = new NSNumberFormatter
        {
            NumberStyle = NSNumberFormatterStyle.Currency,
            Locale = product.PriceLocale
        };

        SubscriptionPeriod? period = product.SubscriptionPeriod is { } p && p.NumberOfUnits > 0
            ? new SubscriptionPeriod((int) p.NumberOfUnits, p.Unit switch
            {
                SKProductPeriodUnit.Day => SubscriptionPeriodUnit.Day,
                SKProductPeriodUnit.Week => SubscriptionPeriodUnit.Week,
                SKProductPeriodUnit.Month => SubscriptionPeriodUnit.Month,
                _ => SubscriptionPeriodUnit.Year
            })
            : null;

        return new AppStoreProductDetails(product.ProductIdentifier, product.LocalizedTitle,
            product.LocalizedDescription, formatter.StringFromNumber(product.Price) ?? product.Price.ToString(),
            product.Price.DoubleValue, product.PriceLocale.CurrencyCode ?? "", product.PriceLocale.CurrencySymbol ?? "",
            period) { Native = product };
    }

    private sealed class ProductsRequest : SKProductsRequestDelegate
    {
        private readonly StoreKitInAppPurchase _owner;
        private readonly SKProductsRequest _request;
        private readonly TaskCompletionSource<ProductDetailsResponse> _done =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ProductsRequest(ISet<string> identifiers, StoreKitInAppPurchase owner)
        {
            _owner = owner;
            _request = new SKProductsRequest(new NSSet<NSString>(identifiers.Select(id => new NSString(id)).ToArray()))
            {
                Delegate = this
            };
            _request.Start();
        }

        public Task<ProductDetailsResponse> Task => _done.Task;

        public override void ReceivedResponse(SKProductsRequest request, SKProductsResponse response)
        {
            var products = response.Products ?? [];
            lock (_owner._gate)
                foreach (var product in products)
                    _owner._products[product.ProductIdentifier] = product;

            _done.TrySetResult(new ProductDetailsResponse([.. products.Select(ToProductDetails)],
                response.InvalidProducts ?? []));
        }

        public override void RequestFailed(SKRequest request, NSError error) =>
            _done.TrySetResult(new ProductDetailsResponse([], [],
                new IAPError(Source, error.Domain + "#" + error.Code, error.LocalizedDescription)));

        public override void RequestFinished(SKRequest request)
        {
            lock (_owner._gate)
                _owner._requests.Remove(this);
        }
    }

    private sealed class TransactionObserver(StoreKitInAppPurchase owner) : SKPaymentTransactionObserver
    {
        public override void UpdatedTransactions(SKPaymentQueue queue, SKPaymentTransaction[] transactions) =>
            owner.OnTransactions(transactions);

        public override void RestoreCompletedTransactionsFinished(SKPaymentQueue queue) => owner.OnRestoreFinished(null);

        public override void RestoreCompletedTransactionsFailedWithError(SKPaymentQueue queue, NSError error) =>
            owner.OnRestoreFinished(error);
    }
}
