using Android.Content;
using Plumix.Plugins;

namespace Plumix.GoogleSignIn;

/// <summary>Starts following the app's activities at process start (see <see cref="ActivityTracker"/>).</summary>
[ContentProvider(["${applicationId}.plumix-google-sign-in"], Exported = false)]
internal sealed class GoogleSignInActivityTracker : ActivityTrackerProvider;
