// The Android activity a plugin shows its UI over. Flutter hands plugins the activity through ActivityAware
// (flutter/engine/.../embedding/engine/plugins/activity/ActivityPluginBinding.java); Plumix has no plugin registry, so
// each plugin follows the app's activities itself: a ContentProvider of the plugin (created by the OS before the first
// activity, as AndroidX Startup does) registers activity lifecycle callbacks on the Application.

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Uri = Android.Net.Uri;

namespace Plumix.Plugins;

/// <summary>The resumed (or last started) activity of the app, followed from the moment the process starts.</summary>
internal static class ActivityTracker
{
    private static readonly Lock Gate = new();
    private static WeakReference<Activity>? _current;
    private static bool _registered;

    /// <summary>The activity in front, or null when none is (the app in the background, or not started yet).</summary>
    public static Activity? Current
    {
        get
        {
            lock (Gate)
                return _current is not null && _current.TryGetTarget(out var activity) && !activity.IsFinishing
                    ? activity
                    : null;
        }
    }

    /// <summary>Starts following the app's activities; called by the plugin's provider at process start.</summary>
    public static void Register(Context context)
    {
        lock (Gate)
        {
            if (_registered || context.ApplicationContext is not Application application)
                return;

            application.RegisterActivityLifecycleCallbacks(new Callbacks());
            _registered = true;
        }
    }

    private static void Set(Activity activity)
    {
        lock (Gate)
            _current = new WeakReference<Activity>(activity);
    }

    private static void Clear(Activity activity)
    {
        lock (Gate)
        {
            if (_current is not null && _current.TryGetTarget(out var current) && ReferenceEquals(current, activity))
                _current = null;
        }
    }

    private sealed class Callbacks : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public void OnActivityCreated(Activity activity, Bundle? savedInstanceState) => Set(activity);

        public void OnActivityStarted(Activity activity) => Set(activity);

        public void OnActivityResumed(Activity activity) => Set(activity);

        public void OnActivityPaused(Activity activity)
        {
        }

        public void OnActivityStopped(Activity activity)
        {
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        public void OnActivityDestroyed(Activity activity) => Clear(activity);
    }
}

/// <summary>
/// A provider that only starts <see cref="ActivityTracker"/>: the OS creates providers before any activity. Each
/// plugin declares a subclass with its own authority (<c>${applicationId}.plumix-…</c>).
/// </summary>
internal abstract class ActivityTrackerProvider : ContentProvider
{
    /// <inheritdoc />
    public override bool OnCreate()
    {
        if (Context is { } context)
            ActivityTracker.Register(context);
        return true;
    }

    /// <inheritdoc />
    public override ICursor? Query(Uri uri, string[]? projection, string? selection, string[]? selectionArgs,
        string? sortOrder) => null;

    /// <inheritdoc />
    public override string? GetType(Uri uri) => null;

    /// <inheritdoc />
    public override Uri? Insert(Uri uri, ContentValues? values) => null;

    /// <inheritdoc />
    public override int Delete(Uri uri, string? selection, string[]? selectionArgs) => 0;

    /// <inheritdoc />
    public override int Update(Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) => 0;
}
