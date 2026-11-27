#!/usr/bin/env bash
set -euo pipefail

output="${GITHUB_OUTPUT:-/dev/stdout}"
version_file="src/DS.Domain/DSVersion.cs"

if [[ -n "${REQUESTED_VERSION:-}" ]]; then
    tag="v${REQUESTED_VERSION#v}"
    if [[ ! "$tag" =~ ^v[0-9]+\.[0-9]+\.[0-9]+(\+[0-9]+)?$ ]]; then
        echo "Ugyldig version: '$REQUESTED_VERSION'. Brug fx v0.3.1 eller v0.3.1+2." >&2
        exit 1
    fi
    if ! git rev-parse -q --verify "refs/tags/$tag" > /dev/null; then
        echo "Tagget $tag findes ikke." >&2
        exit 1
    fi
    build=false
else
    base=$(sed -nE 's/.*const string Version = "([0-9]+\.[0-9]+\.[0-9]+)".*/\1/p' "$version_file")
    if [[ -z "$base" ]]; then
        echo "Kunne ikke læse versionen i $version_file." >&2
        exit 1
    fi

    tag=$(git tag --points-at HEAD | grep -E "^v${base//./\\.}(\+[0-9]+)?$" | head -n 1 || true)
    if [[ -z "$tag" ]]; then
        if git rev-parse -q --verify "refs/tags/v$base" > /dev/null; then
            last=$(git tag --list "v$base+*" | sed -E 's/^.*\+//' | grep -E '^[0-9]+$' | sort -n | tail -n 1 || true)
            tag="v$base+$(( ${last:-0} + 1 ))"
        else
            tag="v$base"
        fi
        git tag "$tag" HEAD
        git push origin "refs/tags/$tag"
    fi
    build=true
fi

version="${tag#v}"
{
    echo "tag=$tag"
    echo "version=$version"
    echo "image_tag=${version/+/_}"
    echo "build=$build"
} >> "$output"
