// Dart parity source (reference): pub.dev/packages/google_sign_in 7.2.0, lib/google_sign_in.dart (approximate)

namespace Plumix.GoogleSignIn;

/// <summary>
/// Signs the user in with their Google account and gives the app an OpenID Connect ID token for its backend.
/// Call <see cref="InitializeAsync"/> once, then <see cref="AuthenticateAsync"/> from a button (or
/// <see cref="AttemptLightweightAuthenticationAsync"/> at start for a returning user).
///
/// Equivalent to google_sign_in's <c>GoogleSignIn</c> (7.x API). Android: Credential Manager's "Sign in with Google"
/// (<c>GetSignInWithGoogleOption</c>; <c>serverClientId</c> — the web client id — is required and becomes the ID
/// token's audience). iOS: Google's OAuth 2.0 with PKCE in <c>ASWebAuthenticationSession</c>, as the GoogleSignIn SDK
/// does (<c>clientId</c> — the iOS client id — is required; the redirect uses its reversed form as the URL scheme,
/// which needs no Info.plist entry). Other platforms throw <see cref="GoogleSignInException"/> with
/// <see cref="GoogleSignInExceptionCode.ProviderConfigurationError"/>.
///
/// Deviations: the authorization client (<c>authorizationClient</c>: access tokens and server auth codes for scopes)
/// is not ported yet; the authentication stream is two events (<see cref="AuthenticationEvent"/>,
/// <see cref="AuthenticationError"/>) instead of a Dart stream with errors.
/// </summary>
#pragma warning disable CA1822 // instance members on purpose: the API is GoogleSignIn.Instance.X, as in Dart
public sealed class GoogleSignIn
{
    private GoogleSignIn()
    {
    }

    /// <summary>The single instance, as <c>GoogleSignIn.instance</c>.</summary>
    public static GoogleSignIn Instance { get; } = new();

    /// <summary>A sign-in or a sign-out happened (through this API).</summary>
    public event EventHandler<GoogleSignInAuthenticationEvent>? AuthenticationEvent;

    /// <summary>A sign-in failed (through this API).</summary>
    public event EventHandler<GoogleSignInException>? AuthenticationError;

    /// <summary>
    /// Sets the client up; call once before anything else.
    /// </summary>
    /// <param name="clientId">The OAuth client id of this app's platform (required on iOS; ignored on Android, which
    /// identifies the app by its package and signing key).</param>
    /// <param name="serverClientId">The web client id of the backend that verifies the ID token (required on Android,
    /// where it is the token's audience; on iOS it is passed as <c>audience</c> like the GoogleSignIn SDK).</param>
    /// <param name="nonce">A nonce put into the ID token.</param>
    /// <param name="hostedDomain">Restricts sign-in to accounts of this Google Workspace domain.</param>
    public Task InitializeAsync(string? clientId = null, string? serverClientId = null, string? nonce = null,
        string? hostedDomain = null) =>
        GoogleSignInPlatform.Instance.InitAsync(new InitParameters(clientId, serverClientId, nonce, hostedDomain));

    /// <summary>
    /// Signs in a returning user with as little interaction as the platform allows (Android: an account that signed in
    /// before, or the account chooser; iOS: the session saved in the Keychain). Null when there is nobody to sign in.
    /// </summary>
    /// <param name="reportAllExceptions">Also throw for cancellation, interruption and missing UI (otherwise those
    /// return null).</param>
    public async Task<GoogleSignInAccount?> AttemptLightweightAuthenticationAsync(bool reportAllExceptions = false)
    {
        try
        {
            var result = await GoogleSignInPlatform.Instance
                .AttemptLightweightAuthenticationAsync(new AttemptLightweightAuthenticationParameters())
                .ConfigureAwait(false);

            if (result is null)
                return null;

            var account = new GoogleSignInAccount(result.User, result.AuthenticationTokens);
            AuthenticationEvent?.Invoke(this, new GoogleSignInAuthenticationEventSignIn(account));
            return account;
        }
        catch (GoogleSignInException e)
        {
            AuthenticationError?.Invoke(this, e);

            if (reportAllExceptions || e.Code is not (GoogleSignInExceptionCode.Canceled
                    or GoogleSignInExceptionCode.Interrupted or GoogleSignInExceptionCode.UiUnavailable))
                throw;

            return null;
        }
    }

    /// <summary>Whether <see cref="AuthenticateAsync"/> works on this platform.</summary>
    public bool SupportsAuthenticate() => GoogleSignInPlatform.Instance.SupportsAuthenticate();

    /// <summary>
    /// Shows the platform's Google sign-in and returns the signed-in account.
    /// </summary>
    /// <param name="scopeHint">Scopes the app will ask for later; the platform may ask for them now.</param>
    /// <exception cref="GoogleSignInException">Cancelled, misconfigured or failed.</exception>
    public async Task<GoogleSignInAccount> AuthenticateAsync(IReadOnlyList<string>? scopeHint = null)
    {
        try
        {
            var result = await GoogleSignInPlatform.Instance
                .AuthenticateAsync(new AuthenticateParameters(scopeHint ?? []))
                .ConfigureAwait(false);

            var account = new GoogleSignInAccount(result.User, result.AuthenticationTokens);
            AuthenticationEvent?.Invoke(this, new GoogleSignInAuthenticationEventSignIn(account));
            return account;
        }
        catch (GoogleSignInException e)
        {
            AuthenticationError?.Invoke(this, e);
            throw;
        }
    }

    /// <summary>Signs out: the next lightweight authentication will not pick the account by itself.</summary>
    public Task SignOutAsync()
    {
        AuthenticationEvent?.Invoke(this, new GoogleSignInAuthenticationEventSignOut());
        return GoogleSignInPlatform.Instance.SignOutAsync();
    }

    /// <summary>Signs out and revokes what the user granted this app, where the platform can.</summary>
    public Task DisconnectAsync()
    {
        AuthenticationEvent?.Invoke(this, new GoogleSignInAuthenticationEventSignOut());
        return GoogleSignInPlatform.Instance.DisconnectAsync();
    }
}
#pragma warning restore CA1822
