// Dart parity source (reference): pub.dev/packages/sign_in_with_apple_platform_interface 2.0.0,
// lib/authorization_credential.dart, lib/authorization_request.dart, lib/credential_state.dart, lib/exceptions.dart,
// lib/web_authentication_options.dart (approximate)

namespace Plumix.SignInWithApple;

/// <summary>What an Apple ID authorization asks for. Equivalent to <c>AppleIDAuthorizationScopes</c>.</summary>
public enum AppleIDAuthorizationScopes
{
    /// <summary>The user's email (possibly a private relay address).</summary>
    Email,

    /// <summary>The user's name.</summary>
    FullName
}

/// <summary>An Apple ID authorization request. Equivalent to <c>AppleIDAuthorizationRequest</c>.</summary>
/// <param name="Scopes">What to ask for.</param>
/// <param name="Nonce">The nonce for the identity token.</param>
/// <param name="State">Opaque state returned in the credential.</param>
public sealed record AppleIDAuthorizationRequest(IReadOnlyList<AppleIDAuthorizationScopes> Scopes, string? Nonce = null,
    string? State = null);

/// <summary>
/// The result of a successful Apple ID authorization. Equivalent to <c>AuthorizationCredentialAppleID</c>.
/// </summary>
/// <param name="UserIdentifier">The stable user identifier (the identity token's <c>sub</c>).</param>
/// <param name="GivenName">The first name; only on the app's first authorization, when asked for.</param>
/// <param name="FamilyName">The last name; only on the first authorization.</param>
/// <param name="Email">The email; only on the first authorization (the identity token carries it every time).</param>
/// <param name="AuthorizationCode">A one-time code (valid five minutes) the backend can exchange with Apple.</param>
/// <param name="IdentityToken">The JWT Apple signed for this app.</param>
/// <param name="State">The request's state.</param>
public sealed record AuthorizationCredentialAppleID(string? UserIdentifier, string? GivenName, string? FamilyName,
    string? Email, string AuthorizationCode, string? IdentityToken, string? State)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"AuthorizationCredentialAppleID({UserIdentifier}, {GivenName}, {FamilyName}, {Email}, " +
        $"[identityToken set: {IdentityToken is not null}], {State})";
}

/// <summary>The state of a user's Apple ID credential for this app. Equivalent to <c>CredentialState</c>.</summary>
public enum CredentialState
{
    /// <summary>The user is authorized.</summary>
    Authorized,

    /// <summary>The user revoked the authorization (signed out of the app in Settings).</summary>
    Revoked,

    /// <summary>No credential for the identifier.</summary>
    NotFound
}

/// <summary>
/// The web flow's options (Services ID and redirect). Kept for API parity; this port has no web flow.
/// Equivalent to <c>WebAuthenticationOptions</c>.
/// </summary>
/// <param name="ClientId">The Services ID.</param>
/// <param name="RedirectUri">The redirect registered for it.</param>
public sealed record WebAuthenticationOptions(string ClientId, Uri RedirectUri);

/// <summary>Why an authorization failed. Equivalent to <c>AuthorizationErrorCode</c>.</summary>
public enum AuthorizationErrorCode
{
    /// <summary>The user cancelled.</summary>
    Canceled,

    /// <summary>The authorization failed.</summary>
    Failed,

    /// <summary>The authorization response was invalid.</summary>
    InvalidResponse,

    /// <summary>The request was not handled.</summary>
    NotHandled,

    /// <summary>The request needed interaction and could not show UI.</summary>
    NotInteractive,

    /// <summary>The credential matched an excluded one.</summary>
    MatchedExcludedCredential,

    /// <summary>Importing the credential failed.</summary>
    CredentialImport,

    /// <summary>Exporting the credential failed.</summary>
    CredentialExport,

    /// <summary>An unknown error (also: the app lacks the Sign in with Apple entitlement).</summary>
    Unknown
}

/// <summary>A failed Sign in with Apple call. Equivalent to <c>SignInWithAppleException</c>.</summary>
public abstract class SignInWithAppleException : Exception
{
    /// <summary>Creates the exception.</summary>
    protected SignInWithAppleException(string message) : base(message)
    {
    }
}

/// <summary>The authorization failed or was cancelled. Equivalent to <c>SignInWithAppleAuthorizationException</c>.</summary>
public sealed class SignInWithAppleAuthorizationException : SignInWithAppleException
{
    /// <summary>Creates the exception.</summary>
    public SignInWithAppleAuthorizationException(AuthorizationErrorCode code, string message)
        : base($"SignInWithAppleAuthorizationError({code}, {message})")
    {
        Code = code;
        Reason = message;
    }

    /// <summary>Why it failed.</summary>
    public AuthorizationErrorCode Code { get; }

    /// <summary>The platform's message.</summary>
    public string Reason { get; }
}

/// <summary>Sign in with Apple is not available here. Equivalent to <c>SignInWithAppleNotSupportedException</c>.</summary>
public sealed class SignInWithAppleNotSupportedException(string message)
    : SignInWithAppleException($"SignInWithAppleNotSupportedException({message})");

/// <summary>The credential state could not be read. Equivalent to <c>SignInWithAppleCredentialsException</c>.</summary>
public sealed class SignInWithAppleCredentialsException(string message)
    : SignInWithAppleException($"SignInWithAppleCredentialsException({message})");
