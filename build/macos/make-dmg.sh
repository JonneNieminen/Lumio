#!/usr/bin/env bash
# Builds a drag-to-install DMG from Velopack's portable macOS zip.
# The DMG holds Lumio.app, an Applications shortcut and "READ ME FIRST.txt" with the
# xattr command to run an unnotarized build.
#
# Usage: build/macos/make-dmg.sh <Portable.zip> <version> <rid> <output-dir>
set -euo pipefail

zip="$1"; version="$2"; rid="$3"; out="$4"
here="$(cd "$(dirname "$0")" && pwd)"
case "$rid" in
  osx-arm64) arch="Apple silicon" ;;
  osx-x64)   arch="Intel" ;;
  *)         arch="$rid" ;;
esac

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

# ditto keeps the bundle's symlinks, permissions and code signature intact.
ditto -x -k "$zip" "$stage"
app="$(find "$stage" -maxdepth 1 -name '*.app' -print -quit)"
[[ -n "$app" ]] || { echo "No .app found in $zip" >&2; exit 1; }

# Unsigned builds: vpk signs only the inner executable ad hoc, which leaves the bundle with an
# invalid signature. Re-sign the whole bundle ad hoc. A Developer ID-signed bundle verifies and
# is left untouched.
if ! codesign --verify --deep --strict "$app" 2>/dev/null; then
  echo "Bundle signature doesn't verify; applying an ad-hoc signature."
  codesign --force --deep --sign - "$app"
  codesign --verify --deep --strict "$app"
fi

ln -s /Applications "$stage/Applications"
sed -e "s/{{VERSION}}/$version/g" -e "s/{{ARCH}}/$arch/g" \
  "$here/READ ME FIRST.txt" > "$stage/READ ME FIRST.txt"

mkdir -p "$out"
dmg="$out/Lumio-$version-$rid.dmg"
rm -f "$dmg"
hdiutil create -volname "Lumio $version" -srcfolder "$stage" -fs HFS+ -format UDZO -ov "$dmg" >/dev/null
echo "Created $dmg"
