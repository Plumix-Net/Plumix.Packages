# Plumix.SignInWithApple

[![NuGet](https://img.shields.io/nuget/v/Plumix.SignInWithApple)](https://www.nuget.org/packages/Plumix.SignInWithApple)

Sign in with Apple for [Plumix](https://github.com/Plumix-Net/Plumix) and other .NET iOS apps — a port of Flutter's
[sign_in_with_apple](https://pub.dev/packages/sign_in_with_apple). Apple's native sheet (`ASAuthorizationController`)
returns the user identifier, the **identity token** (a JWT signed by Apple for your bundle id) and a one-time
authorization code for your backend.

App Store guideline 4.8: an app that offers a third-party sign-in (Google…) must offer Sign in with Apple too.

## Installation

```bash
dotnet add package Plumix.SignInWithApple
```

## Setup

- App ID: enable the **Sign in with Apple** capability (Apple Developer → Identifiers).
- The app's `Entitlements.plist`:

```xml
<key>com.apple.developer.applesignin</key>
<array>
    <string>Default</string>
</array>
```

The entitlement needs a provisioning profile that carries it: an unsigned simulator build fails with
`AuthorizationErrorCode.Unknown`.

## Usage

```csharp
using Plumix.SignInWithApple;

if (await SignInWithApple.IsAvailableAsync())
{
    var credential = await SignInWithApple.GetAppleIDCredentialAsync(
        [AppleIDAuthorizationScopes.Email, AppleIDAuthorizationScopes.FullName]);
    await backend.SignInWithAppleAsync(credential.IdentityToken!, credential.GivenName, credential.FamilyName);
}
```

Name and email come only on the app's first authorization; keep them then.

## API

| Plumix.SignInWithApple | sign_in_with_apple |
|---|---|
| `SignInWithApple.IsAvailableAsync()` | `isAvailable()` |
| `SignInWithApple.GetAppleIDCredentialAsync(scopes, webAuthenticationOptions, nonce, state)` | `getAppleIDCredential(...)` |
| `SignInWithApple.GetCredentialStateAsync(userIdentifier)` | `getCredentialState(...)` |
| `AuthorizationCredentialAppleID`, `AppleIDAuthorizationScopes`, `CredentialState` | same |
| `SignInWithAppleAuthorizationException` (+ `AuthorizationErrorCode`), `SignInWithAppleNotSupportedException`, `SignInWithAppleCredentialsException` | same |

Not ported: the web flow for Android and other platforms (`webAuthenticationOptions` — there `IsAvailableAsync` is
false), `getKeychainCredential`.
