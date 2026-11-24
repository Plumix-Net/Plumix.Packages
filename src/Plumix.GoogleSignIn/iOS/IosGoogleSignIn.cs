// Dart parity source (reference): pub.dev/packages/google_sign_in_ios 6.3.7 (lib/google_sign_in_ios.dart) over the
// GoogleSignIn-iOS SDK (GIDSignIn: AppAuth's authorization code flow with PKCE in ASWebAuthenticationSession, the
// session kept in the Keychain) (approximate)

using AuthenticationServices;
using Foundation;
using Plumix.Plugins;
using Security;
using UIKit;

namespace Plumix.GoogleSignIn;

/// <summary>
/// iOS: Google's OAuth 2.0 for native apps, as the GoogleSignIn SDK runs it — the authorization page in
/// <c>ASWebAuthenticationSession</c> (shares Safari's Google session), PKCE S256, the code exchanged at Google's token
/// endpoint for an ID token. The refresh token stays in the Keychain, so a lightweight authentication signs a returning
/// user in without UI; without a Keychain (an unsigned simulator build) there is just nothing to restore.
/// </summary>
internal sealed class IosGoogleSignIn : GoogleSignInPlatform
{
    private const string KeychainService = "net.plumix.google_sign_in";
    private const string KeychainAccount = "session";

    private static readonly HttpClient Http = new();

    private InitParameters _parameters = new(null, null, null, null);
    private ASWebAuthenticationSession? _session;

    public override Task InitAsync(InitParameters parameters)
    {
        _parameters = parameters;
        return Task.CompletedTask;
    }

    public override async Task<AuthenticationResults?> AttemptLightweightAuthenticationAsync(
        AttemptLightweightAuthenticationParameters parameters)
    {
        var clientId = ClientId();
        var refreshToken = Keychain.Read();
        if (refreshToken is null)
            return null;

        TokenResponse tokens;
        try
        {
            tokens = await PostAsync(GoogleOAuth.Refresh(clientId, refreshToken)).ConfigureAwait(false);
        }
        catch (GoogleSignInException)
        {
            // Revoked or expired: the session is gone
            Keychain.Delete();
            return null;
        }

        return new AuthenticationResults(GoogleOAuth.UserFromIdToken(tokens.IdToken),
            new AuthenticationTokenData(tokens.IdToken));
    }

    public override bool SupportsAuthenticate() => true;

    public override async Task<AuthenticationResults> AuthenticateAsync(AuthenticateParameters parameters)
    {
        var clientId = ClientId();
        var verifier = GoogleOAuth.CreateVerifier();
        var state = GoogleOAuth.CreateState();
        var url = GoogleOAuth.AuthorizationUrl(clientId, GoogleOAuth.Challenge(verifier), state, parameters.ScopeHint,
            _parameters.Nonce, _parameters.HostedDomain, _parameters.ServerClientId);

        var redirect = await AuthorizeAsync(url, GoogleOAuth.RedirectScheme(clientId)).ConfigureAwait(false);
        var code = GoogleOAuth.ReadRedirect(redirect, state);
        var tokens = await PostAsync(GoogleOAuth.CodeExchange(clientId, code, verifier)).ConfigureAwait(false);

        if (tokens.RefreshToken is { } refreshToken)
            Keychain.Write(refreshToken);

        return new AuthenticationResults(GoogleOAuth.UserFromIdToken(tokens.IdToken),
            new AuthenticationTokenData(tokens.IdToken));
    }

    public override Task SignOutAsync()
    {
        Keychain.Delete();
        return Task.CompletedTask;
    }

    public override async Task DisconnectAsync()
    {
        var refreshToken = Keychain.Read();
        Keychain.Delete();

        if (refreshToken is null)
            return;

        try
        {
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", refreshToken)]);
            using var response = await Http.PostAsync(GoogleOAuth.RevocationEndpoint, content).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Signed out here anyway; the grant expires on its own
        }
    }

