using Android.Content;
using Plumix.Plugins;

namespace Plumix.InAppPurchase;

/// <summary>Starts following the app's activities at process start (see <see cref="ActivityTracker"/>).</summary>
[ContentProvider(["${applicationId}.plumix-in-app-purchase"], Exported = false)]
internal sealed class InAppPurchaseActivityTracker : ActivityTrackerProvider;
