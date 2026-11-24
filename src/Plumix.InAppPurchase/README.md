# Plumix.InAppPurchase

[![NuGet](https://img.shields.io/nuget/v/Plumix.InAppPurchase)](https://www.nuget.org/packages/Plumix.InAppPurchase)

In-app purchases for [Plumix](https://github.com/Plumix-Net/Plumix) and other .NET Android / iOS apps — a port of
Flutter's [in_app_purchase](https://pub.dev/packages/in_app_purchase): one API over the **App Store** (StoreKit) and
**Google Play Billing** (Play Billing Library 8).

## Installation

```bash
dotnet add package Plumix.InAppPurchase
```

## Usage

```csharp
using Plumix.InAppPurchase;

var store = InAppPurchase.Instance;

// 1. Listen early: purchases finished while the app was closed arrive here too
store.PurchaseStream.Subscribe(new PurchaseObserver(async purchases =>
{
    foreach (var purchase in purchases)
    {
        if (purchase.Status is PurchaseStatus.Purchased or PurchaseStatus.Restored)
            await backend.VerifyAndGrantAsync(purchase);          // verify on your server
        if (purchase.PendingCompletePurchase)
            await store.CompletePurchaseAsync(purchase);          // finish / acknowledge
    }
}));

// 2. Products
if (!await store.IsAvailableAsync()) return;
var response = await store.QueryProductDetailsAsync(new HashSet<string> { "pro.month", "pro.year" });

// 3. Buy (subscriptions are bought as non-consumables)
await store.BuyNonConsumableAsync(new PurchaseParam(response.ProductDetails[0], applicationUserName: userId));
```

`ApplicationUserName` ties the purchase to your user: on the App Store a UUID string becomes the transaction's
`appAccountToken`, on Play the obfuscated account id. Verify on the server:

| Store | What to send | Server API |
|---|---|---|
| App Store | `PurchaseID` (the transaction id) | App Store Server API `GET /inApps/v1/transactions/{id}`, Server Notifications V2 |
| Google Play | `VerificationData.ServerVerificationData` (the purchase token) | Play Developer API `purchases.subscriptionsv2` / `purchases.products`, Real-time developer notifications |

On Play a subscription comes once per offer (`GooglePlayProductDetails.BasePlanId`, `OfferId`, `OfferToken`); the
price is the offer's first pricing phase, `SubscriptionPeriod` the recurring one. Unacknowledged Play purchases are
refunded after three days — complete them (or acknowledge on the server).

## API

| Plumix.InAppPurchase | in_app_purchase |
|---|---|
| `InAppPurchase.Instance` | `InAppPurchase.instance` |
| `PurchaseStream` (`IObservable`) | `purchaseStream` |
| `IsAvailableAsync()`, `QueryProductDetailsAsync(ids)` | `isAvailable()`, `queryProductDetails(ids)` |
| `BuyNonConsumableAsync(param)`, `BuyConsumableAsync(param, autoConsume)` | `buyNonConsumable`, `buyConsumable` |
| `CompletePurchaseAsync(purchase)`, `RestorePurchasesAsync(userName)` | `completePurchase`, `restorePurchases` |
| `CountryCodeAsync()` | `countryCode()` |
| `ProductDetails`, `AppStoreProductDetails`, `GooglePlayProductDetails` | same |
| `PurchaseDetails`, `AppStorePurchaseDetails`, `GooglePlayPurchaseDetails`, `PurchaseVerificationData`, `PurchaseStatus` | same |
| `PurchaseParam`, `GooglePlayPurchaseParam`, `IAPError`, `InAppPurchaseException` | same |
| `InAppPurchasePlatform.Instance` (replace in tests) | `InAppPurchasePlatform.instance` |

Deviations: iOS runs on **StoreKit 1** — the Dart package's StoreKit 2 path is Swift-only API that .NET cannot bind;
`PurchaseID` is the same transaction id StoreKit 2 and the App Store Server API use, `ServerVerificationData` the app
receipt. Platform additions (`getPlatformAddition`: price-change confirmations, subscription replacement) are not
ported.
