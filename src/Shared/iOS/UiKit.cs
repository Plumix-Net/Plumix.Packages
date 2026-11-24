// What the iOS plugins need from UIKit: the main thread and the window to present system sheets over (the anchor of
// ASWebAuthenticationSession and ASAuthorizationController). Flutter plugins take the key window the same way
// (flutter/packages: google_sign_in_ios, sign_in_with_apple's SignInWithAppleAuthorizationController).

using Foundation;
using UIKit;

namespace Plumix.Plugins;

internal static class UiKit
{
    /// <summary>The app's key window; the first window of a foreground scene when none is key yet.</summary>
    public static UIWindow? KeyWindow()
    {
        var scenes = UIApplication.SharedApplication.ConnectedScenes.ToArray().OfType<UIWindowScene>().ToList();
        var windows = scenes
            .OrderBy(scene => scene.ActivationState == UISceneActivationState.ForegroundActive ? 0 : 1)
            .SelectMany(scene => scene.Windows)
            .ToList();
        return windows.FirstOrDefault(window => window.IsKeyWindow) ?? windows.FirstOrDefault();
    }

    /// <summary>Runs <paramref name="action"/> on the main thread and completes when it has run.</summary>
    public static Task<T> OnMainThreadAsync<T>(Func<T> action)
    {
        if (NSThread.IsMain)
            return Task.FromResult(action());

        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        NSRunLoop.Main.BeginInvokeOnMainThread(() =>
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

    /// <summary>Runs <paramref name="action"/> on the main thread.</summary>
    public static Task OnMainThreadAsync(Action action) => OnMainThreadAsync(() =>
    {
        action();
        return true;
    });
}
