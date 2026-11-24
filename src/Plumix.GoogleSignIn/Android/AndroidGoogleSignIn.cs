// Dart parity source (reference): pub.dev/packages/google_sign_in_android 7.2.18, lib/google_sign_in_android.dart and
// android/src/main/kotlin/io/flutter/plugins/googlesignin/GoogleSignInPlugin.kt (approximate)

using Android.OS;
using AndroidX.Credentials;
using AndroidX.Credentials.Exceptions;
using Java.Util.Concurrent;
using Plumix.Plugins;
using Xamarin.GoogleAndroid.Libraries.Identity.GoogleId;

namespace Plumix.GoogleSignIn;

/// <summary>
/// Android: Credential Manager with Google's ID option. <see cref="AuthenticateAsync"/> shows the "Sign in with
/// Google" sheet (<c>GetSignInWithGoogleOption</c>); a lightweight authentication first tries an account that signed
/// in before without UI, then the one-tap account chooser (<c>GetGoogleIdOption</c>). The ID token's audience is the
/// server client id. Disconnect clears the credential state like sign-out: revoking granted scopes belongs to the
/// authorization client, which is not ported.
/// </summary>
internal sealed class AndroidGoogleSignIn : GoogleSignInPlatform
{
    private const string GoogleIdTokenCredentialType =
        "com.google.android.libraries.identity.googleid.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL";

    private static readonly IExecutor Executor = Executors.NewSingleThreadExecutor()!;

    private InitParameters _parameters = new(null, null, null, null);

    public override Task InitAsync(InitParameters parameters)
    {
        _parameters = parameters;
        return Task.CompletedTask;
    }

    public override async Task<AuthenticationResults?> AttemptLightweightAuthenticationAsync(
        AttemptLightweightAuthenticationParameters parameters)
    {
        var credential = await GetCredentialAsync(buttonFlow: false, filterToAuthorized: true, autoSelect: true,
            throwForNoCredential: false).ConfigureAwait(false);

        // One-tap does not filter by hosted domain: with a domain set, only accounts that signed in before qualify
        if (credential is null && _parameters.HostedDomain is null)
            credential = await GetCredentialAsync(buttonFlow: false, filterToAuthorized: false, autoSelect: false,
                throwForNoCredential: false).ConfigureAwait(false);

        return credential is null ? null : ToResults(credential);
    }

    public override bool SupportsAuthenticate() => true;

