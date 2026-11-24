// The OAuth 2.0 for native apps flow the GoogleSignIn iOS SDK runs through AppAuth (RFC 8252, PKCE RFC 7636):
// google_sign_in_ios → GoogleSignIn-iOS GIDSignIn.m. Platform-free so it is tested on plain .NET; the iOS
// implementation drives it through ASWebAuthenticationSession.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Plumix.GoogleSignIn;

/// <summary>Google's OAuth endpoints and the pieces of the authorization-code flow with PKCE.</summary>
internal static class GoogleOAuth
{
    public static readonly Uri AuthorizationEndpoint = new("https://accounts.google.com/o/oauth2/v2/auth");
    public static readonly Uri TokenEndpoint = new("https://oauth2.googleapis.com/token");
    public static readonly Uri RevocationEndpoint = new("https://oauth2.googleapis.com/revoke");

    private const string ClientIdSuffix = ".apps.googleusercontent.com";

    /// <summary>
    /// The URL scheme Google redirects an iOS client to: the client id reversed,
    /// <c>123-abc.apps.googleusercontent.com</c> → <c>com.googleusercontent.apps.123-abc</c>.
    /// </summary>
    public static string RedirectScheme(string clientId)
    {
        if (!clientId.EndsWith(ClientIdSuffix, StringComparison.Ordinal) || clientId.Length == ClientIdSuffix.Length)
            throw new GoogleSignInException(GoogleSignInExceptionCode.ClientConfigurationError,
                $"'{clientId}' is not a Google OAuth client id (…{ClientIdSuffix}).");

        return "com.googleusercontent.apps." + clientId[..^ClientIdSuffix.Length];
    }

    /// <summary>The redirect URI registered for an iOS client: <c>&lt;reversed client id&gt;:/oauth2redirect</c>.</summary>
    public static string RedirectUri(string clientId) => RedirectScheme(clientId) + ":/oauth2redirect";

    /// <summary>A PKCE code verifier: 32 random bytes, base64url (43 characters).</summary>
    public static string CreateVerifier() => Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>A random <c>state</c> value.</summary>
    public static string CreateState() => Base64Url(RandomNumberGenerator.GetBytes(16));

    /// <summary>The S256 challenge of a verifier.</summary>
    public static string Challenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    /// <summary>The authorization request's URL.</summary>
    public static Uri AuthorizationUrl(string clientId, string codeChallenge, string state, IEnumerable<string> scopes,
        string? nonce, string? hostedDomain, string? serverClientId, string? loginHint = null)
    {
        var allScopes = new List<string> { "openid", "email", "profile" };
        foreach (var scope in scopes)
            if (!allScopes.Contains(scope))
                allScopes.Add(scope);

        var query = new List<(string Key, string Value)>
        {
            ("client_id", clientId),
            ("redirect_uri", RedirectUri(clientId)),
            ("response_type", "code"),
            ("scope", string.Join(' ', allScopes)),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", "S256"),
            ("state", state),
            ("include_granted_scopes", "true")
        };

        if (nonce is not null)
            query.Add(("nonce", nonce));
        if (hostedDomain is not null)
            query.Add(("hd", hostedDomain));
        // GIDSignIn puts the server client id into the request as "audience": the ID token is then issued for it
        if (serverClientId is not null)
            query.Add(("audience", serverClientId));
        if (loginHint is not null)
            query.Add(("login_hint", loginHint));

        return new Uri(AuthorizationEndpoint + "?" + Encode(query));
    }

    /// <summary>
    /// The code from the redirect, after checking its <c>state</c>. Throws for an error answer (<c>access_denied</c> —
    /// cancelled) or a state that is not ours.
    /// </summary>
    public static string ReadRedirect(Uri redirect, string expectedState)
    {
        var query = ParseQuery(redirect.Query);

        if (query.TryGetValue("error", out var error))
            throw new GoogleSignInException(
                error == "access_denied" ? GoogleSignInExceptionCode.Canceled : GoogleSignInExceptionCode.UnknownError,
                $"Google answered '{error}'" + (query.TryGetValue("error_description", out var description)
                    ? $": {description}"
                    : "."));

        if (!query.TryGetValue("state", out var state) || state != expectedState)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                "The redirect's state does not match the request.");

        if (!query.TryGetValue("code", out var code) || code.Length == 0)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError, "The redirect carries no code.");

        return code;
    }

    /// <summary>The form of the code exchange.</summary>
    public static FormUrlEncodedContent CodeExchange(string clientId, string code, string verifier) => new(
    [
        new("grant_type", "authorization_code"),
        new("client_id", clientId),
        new("code", code),
        new("code_verifier", verifier),
        new("redirect_uri", RedirectUri(clientId))
    ]);

    /// <summary>The form of a refresh.</summary>
    public static FormUrlEncodedContent Refresh(string clientId, string refreshToken) => new(
    [
        new("grant_type", "refresh_token"),
        new("client_id", clientId),
        new("refresh_token", refreshToken)
    ]);

    /// <summary>Reads a token endpoint answer; throws a <see cref="GoogleSignInException"/> for an error answer.</summary>
    public static TokenResponse ReadTokenResponse(int status, string body)
    {
        using var json = Parse(body);
        var root = json?.RootElement;

        if (status is < 200 or > 299)
        {
            var error = root is { ValueKind: JsonValueKind.Object } r && r.TryGetProperty("error", out var e)
                ? e.GetString()
                : null;
            throw new GoogleSignInException(
                error is "invalid_client" or "unauthorized_client"
                    ? GoogleSignInExceptionCode.ClientConfigurationError
                    : GoogleSignInExceptionCode.UnknownError,
                $"Google's token endpoint answered {status} {error}.");
        }

        if (root is not { ValueKind: JsonValueKind.Object } answer || String(answer, "id_token") is not { } idToken)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                "Google's token endpoint returned no ID token.");

        return new TokenResponse(idToken, String(answer, "access_token"), String(answer, "refresh_token"));
    }

    /// <summary>The user an ID token names, from its payload (the token's signature is the backend's to check).</summary>
    public static GoogleSignInUserData UserFromIdToken(string idToken)
    {
        var claims = IdTokenClaims.Read(idToken);

        if (claims.Subject is null || claims.Email is null)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError,
                "The ID token carries no subject or email.");

        return new GoogleSignInUserData(claims.Email, claims.Subject, claims.Name, claims.Picture);
    }

    internal static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var at = pair.IndexOf('=', StringComparison.Ordinal);
            var key = Uri.UnescapeDataString((at < 0 ? pair : pair[..at]).Replace('+', ' '));
            var value = at < 0 ? "" : Uri.UnescapeDataString(pair[(at + 1)..].Replace('+', ' '));
            result.TryAdd(key, value);
        }

        return result;
    }

    private static string Encode(IEnumerable<(string Key, string Value)> query) =>
        string.Join('&', query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static JsonDocument? Parse(string body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

/// <summary>What the token endpoint answered.</summary>
internal sealed record TokenResponse(string IdToken, string? AccessToken, string? RefreshToken);

/// <summary>The claims of an ID token the app reads (not verified: that is the backend's job).</summary>
internal sealed record IdTokenClaims(string? Subject, string? Email, string? Name, string? Picture)
{
    public static IdTokenClaims Read(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError, "The ID token is not a JWT.");

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        try
        {
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = json.RootElement;
            return new IdTokenClaims(Get(root, "sub"), Get(root, "email"), Get(root, "name"), Get(root, "picture"));
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            throw new GoogleSignInException(GoogleSignInExceptionCode.UnknownError, "The ID token is not a JWT.");
        }
    }

    private static string? Get(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
