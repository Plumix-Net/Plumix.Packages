// Dart parity source (reference): pub.dev/packages/sign_in_with_apple 8.2.0, lib/src/sign_in_with_apple.dart;
// sign_in_with_apple_platform_interface 2.0.0 (approximate)

namespace Plumix.SignInWithApple;

/// <summary>
/// Sign in with Apple. On iOS it shows Apple's native sheet (<c>ASAuthorizationController</c>) and returns the user's
/// identifier, the identity token (a JWT signed by Apple, audience — the app's bundle id) and the one-time
/// authorization code for the backend. The app needs the "Sign in with Apple" capability: the
/// <c>com.apple.developer.applesignin</c> entitlement (<c>Default</c>) and the App ID's capability.
///
/// Equivalent to sign_in_with_apple's <c>SignInWithApple</c>. Deviations: the web flow for Android and other platforms
/// (<c>webAuthenticationOptions</c>) and <c>getKeychainCredential</c> are not ported — elsewhere
/// <see cref="IsAvailableAsync"/> is false and the calls throw <see cref="SignInWithAppleNotSupportedException"/>.
/// </summary>
public static class SignInWithApple
{
    /// <summary>Whether Sign in with Apple works here (iOS 13+).</summary>
    public static Task<bool> IsAvailableAsync() => SignInWithApplePlatform.Instance.IsAvailableAsync();

    /// <summary>
    /// Asks the user to sign in with their Apple ID. Name and email come only on the first authorization of the app
    /// (Apple shares them once); later sign-ins carry the identifier and the tokens.
    /// </summary>
    /// <param name="scopes">What to ask for: the email, the full name.</param>
    /// <param name="webAuthenticationOptions">Options of the web flow; not supported by this port (ignored on iOS).</param>
    /// <param name="nonce">Put into the identity token's <c>nonce</c> claim as given (pass a SHA-256 of a raw nonce
    /// when the backend expects that).</param>
    /// <param name="state">Returned unchanged in the credential.</param>
    /// <exception cref="SignInWithAppleAuthorizationException">Cancelled or failed.</exception>
    /// <exception cref="SignInWithAppleNotSupportedException">Not iOS.</exception>
    public static Task<AuthorizationCredentialAppleID> GetAppleIDCredentialAsync(
        IReadOnlyList<AppleIDAuthorizationScopes> scopes, WebAuthenticationOptions? webAuthenticationOptions = null,
        string? nonce = null, string? state = null) =>
        SignInWithApplePlatform.Instance.GetAppleIDCredentialAsync(new AppleIDAuthorizationRequest(scopes, nonce, state));

    /// <summary>Whether the user's Apple ID still authorizes this app.</summary>
    /// <param name="userIdentifier">The <see cref="AuthorizationCredentialAppleID.UserIdentifier"/> of a sign-in.</param>
    public static Task<CredentialState> GetCredentialStateAsync(string userIdentifier) =>
        SignInWithApplePlatform.Instance.GetCredentialStateAsync(userIdentifier);
}

/// <summary>The platform side of <see cref="SignInWithApple"/>; replaceable for tests.</summary>
public abstract class SignInWithApplePlatform
{
    /// <summary>The implementation in use.</summary>
    public static SignInWithApplePlatform Instance { get; set; } = CreateDefault();

    /// <summary>Whether Sign in with Apple works here.</summary>
    public abstract Task<bool> IsAvailableAsync();

    /// <summary>Runs the Apple ID authorization.</summary>
    public abstract Task<AuthorizationCredentialAppleID> GetAppleIDCredentialAsync(AppleIDAuthorizationRequest request);

    /// <summary>The state of a user's credential.</summary>
    public abstract Task<CredentialState> GetCredentialStateAsync(string userIdentifier);

#pragma warning disable CA1859 // the return type is the abstraction on purpose: each TFM returns another class
    private static SignInWithApplePlatform CreateDefault() =>
#if IOS
        new IosSignInWithApple();
#else
        new UnsupportedSignInWithApple();
#endif
#pragma warning restore CA1859
}