    public override async Task<AuthenticationResults> AuthenticateAsync(AuthenticateParameters parameters)
    {
        var credential = await GetCredentialAsync(buttonFlow: true, filterToAuthorized: false, autoSelect: false,
            throwForNoCredential: true).ConfigureAwait(false);

        return credential is null
            ? throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                "Authenticate returned no credential without an error")
            : ToResults(credential);
    }

    public override Task SignOutAsync() => ClearCredentialStateAsync();

    public override Task DisconnectAsync() => ClearCredentialStateAsync();

    private async Task<GoogleIdTokenCredential?> GetCredentialAsync(bool buttonFlow, bool filterToAuthorized,
        bool autoSelect, bool throwForNoCredential)
    {
        var serverClientId = _parameters.ServerClientId
            ?? throw new GoogleSignInException(GoogleSignInExceptionCode.ClientConfigurationError,
                "serverClientId must be provided on Android");

        var activity = ActivityTracker.Current
            ?? throw new GoogleSignInException(GoogleSignInExceptionCode.UiUnavailable,
                "No activity is available to show the sign-in UI.");

        CredentialOption option;
        if (buttonFlow)
        {
            var builder = new GetSignInWithGoogleOption.Builder(serverClientId);
            if (_parameters.HostedDomain is { } domain)
                builder.SetHostedDomainFilter(domain);
            if (_parameters.Nonce is { } nonce)
                builder.SetNonce(nonce);
            option = builder.Build();
        }
        else
        {
            var builder = new GetGoogleIdOption.Builder()
                .SetFilterByAuthorizedAccounts(filterToAuthorized)
                .SetAutoSelectEnabled(autoSelect)
                .SetServerClientId(serverClientId);
            if (_parameters.Nonce is { } nonce)
                builder.SetNonce(nonce);
            option = builder.Build();
        }

        var request = new GetCredentialRequest.Builder().AddCredentialOption(option).Build();
        var manager = CredentialManager.Create(activity);
        var callback = new Callback();

        manager.GetCredentialAsync(activity, request, new CancellationSignal(), Executor, callback);

        Java.Lang.Object? answer;
        try
        {
            answer = await callback.Task.ConfigureAwait(false);
        }
        catch (CredentialFailure failure)
        {
            if (failure.Error is NoCredentialException && !throwForNoCredential)
                return null;

            throw ToException(failure.Error, throwForNoCredential);
        }

        if (answer is not GetCredentialResponse { Credential: CustomCredential custom }
            || custom.Type != GoogleIdTokenCredentialType)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                $"Unexpected credential type: {(answer as GetCredentialResponse)?.Credential?.Type}");

        return GoogleIdTokenCredential.CreateFrom(custom.Data);
    }

    private static Task ClearCredentialStateAsync()
    {
        var context = ActivityTracker.Current ?? (Android.Content.Context?) Android.App.Application.Context;
        var manager = CredentialManager.Create(context!);
        var callback = new Callback();
        manager.ClearCredentialStateAsync(new ClearCredentialStateRequest(), new CancellationSignal(), Executor,
            callback);
        return callback.Task.ContinueWith(task =>
        {
            if (task.Exception?.InnerException is CredentialFailure failure)
                throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError, failure.Error?.ToString());
        }, TaskScheduler.Default);
    }

    private static AuthenticationResults ToResults(GoogleIdTokenCredential credential)
    {
        // The credential's id is the email; the stable account id is the ID token's subject
        var claims = IdTokenClaims.Read(credential.IdToken);
        var user = new GoogleSignInUserData(credential.Id, claims.Subject ?? credential.Id, credential.DisplayName,
            credential.ProfilePictureUri?.ToString());
        return new AuthenticationResults(user, new AuthenticationTokenData(credential.IdToken));
    }

    private static GoogleSignInException ToException(Java.Lang.Throwable? error, bool noCredentialIsUnknown)
    {
        var message = (error as GetCredentialException)?.ErrorMessage?.ToString() ?? error?.Message;

        return error switch
        {
            GetCredentialCancellationException => new GoogleSignInException(GoogleSignInExceptionCode.Canceled,
                message),
            GetCredentialInterruptedException => new GoogleSignInException(GoogleSignInExceptionCode.Interrupted,
                message),
            GetCredentialProviderConfigurationException => new GoogleSignInException(
                GoogleSignInExceptionCode.ProviderConfigurationError, message),
            NoCredentialException when noCredentialIsUnknown => new GoogleSignInException(
                GoogleSignInExceptionCode.UnknownError, $"No credential available: {message}"),
            _ => new GoogleSignInException(GoogleSignInExceptionCode.UnknownError, message)
        };
    }

    private sealed class CredentialFailure(Java.Lang.Throwable? error) : Exception(error?.Message)
    {
        public Java.Lang.Throwable? Error { get; } = error;
    }

    private sealed class Callback : Java.Lang.Object, ICredentialManagerCallback
    {
        private readonly TaskCompletionSource<Java.Lang.Object?> _done =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Java.Lang.Object?> Task => _done.Task;

        public void OnResult(Java.Lang.Object? result) => _done.TrySetResult(result);

        // The error arrives as a Java object: take its most derived managed wrapper (GetCredentialCancellationException…)
        public void OnError(Java.Lang.Object error) => _done.TrySetException(new CredentialFailure(
            Java.Lang.Object.GetObject<Java.Lang.Throwable>(error.Handle, Android.Runtime.JniHandleOwnership.DoNotTransfer)));
    }
}
