# Changelog

## Unreleased

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
