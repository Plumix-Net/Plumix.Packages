# AGENTS.md

This file defines expectations for coding agents working in this repository.

## Project Snapshot

- Platform: .NET 11
- Purpose: ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix), a Flutter-like UI framework for .NET
- Main solution: `src/Plumix.Packages.slnx`
- Main framework repo (sibling): `../Plumix` — see its `AGENTS.md` for framework-level rules

## Repository Map

| Path | Contents | Flutter/Dart original (pin) |
|------|----------|-----------------------------|
| `src/Plumix.Provider` | `Provider`, `ChangeNotifierProvider`, `Consumer`, `MultiProvider`, `context.Watch/Read` | [provider](https://pub.dev/packages/provider) 6.1.5 |
| `src/Plumix.Bloc.Core` | `Cubit`, `Bloc`, `Change`, `Transition`, `EventTransformers`, `IBlocObserver` — no Plumix dependency (namespace `Plumix.Bloc`) | [bloc](https://pub.dev/packages/bloc) 9.2.1 |
| `src/Plumix.Bloc` | `BlocProvider`, `BlocBuilder`, `BlocListener`, `BlocConsumer`, `BlocSelector`, `RepositoryProvider`, `context.Read/Watch` — depends on `Plumix.Bloc.Core` | [flutter_bloc](https://pub.dev/packages/flutter_bloc) 9.1.1 |
| `src/Plumix.Slang` | `ITranslations`, `TranslationProvider`, `Translations.Of(context)`, `LocaleSettings` — widget layer over [Slang.Net](https://www.nuget.org/packages/Slang.Net) | [slang_flutter](https://pub.dev/packages/slang_flutter) 4.19.0 |
| `src/Plumix.GoogleSignIn` | `GoogleSignIn.Instance` (initialize, authenticate, lightweight, sign-out), `GoogleSignInAccount`, `GoogleSignInPlatform` — Credential Manager on Android, OAuth + PKCE in `ASWebAuthenticationSession` on iOS | [google_sign_in](https://pub.dev/packages/google_sign_in) 7.2.0 |
| `src/Plumix.SignInWithApple` | `SignInWithApple.GetAppleIDCredentialAsync`, credential state — `ASAuthorizationController`, iOS only | [sign_in_with_apple](https://pub.dev/packages/sign_in_with_apple) 8.2.0 |
| `src/Plumix.InAppPurchase` | `InAppPurchase.Instance` (products, buy, purchase stream, complete, restore) — StoreKit 1 on iOS, Play Billing 8 on Android | [in_app_purchase](https://pub.dev/packages/in_app_purchase) 3.3.1 |
| `src/*.Tests` | xunit test projects, one per package | — |

## Change Guidelines

1. API shape and behavior follow the Dart original (see the parity header comment at the
   top of each source file). Deviations must be deliberate and documented in the XML docs
   and `CHANGELOG.md`.
2. Every public and protected member requires XML documentation
   (`GenerateDocumentationFile` + `TreatWarningsAsErrors` are on; CS1591 fails the build).
3. Analyzer severities live in `.editorconfig`. CA1000/CA1711/CA1716 are intentionally
   disabled — they conflict with the Flutter-parity API surface (static `Of(context)`
   accessors, `event`/`error` parameter names). Do not re-enable or "fix" these.
4. Shared build settings live in `src/Directory.Build.props`; dependency versions in
   `src/Directory.Packages.props` (central package management); our own package versions in
   `src/Versions.props`. A new package project needs `IsPackable`, `PackageId`, `Description`,
   `<Version>$(Plumix<Name>Version)</Version>` (a new line in `Versions.props`, the version of
   the pub package it ports) in its csproj plus a `README.md` in the project directory. A port
   of a pure-Dart package (no Flutter dependency, like `bloc`) sets `UsesPlumix=false`.
5. Non-trivial logic needs test coverage in the matching `*.Tests` project.
   Note: widget-tree tests (mount/pump) are not possible from this repo yet —
   `Element.Mount/Attach/Rebuild` are internal to Plumix. Test the publicly observable
   contracts; framework-level widget behavior is tested in the Plumix repo.

## Platform Plugins

`Plumix.GoogleSignIn`, `Plumix.SignInWithApple` and `Plumix.InAppPurchase` port Flutter *plugins*: the work is the
OS's, so they set `UsesPlumix=false` and import `src/PlatformPlugin.props`:

- **Targets**: `net11.0` (the public API with an `Unsupported*` platform implementation) plus `net11.0-android` and
  `net11.0-ios` when `PlumixMobileTargets` is true — the default on macOS (the iOS workload builds only there), false
  elsewhere; CI builds and tests on Linux with `net11.0` only and the release job packs on macOS
  (`-p:PlumixMobileTargets=true`, Xcode 27.2, `android` + `ios` workloads). A plain `net11.0` app project compiles
  against the API; NuGet hands the heads the platform build (bait and switch) — the public surface must be identical
  in every build.
- **Folders**: `Android/**` compiles only for Android, `iOS/**` only for iOS, `Unsupported/**` only for `net11.0`
  (a package may include it for a platform it does not support, as SignInWithApple does on Android). Platform-free
  logic (OAuth URLs, JWT claims, ISO periods) stays outside those folders so `net11.0` tests cover it.
- **Shared**: `src/Shared/Android/ActivityTracker.cs` — the activity a plugin shows UI over. Plumix has no plugin
  registry (Flutter's `ActivityAware`), so each plugin declares a `ContentProvider` (`[ContentProvider]` with authority
  `${applicationId}.plumix-<plugin>`) that registers activity lifecycle callbacks at process start.
  `src/Shared/iOS/UiKit.cs` — the key window and the main thread. Both are linked into each plugin as internal types.
- **Platform interface**: each plugin has a public `*Platform` class with a settable `Instance`, as Dart's
  `*_platform_interface` packages, so apps fake it in tests.
- Analyzer and platform warnings are errors here too: deprecated platform API that is still the only way (StoreKit 1,
  `ASWebAuthenticationSession`'s pre-17.4 constructor) is wrapped with a version check or a scoped `#pragma`.

## Local Plumix Reference

`src/Directory.Build.props` auto-switches the `Plumix` dependency to a project reference
when the sibling repo exists at `../Plumix`. Override with `-p:UsePlumixLocal=false`.
On CI (`CI=true`) the switch is always off so published packages depend on the pinned
NuGet version.

## Validation Checklist

```bash
dotnet build src/Plumix.Packages.slnx -c Release   # zero warnings tolerated
dotnet test  src/Plumix.Packages.slnx -c Release
dotnet pack  src/Plumix.Packages.slnx -c Release   # packaging smoke check
# On macOS the platform plugins also build for net11.0-android / net11.0-ios (android + ios workloads);
# -p:PlumixMobileTargets=false skips them.
```

## Releases

- Versions follow the pub package each port targets (policy: `docs/VERSIONING.md` in the Plumix
  repo): `X.Y.Z-preview.N` until full parity with upstream `X.Y.Z`, then `X.Y.Z`, then `X.Y.Z.N`
  for our fixes. The numbers live only in `src/Versions.props`; the pin moves only after parity.
- Release: bump the package(s) in `src/Versions.props`, tag the commit once per bumped package
  (`provider-v*`, `bloc-core-v*`, `bloc-v*`, `slang-v*`, `google-sign-in-v*`, `sign-in-with-apple-v*`,
  `in-app-purchase-v*`) and push the tags together (with
  more than three tags GitHub starts no run: re-push one tag alone, it releases them all). CI
  (`scripts/release_plan.sh`) checks each tag against `Versions.props`, refuses an unpublished
  version without its tag, and pushes every package (plus `.snupkg` symbols) with
  `--skip-duplicate`.
- Record shipped changes in `CHANGELOG.md` (short entries, a few lines per change).
- `PlumixVersion` in `src/Directory.Packages.props` is the minimum Plumix version every
  nuspec declares (`>=`). When a Plumix release breaks binary compatibility, raise it to that
  release in the same change that ports the code. Do not repeat release numbers
  (`-preview.N`) in READMEs or the changelog — `Versions.props` sets the package version, the
  nuspec carries the Plumix floor. Upstream pins (`provider 6.1.5`) may be named; update them
  when a pin moves.
  Never ship a dependency floor the packages were not tested against — CI re-tests against
  the version read from the packed nuspecs.
- Do not hardcode a version number in a csproj (only `$(Plumix<Name>Version)`), and never
  pass `-p:Version`: it would restamp the `Plumix.Bloc` → `Plumix.Bloc.Core` floor.
