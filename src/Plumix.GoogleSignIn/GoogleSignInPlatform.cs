// Dart parity source (reference): pub.dev/packages/google_sign_in_platform_interface 3.1.0,
// lib/google_sign_in_platform_interface.dart, lib/src/types.dart (approximate)

namespace Plumix.GoogleSignIn;

/// <summary>
/// The platform side of <see cref="GoogleSignIn"/>. The package sets <see cref="Instance"/> to the implementation of
/// the platform it was built for; tests replace it with a fake. Equivalent to <c>GoogleSignInPlatform</c>.
/// </summary>
public abstract class GoogleSignInPlatform
{
    /// <summary>The implementation in use.</summary>
    public static GoogleSignInPlatform Instance { get; set; } = CreateDefault();

    /// <summary>Sets the client up.</summary>
    public abstract Task InitAsync(InitParameters parameters);

    /// <summary>Signs in a returning user with minimal interaction; null when nobody is signed in.</summary>
    public abstract Task<AuthenticationResults?> AttemptLightweightAuthenticationAsync(
        AttemptLightweightAuthenticationParameters parameters);

    /// <summary>Whether <see cref="AuthenticateAsync"/> is supported.</summary>
    public abstract bool SupportsAuthenticate();

    /// <summary>Shows the sign-in UI.</summary>
    public abstract Task<AuthenticationResults> AuthenticateAsync(AuthenticateParameters parameters);

    /// <summary>Signs out.</summary>
    public abstract Task SignOutAsync();

    /// <summary>Signs out and revokes the grant.</summary>
    public abstract Task DisconnectAsync();

#pragma warning disable CA1859 // the return type is the abstraction on purpose: each TFM returns another class
    private static GoogleSignInPlatform CreateDefault() =>
#if ANDROID
        new AndroidGoogleSignIn();
#elif IOS
        new IosGoogleSignIn();
#else
        new UnsupportedGoogleSignIn();
#endif
#pragma warning restore CA1859
}

/// <summary>The parameters of <see cref="GoogleSignInPlatform.InitAsync"/>.</summary>
/// <param name="ClientId">This platform's OAuth client id.</param>
/// <param name="ServerClientId">The backend's (web) client id.</param>
/// <param name="Nonce">A nonce for the ID token.</param>
/// <param name="HostedDomain">A Google Workspace domain to restrict accounts to.</param>
public sealed record InitParameters(string? ClientId, string? ServerClientId, string? Nonce, string? HostedDomain);

/// <summary>The parameters of a lightweight authentication (none yet).</summary>
public sealed record AttemptLightweightAuthenticationParameters;

/// <summary>The parameters of <see cref="GoogleSignInPlatform.AuthenticateAsync"/>.</summary>
/// <param name="ScopeHint">Scopes the app will ask for later.</param>
public sealed record AuthenticateParameters(IReadOnlyList<string> ScopeHint);

/// <summary>Who signed in and the tokens.</summary>
/// <param name="User">The user.</param>
/// <param name="AuthenticationTokens">The tokens.</param>
public sealed record AuthenticationResults(GoogleSignInUserData User, AuthenticationTokenData AuthenticationTokens);

/// <summary>A user as the platform reports it.</summary>
/// <param name="Email">The email address.</param>
/// <param name="Id">The stable account id (<c>sub</c>).</param>
/// <param name="DisplayName">The name.</param>
/// <param name="PhotoUrl">The profile picture.</param>
public sealed record GoogleSignInUserData(string Email, string Id, string? DisplayName = null, string? PhotoUrl = null);

/// <summary>The tokens of an authentication.</summary>
/// <param name="IdToken">The ID token.</param>
public sealed record AuthenticationTokenData(string? IdToken);
