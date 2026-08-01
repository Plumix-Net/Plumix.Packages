# Changelog

## Unreleased

- New `Plumix.Slang` package: a widget layer over [Slang.Net](https://www.nuget.org/packages/Slang.Net),
  ported from `slang_flutter`. `TranslationProvider<T>` publishes the active translations into the
  widget tree and rebuilds it on every culture change, `Translations<T>.Of(context)` reads them with
  a rebuild subscription, `LocaleSettings<T>` switches locales with slang's fallback rules.
  The generated `[Translations]` class opts in by implementing `ITranslations<TSelf>` (static
  abstract members — no reflection, trimming/AOT safe).
- Pinned `Plumix` NuGet dependency moved from `0.1.0-alpha.3` to `0.1.0-alpha.13`; the old pin no
  longer builds (`State.Mounted` is newer), so CI builds against the NuGet package were broken.

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
