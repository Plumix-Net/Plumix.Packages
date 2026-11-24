using Xunit;
namespace Plumix.GoogleSignIn.Tests;

[Collection("platform")]
public sealed class GoogleSignInTests : IDisposable
{
    private readonly GoogleSignInPlatform _original = GoogleSignInPlatform.Instance;
    private readonly FakePlatform _fake = new();

    public GoogleSignInTests() => GoogleSignInPlatform.Instance = _fake;

    public void Dispose() => GoogleSignInPlatform.Instance = _original;

    [Fact]
    public async Task Authenticate_ReturnsTheAccountAndRaisesSignIn()
    {
        GoogleSignInAuthenticationEvent? seen = null;
        GoogleSignIn.Instance.AuthenticationEvent += Handler;
        try
        {
            await GoogleSignIn.Instance.InitializeAsync(clientId: "c", serverClientId: "s");
            var account = await GoogleSignIn.Instance.AuthenticateAsync();

            Assert.Equal(new InitParameters("c", "s", null, null), _fake.Init);
            Assert.Equal("ann@example.com", account.Email);
            Assert.Equal("1077", account.Id);
            Assert.Equal("id-token", account.Authentication.IdToken);
            Assert.Equal(new GoogleSignInAuthenticationEventSignIn(account), seen);
        }
        finally
        {
            GoogleSignIn.Instance.AuthenticationEvent -= Handler;
        }

        void Handler(object? sender, GoogleSignInAuthenticationEvent e) => seen = e;
    }

    [Theory]
    [InlineData(GoogleSignInExceptionCode.Canceled, false)]
    [InlineData(GoogleSignInExceptionCode.Interrupted, false)]
    [InlineData(GoogleSignInExceptionCode.UiUnavailable, false)]
    [InlineData(GoogleSignInExceptionCode.ClientConfigurationError, true)]
    public async Task Lightweight_SwallowsOnlyInteractionFailures(GoogleSignInExceptionCode code, bool throws)
    {
        _fake.Failure = new GoogleSignInException(code);

        if (throws)
            await Assert.ThrowsAsync<GoogleSignInException>(() =>
                GoogleSignIn.Instance.AttemptLightweightAuthenticationAsync());
        else
            Assert.Null(await GoogleSignIn.Instance.AttemptLightweightAuthenticationAsync());

        await Assert.ThrowsAsync<GoogleSignInException>(() =>
            GoogleSignIn.Instance.AttemptLightweightAuthenticationAsync(reportAllExceptions: true));
    }

    [Fact]
    public async Task Authenticate_RaisesTheErrorAndRethrows()
    {
        _fake.Failure = new GoogleSignInException(GoogleSignInExceptionCode.Canceled);
        GoogleSignInException? seen = null;
        EventHandler<GoogleSignInException> handler = (_, e) => seen = e;
        GoogleSignIn.Instance.AuthenticationError += handler;
        try
        {
            var e = await Assert.ThrowsAsync<GoogleSignInException>(() => GoogleSignIn.Instance.AuthenticateAsync());
            Assert.Same(_fake.Failure, e);
            Assert.Same(_fake.Failure, seen);
        }
        finally
        {
            GoogleSignIn.Instance.AuthenticationError -= handler;
        }
    }

    [Fact]
    public async Task SignOut_RaisesSignOut()
    {
        GoogleSignInAuthenticationEvent? seen = null;
        EventHandler<GoogleSignInAuthenticationEvent> handler = (_, e) => seen = e;
        GoogleSignIn.Instance.AuthenticationEvent += handler;
        try
        {
            await GoogleSignIn.Instance.SignOutAsync();
            Assert.IsType<GoogleSignInAuthenticationEventSignOut>(seen);
            Assert.True(_fake.SignedOut);
        }
        finally
        {
            GoogleSignIn.Instance.AuthenticationEvent -= handler;
        }
    }

    private sealed class FakePlatform : GoogleSignInPlatform
    {
        private static readonly AuthenticationResults Ann = new(
            new GoogleSignInUserData("ann@example.com", "1077", "Ann"), new AuthenticationTokenData("id-token"));

        public InitParameters? Init { get; private set; }

        public GoogleSignInException? Failure { get; set; }

        public bool SignedOut { get; private set; }

        public override Task InitAsync(InitParameters parameters)
        {
            Init = parameters;
            return Task.CompletedTask;
        }

        public override Task<AuthenticationResults?> AttemptLightweightAuthenticationAsync(
            AttemptLightweightAuthenticationParameters parameters) =>
            Failure is { } failure ? throw failure : Task.FromResult<AuthenticationResults?>(Ann);

        public override bool SupportsAuthenticate() => true;

        public override Task<AuthenticationResults> AuthenticateAsync(AuthenticateParameters parameters) =>
            Failure is { } failure ? throw failure : Task.FromResult(Ann);

        public override Task SignOutAsync()
        {
            SignedOut = true;
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync() => SignOutAsync();
    }
}

[Collection("platform")]
public class UnsupportedPlatformTests
{
    [Fact]
    public async Task OnPlainDotNet_AuthenticateIsNotSupported()
    {
        Assert.False(GoogleSignIn.Instance.SupportsAuthenticate());
        var e = await Assert.ThrowsAsync<GoogleSignInException>(() => GoogleSignIn.Instance.AuthenticateAsync());
        Assert.Equal(GoogleSignInExceptionCode.ProviderConfigurationError, e.Code);
        Assert.Null(await GoogleSignIn.Instance.AttemptLightweightAuthenticationAsync());
    }
}
