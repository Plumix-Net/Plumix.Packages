// Dart parity source (reference): pub.dev/packages/sign_in_with_apple 8.2.0,
// darwin/sign_in_with_apple/Sources/sign_in_with_apple/SignInWithAppleAuthorizationController.swift and
// SignInWithAppleAvailablePlugin.swift (approximate)

using AuthenticationServices;
using Foundation;
using Plumix.Plugins;
using UIKit;

namespace Plumix.SignInWithApple;

/// <summary>iOS: <c>ASAuthorizationAppleIDProvider</c> through an <c>ASAuthorizationController</c>.</summary>
internal sealed class IosSignInWithApple : SignInWithApplePlatform
{
    // The controller and its delegate must outlive the call: ASAuthorizationController holds its delegate weakly
    private Authorization? _running;

    public override Task<bool> IsAvailableAsync() => Task.FromResult(OperatingSystem.IsIOSVersionAtLeast(13));

    public override Task<AuthorizationCredentialAppleID> GetAppleIDCredentialAsync(AppleIDAuthorizationRequest request) =>
        UiKit.OnMainThreadAsync(() =>
        {
            var window = UiKit.KeyWindow()
                ?? throw new SignInWithAppleAuthorizationException(AuthorizationErrorCode.NotInteractive,
                    "No window is available to show the sign-in sheet.");

            var appleIdRequest = new ASAuthorizationAppleIdProvider().CreateRequest();
            var scopes = new List<ASAuthorizationScope>();
            if (request.Scopes.Contains(AppleIDAuthorizationScopes.Email))
                scopes.Add(ASAuthorizationScope.Email);
            if (request.Scopes.Contains(AppleIDAuthorizationScopes.FullName))
                scopes.Add(ASAuthorizationScope.FullName);
            appleIdRequest.RequestedScopes = [.. scopes];
            if (request.Nonce is { } nonce)
                appleIdRequest.Nonce = nonce;
            if (request.State is { } state)
                appleIdRequest.State = state;

            // A second call cancels nothing: the newer one wins, as in the Swift plugin
            var authorization = new Authorization(window, () => _running = null);
            _running = authorization;

            var controller = new ASAuthorizationController([appleIdRequest])
            {
                Delegate = authorization,
                PresentationContextProvider = authorization
            };
            authorization.Controller = controller;
            controller.PerformRequests();
            return authorization.Task;
        }).Unwrap();

    public override Task<CredentialState> GetCredentialStateAsync(string userIdentifier)
    {
        var done = new TaskCompletionSource<CredentialState>(TaskCreationOptions.RunContinuationsAsynchronously);

        new ASAuthorizationAppleIdProvider().GetCredentialState(userIdentifier, (state, error) =>
        {
            if (error is not null)
            {
                done.TrySetException(new SignInWithAppleCredentialsException(error.LocalizedDescription));
                return;
            }

            done.TrySetResult(state switch
            {
                ASAuthorizationAppleIdProviderCredentialState.Authorized => CredentialState.Authorized,
                ASAuthorizationAppleIdProviderCredentialState.Revoked => CredentialState.Revoked,
                ASAuthorizationAppleIdProviderCredentialState.NotFound => CredentialState.NotFound,
                _ => throw new SignInWithAppleCredentialsException($"Unknown credential state {state}")
            });
        });

        return done.Task;
    }

    private sealed class Authorization(UIWindow window, Action finished)
        : NSObject, IASAuthorizationControllerDelegate, IASAuthorizationControllerPresentationContextProviding
    {
        private readonly TaskCompletionSource<AuthorizationCredentialAppleID> _done =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ASAuthorizationController? Controller { get; set; }

        public Task<AuthorizationCredentialAppleID> Task => _done.Task;

        public UIWindow GetPresentationAnchor(ASAuthorizationController controller) => window;

        [Export("authorizationController:didCompleteWithAuthorization:")]
        public void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
        {
            finished();

            if (authorization.GetCredential<ASAuthorizationAppleIdCredential>() is not { } credential)
            {
                _done.TrySetException(new SignInWithAppleAuthorizationException(AuthorizationErrorCode.InvalidResponse,
                    $"Unexpected credential {authorization.GetCredential<ASPasswordCredential>()?.GetType().Name ?? "of another type"}"));
                return;
            }

            var code = credential.AuthorizationCode?.ToString(NSStringEncoding.UTF8);
            if (code is null)
            {
                _done.TrySetException(new SignInWithAppleAuthorizationException(AuthorizationErrorCode.InvalidResponse,
                    "parseAuthorizationCredentialAppleID: `authorizationCode` field was `null`"));
                return;
            }

            _done.TrySetResult(new AuthorizationCredentialAppleID(
                credential.User,
                credential.FullName?.GivenName,
                credential.FullName?.FamilyName,
                credential.Email,
                code,
                credential.IdentityToken?.ToString(NSStringEncoding.UTF8),
                credential.State));
        }

        [Export("authorizationController:didCompleteWithError:")]
        public void DidComplete(ASAuthorizationController controller, NSError error)
        {
            finished();

            var code = error.Domain == ASAuthorizationError.Unknown.GetDomain()
                ? (ASAuthorizationError) (long) error.Code switch
                {
                    ASAuthorizationError.Canceled => AuthorizationErrorCode.Canceled,
                    ASAuthorizationError.Failed => AuthorizationErrorCode.Failed,
                    ASAuthorizationError.InvalidResponse => AuthorizationErrorCode.InvalidResponse,
                    ASAuthorizationError.NotHandled => AuthorizationErrorCode.NotHandled,
                    ASAuthorizationError.NotInteractive => AuthorizationErrorCode.NotInteractive,
                    ASAuthorizationError.MatchedExcludedCredential =>
                        AuthorizationErrorCode.MatchedExcludedCredential,
                    ASAuthorizationError.CredentialImport => AuthorizationErrorCode.CredentialImport,
                    ASAuthorizationError.CredentialExport => AuthorizationErrorCode.CredentialExport,
                    _ => AuthorizationErrorCode.Unknown
                }
                : AuthorizationErrorCode.Unknown;

            _done.TrySetException(new SignInWithAppleAuthorizationException(code, error.LocalizedDescription));
        }
    }
}
