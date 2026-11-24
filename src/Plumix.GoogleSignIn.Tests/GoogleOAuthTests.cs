using Xunit;
using System.Text;

namespace Plumix.GoogleSignIn.Tests;

public class GoogleOAuthTests
{
    private const string ClientId = "123-abc.apps.googleusercontent.com";

    [Fact]
    public void RedirectScheme_ReversesTheClientId()
    {
        Assert.Equal("com.googleusercontent.apps.123-abc", GoogleOAuth.RedirectScheme(ClientId));
        Assert.Equal("com.googleusercontent.apps.123-abc:/oauth2redirect", GoogleOAuth.RedirectUri(ClientId));
    }

    [Fact]
    public void RedirectScheme_RejectsWhatIsNotAClientId()
    {
        var e = Assert.Throws<GoogleSignInException>(() => GoogleOAuth.RedirectScheme("my-app"));
        Assert.Equal(GoogleSignInExceptionCode.ClientConfigurationError, e.Code);
    }

    [Fact]
    public void Challenge_IsTheRfc7636Example()
    {
        // RFC 7636 appendix B
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            GoogleOAuth.Challenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Fact]
    public void Verifier_IsBase64UrlOf32Bytes()
    {
        var verifier = GoogleOAuth.CreateVerifier();
        Assert.Equal(43, verifier.Length);
        Assert.DoesNotContain('=', verifier);
        Assert.DoesNotContain('+', verifier);
        Assert.DoesNotContain('/', verifier);
    }

    [Fact]
    public void AuthorizationUrl_CarriesPkceStateScopesAndOptions()
    {
        var url = GoogleOAuth.AuthorizationUrl(ClientId, "challenge", "state-1", ["https://www.googleapis.com/auth/drive", "email"],
            nonce: "n-1", hostedDomain: "example.com", serverClientId: "web.apps.googleusercontent.com");
        var query = GoogleOAuth.ParseQuery(url.Query);

        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", url.AbsoluteUri);
        Assert.Equal(ClientId, query["client_id"]);
        Assert.Equal("com.googleusercontent.apps.123-abc:/oauth2redirect", query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("openid email profile https://www.googleapis.com/auth/drive", query["scope"]);
        Assert.Equal("challenge", query["code_challenge"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal("state-1", query["state"]);
        Assert.Equal("n-1", query["nonce"]);
        Assert.Equal("example.com", query["hd"]);
        Assert.Equal("web.apps.googleusercontent.com", query["audience"]);
    }

    [Fact]
    public void ReadRedirect_ReturnsTheCodeForOurState()
    {
        var code = GoogleOAuth.ReadRedirect(
            new Uri("com.googleusercontent.apps.123-abc:/oauth2redirect?state=s&code=4%2F0Ab&scope=email"), "s");
        Assert.Equal("4/0Ab", code);
    }

    [Theory]
    [InlineData("com.googleusercontent.apps.x:/oauth2redirect?state=other&code=c", GoogleSignInExceptionCode.UnknownError)]
    [InlineData("com.googleusercontent.apps.x:/oauth2redirect?error=access_denied&state=s", GoogleSignInExceptionCode.Canceled)]
    [InlineData("com.googleusercontent.apps.x:/oauth2redirect?state=s", GoogleSignInExceptionCode.UnknownError)]
    public void ReadRedirect_RefusesErrorsForeignStatesAndMissingCodes(string redirect, GoogleSignInExceptionCode code)
    {
        var e = Assert.Throws<GoogleSignInException>(() => GoogleOAuth.ReadRedirect(new Uri(redirect), "s"));
        Assert.Equal(code, e.Code);
    }

    [Fact]
    public void ReadTokenResponse_ReadsTokens()
    {
        var tokens = GoogleOAuth.ReadTokenResponse(200,
            """{"access_token":"at","expires_in":3599,"refresh_token":"rt","id_token":"a.b.c","token_type":"Bearer"}""");
        Assert.Equal(new TokenResponse("a.b.c", "at", "rt"), tokens);
    }

    [Fact]
    public void ReadTokenResponse_MapsClientErrors()
    {
        var e = Assert.Throws<GoogleSignInException>(() =>
            GoogleOAuth.ReadTokenResponse(401, """{"error":"invalid_client"}"""));
        Assert.Equal(GoogleSignInExceptionCode.ClientConfigurationError, e.Code);

        e = Assert.Throws<GoogleSignInException>(() => GoogleOAuth.ReadTokenResponse(200, """{"access_token":"at"}"""));
        Assert.Equal(GoogleSignInExceptionCode.UnknownError, e.Code);
    }

    [Fact]
    public void UserFromIdToken_ReadsTheClaims()
    {
        var token = Jwt("""{"sub":"1077","email":"ann@example.com","name":"Ann","picture":"https://p/1.png"}""");
        Assert.Equal(new GoogleSignInUserData("ann@example.com", "1077", "Ann", "https://p/1.png"),
            GoogleOAuth.UserFromIdToken(token));
    }

    [Fact]
    public void UserFromIdToken_NeedsSubjectAndEmail()
    {
        Assert.Throws<GoogleSignInException>(() => GoogleOAuth.UserFromIdToken(Jwt("""{"sub":"1"}""")));
        Assert.Throws<GoogleSignInException>(() => GoogleOAuth.UserFromIdToken("not-a-jwt"));
    }

    internal static string Jwt(string payload) =>
        "eyJhbGciOiJSUzI1NiJ9." + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=')
            .Replace('+', '-').Replace('/', '_') + ".sig";
}