    private string ClientId() => _parameters.ClientId
        ?? throw new GoogleSignInException(GoogleSignInExceptionCode.ClientConfigurationError,
            "clientId (the iOS OAuth client id) must be provided on iOS");

    private Task<Uri> AuthorizeAsync(Uri url, string callbackScheme)
    {
        var done = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

        return UiKit.OnMainThreadAsync(() =>
        {
            var window = UiKit.KeyWindow()
                ?? throw new GoogleSignInException(GoogleSignInExceptionCode.UiUnavailable,
                    "No window is available to show the sign-in UI.");

            ASWebAuthenticationSessionCompletionHandler completion = (callback, error) =>
                {
                    _session = null;

                    if (callback?.AbsoluteString is { } answer)
                        done.TrySetResult(new Uri(answer));
                    else if (error is not null && error.Domain == ASWebAuthenticationSessionErrorCode.CanceledLogin.GetDomain()
                             && error.Code == (long) ASWebAuthenticationSessionErrorCode.CanceledLogin)
                        done.TrySetException(new GoogleSignInException(GoogleSignInExceptionCode.Canceled,
                            "The user canceled the sign-in flow."));
                    else
                        done.TrySetException(new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                            error?.LocalizedDescription ?? "The sign-in page returned nothing."));
                };

            var authorization = new NSUrl(url.AbsoluteUri);
            var session = OperatingSystem.IsIOSVersionAtLeast(17, 4)
                ? new ASWebAuthenticationSession(authorization, ASWebAuthenticationSessionCallback.Create(callbackScheme),
                    completion)
                : Legacy(authorization, callbackScheme, completion);
            session.PresentationContextProvider = new Anchor(window);
            // Keep Safari's Google session, as the SDK does: a returning user just picks the account
            session.PrefersEphemeralWebBrowserSession = false;

            _session = session;
            if (!session.Start())
            {
                _session = null;
                done.TrySetException(new GoogleSignInException(GoogleSignInExceptionCode.UiUnavailable,
                    "The sign-in page could not be shown."));
            }
        }).ContinueWith(started => started.IsFaulted ? Task.FromException<Uri>(started.Exception!.InnerException!)
            : done.Task, TaskScheduler.Default).Unwrap();
    }

    [System.Runtime.Versioning.ObsoletedOSPlatform("ios17.4")]
    private static ASWebAuthenticationSession Legacy(NSUrl url, string scheme,
        ASWebAuthenticationSessionCompletionHandler completion) => new(url, scheme, completion);

    private static async Task<TokenResponse> PostAsync(FormUrlEncodedContent form)
    {
        using (form)
        {
            try
            {
                using var response = await Http.PostAsync(GoogleOAuth.TokenEndpoint, form).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return GoogleOAuth.ReadTokenResponse((int) response.StatusCode, body);
            }
            catch (HttpRequestException e)
            {
                throw new GoogleSignInException(GoogleSignInExceptionCode.Interrupted,
                    $"Google's token endpoint is unreachable: {e.Message}");
            }
        }
    }

    private sealed class Anchor(UIWindow window) : NSObject, IASWebAuthenticationPresentationContextProviding
    {
        public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session) => window;
    }

    /// <summary>The refresh token as a generic password, readable after the first unlock.</summary>
    private static class Keychain
    {
        private static SecRecord Query() => new(SecKind.GenericPassword)
        {
            Service = KeychainService,
            Account = KeychainAccount
        };

        public static string? Read()
        {
            var match = SecKeyChain.QueryAsData(Query(), false, out var status);
            return status == SecStatusCode.Success && match is not null ? match.ToString(NSStringEncoding.UTF8) : null;
        }

        public static void Write(string refreshToken)
        {
            Delete();
            var record = Query();
            record.ValueData = NSData.FromString(refreshToken, NSStringEncoding.UTF8);
            record.Accessible = SecAccessible.AfterFirstUnlockThisDeviceOnly;
            // errSecMissingEntitlement on an unsigned simulator build: no lightweight sign-in there
            SecKeyChain.Add(record);
        }

        public static void Delete() => SecKeyChain.Remove(Query());
    }
}
