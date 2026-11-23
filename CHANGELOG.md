# Changelog

## Unreleased

## Provider 6.1.5-preview.1, Bloc 9.1.1-preview.1, Bloc.Core 9.2.1-preview.1, Slang 4.19.0-preview.1 — 2026-11-23

- **Breaking: the bloc core moves to the new `Plumix.Bloc.Core`** (port of `bloc`: Cubit, Bloc, observers; no
  Plumix dependency); `Plumix.Bloc` keeps the widgets (port of `flutter_bloc`) and depends on it, so apps keep their
  reference. Types keep the `Plumix.Bloc` namespace. `IBlocBase` no longer implements Plumix's `IListenable` (it
  keeps `AddListener`/`RemoveListener`).
- **Versions follow the ported pub package**: `Plumix.Provider 6.1.5`, `Plumix.Bloc 9.1.1` (`flutter_bloc`),
  `Plumix.Bloc.Core 9.2.1` (`bloc`), `Plumix.Slang 4.19.0`, each `-preview.N` until full parity. Numbers live in
  `src/Versions.props`; release tags are per package (`provider-v*`, `bloc-core-v*`, `bloc-v*`, `slang-v*`).

## v1.0.0-preview.7 and earlier

- **Packages target .NET 11** (`net11.0`); consumers need the .NET 11 SDK.
- **Raised the minimum Plumix dependency** to the first 1.0 preview (the RC API cleanup, with
  framework `EdgeInsets` in place of Avalonia `Thickness`). The packages port unchanged.
- **Raised the minimum Plumix dependency** to the release with the strict `InheritedWidget`
  port. Previous releases fail with it at runtime (`MissingMethodException` on the
  `InheritedWidget` constructor). Ported to the `InheritedWidget(child, key)` API:
  `Provider<T>`, `RepositoryProvider<T>` and `TranslationScope<T>` use the base `Child`, no
  longer override `Build`, and expose `UpdateShouldNotify` as public; lookups use
  `DependOnInheritedWidgetOfExactType` / `GetInheritedWidgetOfExactType`.
- CI re-tests against the Plumix version declared in the packed nuspecs and runs a non-blocking
  canary against the newest Plumix on NuGet. The pin is overridable via `-p:PlumixVersion=...`.

- New `Plumix.Slang` package: a widget layer over [Slang.Net](https://www.nuget.org/packages/Slang.Net),
  ported from `slang_flutter`. `TranslationProvider<T>` publishes the active translations into the
  widget tree and rebuilds it on every culture change, `Translations<T>.Of(context)` reads them with
  a rebuild subscription, `LocaleSettings<T>` switches locales with slang's fallback rules.
  The generated `[Translations]` class opts in by implementing `ITranslations<TSelf>` (static
  abstract members — no reflection, trimming/AOT safe).

- Build infrastructure: consolidated shared settings in `src/Directory.Build.props`
  (net10.0, nullable, warnings-as-errors, XML docs, analyzers, SourceLink/snupkg,
  deterministic CI builds); csproj files reduced to package identity only.
- Fixed sibling-repo auto-detection path (`UsePlumixLocal`); the local project
  reference switch now actually engages and is disabled on CI.
- CI now runs tests and a packaging smoke check on every push/PR.
- XML documentation added for the entire public API of `Plumix.Bloc` and `Plumix.Provider`.
- New `Plumix.Provider.Tests` project covering provider composition contracts.
- Added `.editorconfig`, `AGENTS.md`, this changelog.

## 0.1.0-alpha.1 — 2026-04-26

- Initial alpha releases of `Plumix.Provider` and `Plumix.Bloc`.
