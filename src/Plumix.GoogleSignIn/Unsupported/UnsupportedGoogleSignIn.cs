namespace Plumix.GoogleSignIn;

/// <summary>
/// The net11.0 build (desktop, tests): no platform sign-in. Lightweight authentication finds nobody; authentication
/// throws <see cref="GoogleSignInExceptionCode.ProviderConfigurationError"/> — as a Flutter app without the plugin's
/// platform implementation fails.
/// </summary>
internal sealed class UnsupportedGoogleSignIn : GoogleSignInPlatform
{
    public override Task InitAsync(InitParameters parameters) => Task.CompletedTask;

    public override Task<AuthenticationResults?> AttemptLightweightAuthenticationAsync(
        AttemptLightweightAuthenticationParameters parameters) => Task.FromResult<AuthenticationResults?>(null);

    public override bool SupportsAuthenticate() => false;

    public override Task<AuthenticationResults> AuthenticateAsync(AuthenticateParameters parameters) =>
        throw new GoogleSignInException(GoogleSignInExceptionCode.ProviderConfigurationError,
            "Google sign-in has no implementation on this platform (Android and iOS only).");

    public override Task SignOutAsync() => Task.CompletedTask;

    public override Task DisconnectAsync() => Task.CompletedTask;
}
