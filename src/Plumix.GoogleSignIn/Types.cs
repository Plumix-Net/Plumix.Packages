// Dart parity source (reference): pub.dev/packages/google_sign_in 7.2.0, lib/google_sign_in.dart (GoogleSignInAccount),
// lib/src/token_types.dart, lib/src/event_types.dart; google_sign_in_platform_interface 3.1.0, lib/src/types.dart
// (GoogleSignInException, GoogleSignInExceptionCode) (approximate)

namespace Plumix.GoogleSignIn;

/// <summary>
/// A signed-in Google user. Equivalent to google_sign_in's <c>GoogleSignInAccount</c>.
/// </summary>
public sealed class GoogleSignInAccount : IEquatable<GoogleSignInAccount>
{
    private readonly AuthenticationTokenData _tokens;

    internal GoogleSignInAccount(GoogleSignInUserData user, AuthenticationTokenData tokens)
    {
        DisplayName = user.DisplayName;
        Email = user.Email;
        Id = user.Id;
        PhotoUrl = user.PhotoUrl;
        _tokens = tokens;
    }

    /// <summary>The user's name, when Google shares it.</summary>
    public string? DisplayName { get; }

    /// <summary>The account's email address.</summary>
    public string Email { get; }

    /// <summary>The stable Google account id (the ID token's <c>sub</c>).</summary>
    public string Id { get; }

    /// <summary>The profile picture's URL, when there is one.</summary>
    public string? PhotoUrl { get; }

    /// <summary>The tokens of this sign-in.</summary>
    public GoogleSignInAuthentication Authentication => new(_tokens.IdToken);

    /// <inheritdoc />
    public bool Equals(GoogleSignInAccount? other) => other is not null
        && DisplayName == other.DisplayName && Email == other.Email && Id == other.Id && PhotoUrl == other.PhotoUrl
        && _tokens.IdToken == other._tokens.IdToken;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GoogleSignInAccount other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(DisplayName, Email, Id, PhotoUrl, _tokens.IdToken);

    /// <inheritdoc />
    public override string ToString() =>
        $"GoogleSignInAccount:{{displayName: {DisplayName}, email: {Email}, id: {Id}, photoUrl: {PhotoUrl}}}";
}

/// <summary>The tokens of an authentication. Equivalent to <c>GoogleSignInAuthentication</c>.</summary>
/// <param name="IdToken">The OpenID Connect ID token that identifies the user to a backend.</param>
public sealed record GoogleSignInAuthentication(string? IdToken);

/// <summary>Something that happened to the signed-in user. Equivalent to <c>GoogleSignInAuthenticationEvent</c>.</summary>
public abstract record GoogleSignInAuthenticationEvent;

/// <summary>A user signed in. Equivalent to <c>GoogleSignInAuthenticationEventSignIn</c>.</summary>
/// <param name="User">The user that was authenticated.</param>
public sealed record GoogleSignInAuthenticationEventSignIn(GoogleSignInAccount User) : GoogleSignInAuthenticationEvent;

/// <summary>The user signed out. Equivalent to <c>GoogleSignInAuthenticationEventSignOut</c>.</summary>
public sealed record GoogleSignInAuthenticationEventSignOut : GoogleSignInAuthenticationEvent;

/// <summary>Why a Google sign-in failed. Equivalent to <c>GoogleSignInExceptionCode</c>.</summary>
public enum GoogleSignInExceptionCode
{
    /// <summary>A failure without a more specific code; see the description.</summary>
    UnknownError,

    /// <summary>The user cancelled.</summary>
    Canceled,

    /// <summary>Interrupted for a reason other than the user cancelling.</summary>
    Interrupted,

    /// <summary>The app's client is misconfigured (a missing or wrong client id).</summary>
    ClientConfigurationError,

    /// <summary>The platform's sign-in provider is missing or misconfigured (or the platform has none).</summary>
    ProviderConfigurationError,

    /// <summary>Sign-in needed to show UI and could not (no activity or window in front).</summary>
    UiUnavailable,

    /// <summary>The operation was for a user other than the signed-in one.</summary>
    UserMismatch
}

/// <summary>A failed Google sign-in operation. Equivalent to <c>GoogleSignInException</c>.</summary>
public sealed class GoogleSignInException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GoogleSignInException(GoogleSignInExceptionCode code, string? description = null, object? details = null)
        : base($"GoogleSignInException(code {code}, {description}, {details})")
    {
        Code = code;
        Description = description;
        Details = details;
    }

    /// <summary>Why it failed.</summary>
    public GoogleSignInExceptionCode Code { get; }

    /// <summary>A human-readable description, for developers.</summary>
    public string? Description { get; }

    /// <summary>Platform details, when there are any.</summary>
    public object? Details { get; }
}
