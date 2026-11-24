using Xunit;
namespace Plumix.SignInWithApple.Tests;

public class SignInWithAppleTests
{
    [Fact]
    public async Task OnPlainDotNet_ItIsNotAvailable()
    {
        Assert.False(await SignInWithApple.IsAvailableAsync());
        await Assert.ThrowsAsync<SignInWithAppleNotSupportedException>(() =>
            SignInWithApple.GetAppleIDCredentialAsync([AppleIDAuthorizationScopes.Email]));
    }

    [Fact]
    public void Credential_DoesNotPrintTheToken()
    {
        var credential = new AuthorizationCredentialAppleID("001.abc", "Ann", null, "a@b.c", "code", "secret.jwt", null);
        Assert.DoesNotContain("secret", credential.ToString());
        Assert.Contains("[identityToken set: True]", credential.ToString());
    }

    [Fact]
    public void AuthorizationException_KeepsTheCode()
    {
        var e = new SignInWithAppleAuthorizationException(AuthorizationErrorCode.Canceled, "The user canceled.");
        Assert.Equal(AuthorizationErrorCode.Canceled, e.Code);
        Assert.Equal("The user canceled.", e.Reason);
    }
}
