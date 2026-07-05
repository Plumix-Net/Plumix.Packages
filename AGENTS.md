# AGENTS.md

This file defines expectations for coding agents working in this repository.

## Project Snapshot

- Platform: .NET 10
- Purpose: ports of popular Flutter packages for [Plumix](https://github.com/Plumix-Net/Plumix), a Flutter-like UI framework for .NET
- Main solution: `src/Plumix.Packages.slnx`
- Main framework repo (sibling): `../Plumix` — see its `AGENTS.md` for framework-level rules

## Repository Map

| Path | Contents | Flutter/Dart original |
|------|----------|-----------------------|
| `src/Plumix.Provider` | `Provider`, `ChangeNotifierProvider`, `Consumer`, `MultiProvider`, `context.Watch/Read` | [provider](https://pub.dev/packages/provider) |
| `src/Plumix.Bloc` | `Cubit`, `Bloc`, `BlocProvider`, `BlocBuilder`, `BlocListener`, `BlocConsumer`, `BlocSelector` | [bloc](https://pub.dev/packages/bloc) + [flutter_bloc](https://pub.dev/packages/flutter_bloc) |
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
4. Shared build settings live in `src/Directory.Build.props`; package versions in
   `src/Directory.Packages.props` (central package management). A new package project
   needs only `IsPackable`, `PackageId`, `Description` in its csproj plus a `README.md`
   in the project directory.
5. Non-trivial logic needs test coverage in the matching `*.Tests` project.
   Note: widget-tree tests (mount/pump) are not possible from this repo yet —
   `Element.Mount/Attach/Rebuild` are internal to Plumix. Test the publicly observable
   contracts; framework-level widget behavior is tested in the Plumix repo.

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
```

## Releases

- Push a `v*.*.*` tag; CI packs with that version and publishes both packages
  (plus `.snupkg` symbols) to NuGet.org. All packages share one version line.
- Record shipped changes in `CHANGELOG.md` (short entries, a few lines per change).
- Do not hardcode `Version` in csproj files; local packs are versioned `0.1.0-dev`
  via `VersionPrefix`/`VersionSuffix` in `src/Directory.Build.props`.
