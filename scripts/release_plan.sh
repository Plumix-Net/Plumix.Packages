#!/usr/bin/env bash
# Check a release tag against src/Versions.props and list the packages a release pushes
# (policy: Plumix repo docs/VERSIONING.md). Prints NAME=value lines for $GITHUB_ENV.
#
# Every package is its own line, versioned after the pub.dev package it ports:
#   provider      provider-v<PlumixProviderVersion>          Plumix.Provider
#   bloc-core     bloc-core-v<PlumixBlocCoreVersion>         Plumix.Bloc.Core (port of bloc)
#   bloc          bloc-v<PlumixBlocVersion>                  Plumix.Bloc (port of flutter_bloc, depends on Core)
#   slang         slang-v<PlumixSlangVersion>                Plumix.Slang
#   google-sign-in      google-sign-in-v<PlumixGoogleSignInVersion>          Plumix.GoogleSignIn
#   sign-in-with-apple  sign-in-with-apple-v<PlumixSignInWithAppleVersion>   Plumix.SignInWithApple
#   in-app-purchase     in-app-purchase-v<PlumixInAppPurchaseVersion>        Plumix.InAppPurchase
#
# A release pushes every package at its Versions.props version with --skip-duplicate, so packages
# already on nuget.org are skipped and a dependency released in the same commit is pushed with its
# dependent. Every package whose version is not on nuget.org yet must carry its own tag on origin, so
# no version ships untagged — push all release tags of a commit in one `git push origin <tag>...`.
# GitHub creates no events for a push of more than three tags: then re-push one tag on its own
# (Plumix repo docs/VERSIONING.md, Releasing).
#
# Usage:
#   scripts/release_plan.sh bloc-v9.2.1-preview.1 >> "$GITHUB_ENV"

set -euo pipefail

tag="${1:?usage: release_plan.sh <tag>}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
versions="$root/src/Versions.props"

read_version() {
  sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" "$versions"
}

# line | tag prefix | version | packages (the first one probes nuget.org for the line)
lines=(
  "provider|provider-v|$(read_version PlumixProviderVersion)|Plumix.Provider"
  "bloc-core|bloc-core-v|$(read_version PlumixBlocCoreVersion)|Plumix.Bloc.Core"
  "bloc|bloc-v|$(read_version PlumixBlocVersion)|Plumix.Bloc"
  "slang|slang-v|$(read_version PlumixSlangVersion)|Plumix.Slang"
  "google-sign-in|google-sign-in-v|$(read_version PlumixGoogleSignInVersion)|Plumix.GoogleSignIn"
  "sign-in-with-apple|sign-in-with-apple-v|$(read_version PlumixSignInWithAppleVersion)|Plumix.SignInWithApple"
  "in-app-purchase|in-app-purchase-v|$(read_version PlumixInAppPurchaseVersion)|Plumix.InAppPurchase"
)

version_pattern='^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?$'

tag_line=""
for entry in "${lines[@]}"; do
  IFS='|' read -r line prefix version _ <<< "$entry"
  if ! [[ "$version" =~ $version_pattern ]]; then
    echo "::error::src/Versions.props: the $line line has no valid version ('$version')" >&2
    exit 1
  fi
  if [[ "$tag" == "$prefix"* && "${tag#"$prefix"}" =~ ^[0-9] ]]; then
    tag_line="$line"
    if [[ "${tag#"$prefix"}" != "$version" ]]; then
      echo "::error::Tag '$tag' does not match the $line line in src/Versions.props ($version)." \
        "Bump Versions.props in the tagged commit." >&2
      exit 1
    fi
  fi
done
if [[ -z "$tag_line" ]]; then
  echo "::error::Unknown release tag '$tag' (expected provider-v*, bloc-core-v*, bloc-v*, slang-v*, google-sign-in-v*, sign-in-with-apple-v* or in-app-purchase-v*)" >&2
  exit 1
fi

packages=()
for entry in "${lines[@]}"; do
  IFS='|' read -r line prefix version ids <<< "$entry"
  probe="${ids%% *}"
  index="https://api.nuget.org/v3-flatcontainer/$(tr '[:upper:]' '[:lower:]' <<< "$probe")/index.json"
  published="$(curl -fsSL "$index" 2>/dev/null || echo '{"versions":[]}')"
  lower_version="$(tr '[:upper:]' '[:lower:]' <<< "$version")"
  if ! grep -q "\"$lower_version\"" <<< "$published"; then
    if ! git -C "$root" ls-remote --exit-code --tags origin "refs/tags/$prefix$version" > /dev/null; then
      echo "::error::The $line line ($version) is not on nuget.org and has no tag $prefix$version on origin." \
        "Tag it on this commit and push the release tags together." >&2
      exit 1
    fi
    echo "::notice::Releasing the $line line: $version" >&2
  fi
  for id in $ids; do
    packages+=("$id.$version")
  done
done

echo "RELEASE_PACKAGES=${packages[*]}"
