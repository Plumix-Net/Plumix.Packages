# Plumix.GoogleSignIn

[![NuGet](https://img.shields.io/nuget/v/Plumix.GoogleSignIn)](https://www.nuget.org/packages/Plumix.GoogleSignIn)

Sign in with Google for [Plumix](https://github.com/Plumix-Net/Plumix) and other .NET Android / iOS apps — a port of
Flutter's [google_sign_in](https://pub.dev/packages/google_sign_in) (7.x API). It gives you the user and a Google
**ID token** for your backend to verify.

| Platform | How |
|---|---|
| Android | Credential Manager — the "Sign in with Google" sheet (`GetSignInWithGoogleOption`) |
| iOS | Google's OAuth 2.0 with PKCE in `ASWebAuthenticationSession`, as the GoogleSignIn SDK does; the session is kept in the Keychain |
| net11.0 (desktop, tests) | not supported: `AuthenticateAsync` throws `ProviderConfigurationError` |

## Installation

```bash
dotnet add package Plumix.GoogleSignIn
```

Reference it from your shared UI project (plain `net11.0`) or the heads: the Android and iOS heads get the platform
build automatically.

## Setup

In the Google Cloud console of your project create OAuth clients:

- **Web** — its id is the `serverClientId`; on Android it is the ID token's audience.
- **Android** — package name and the SHA-1 of every signing key (debug, upload, Play App Signing). Nothing goes into
  the app; Credential Manager matches the app by package and signature.
- **iOS** — bundle id. Its id is the `clientId` on iOS; Google redirects to the reversed id
  (`com.googleusercontent.apps.…:/oauth2redirect`), which `ASWebAuthenticationSession` catches — no Info.plist entry.

Your backend accepts the ID token when its `aud` is one of your client ids (web, iOS).

## Usage

```csharp
using Plumix.GoogleSignIn;

await GoogleSignIn.Instance.InitializeAsync(
    clientId: OperatingSystem.IsIOS() ? "<ios-client-id>.apps.googleusercontent.com" : null,
    serverClientId: OperatingSystem.IsAndroid() ? "<web-client-id>.apps.googleusercontent.com" : null);

try
{
    var account = await GoogleSignIn.Instance.AuthenticateAsync();
    await backend.SignInWithGoogleAsync(account.Authentication.IdToken!);
}
catch (GoogleSignInException e) when (e.Code == GoogleSignInExceptionCode.Canceled)
{
    // the user closed the sheet
}
```

`AttemptLightweightAuthenticationAsync()` signs a returning user in with minimal UI (null when there is nobody);
`SignOutAsync()` / `DisconnectAsync()` sign out.

## API

| Plumix.GoogleSignIn | google_sign_in |
|---|---|
| `GoogleSignIn.Instance` | `GoogleSignIn.instance` |
| `InitializeAsync(clientId, serverClientId, nonce, hostedDomain)` | `initialize(...)` |
| `AuthenticateAsync(scopeHint)` | `authenticate(scopeHint:)` |
| `AttemptLightweightAuthenticationAsync(reportAllExceptions)` | `attemptLightweightAuthentication(...)` |
| `SupportsAuthenticate()` | `supportsAuthenticate()` |
| `SignOutAsync()`, `DisconnectAsync()` | `signOut()`, `disconnect()` |
| `AuthenticationEvent`, `AuthenticationError` events | `authenticationEvents` stream |
| `GoogleSignInAccount`, `GoogleSignInAuthentication` | same |
| `GoogleSignInException`, `GoogleSignInExceptionCode` | same |
| `GoogleSignInPlatform.Instance` (replace in tests) | `GoogleSignInPlatform.instance` |

Not ported yet: the authorization client (`authorizationClient` — access tokens and server auth codes for scopes).
