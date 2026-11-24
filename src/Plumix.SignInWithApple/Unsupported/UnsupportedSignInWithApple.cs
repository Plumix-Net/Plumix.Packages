namespace Plumix.SignInWithApple;

/// <summary>Everywhere but iOS: not available.</summary>
internal sealed class UnsupportedSignInWithApple : SignInWithApplePlatform
{
    public override Task<bool> IsAvailableAsync() => Task.FromResult(false);

    public override Task<AuthorizationCredentialAppleID> GetAppleIDCredentialAsync(AppleIDAuthorizationRequest request) =>
        throw new SignInWithAppleNotSupportedException("Sign in with Apple is available on iOS only in this port.");

    public override Task<CredentialState> GetCredentialStateAsync(string userIdentifier) =>
        throw new SignInWithAppleNotSupportedException("Sign in with Apple is available on iOS only in this port.");
}
