#!/usr/bin/env bash
# Assembles Lumio.app from a `dotnet publish` folder, ready for `vpk pack --packDir Lumio.app`.
#   Contents/MacOS      the publish output
#   Contents/Info.plist build/macos/Info.plist with the version filled in
#   Contents/Resources  Assets.car compiled from assets/icons/Lumio.icon (the macOS 26+ icon,
#                       which stops macOS drawing the app on a grey plate), plus Lumio.icns for
#                       macOS 11-15.
#
# Usage: build/macos/make-app.sh <publish-dir> <version> <output-dir>
# Needs Xcode's actool for the macOS 26+ icon. In CI a missing actool fails the build; locally
# the script falls back to the legacy icon with a warning.
set -euo pipefail

publish="$1"; version="$2"; out="$3"
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
app="$out/Lumio.app"

rm -rf "$app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp -R "$publish/." "$app/Contents/MacOS/"
sed "s/{{VERSION}}/$version/g" "$here/Info.plist" > "$app/Contents/Info.plist"
cp "$root/assets/icons/lumio-icon.icns" "$app/Contents/Resources/Lumio.icns"

compiled="$(mktemp -d)"
trap 'rm -rf "$compiled"' EXIT
if xcrun actool "$root/assets/icons/Lumio.icon" \
     --compile "$compiled" \
     --app-icon Lumio --include-all-app-icons \
     --platform macosx --target-device mac --minimum-deployment-target 12.0 \
     --output-partial-info-plist "$compiled/partial.plist" \
     --notices --warnings --errors --output-format human-readable-text; then
  cp "$compiled/Assets.car" "$app/Contents/Resources/"
  # actool also renders a matching .icns for older macOS; prefer it over the fallback.
  [[ -f "$compiled/Lumio.icns" ]] && cp "$compiled/Lumio.icns" "$app/Contents/Resources/Lumio.icns"
  echo "Compiled Lumio.icon into Assets.car"
elif [[ "${CI:-}" == "true" ]]; then
  echo "::error::actool couldn't compile assets/icons/Lumio.icon; macOS 26+ would show the icon on a grey plate."
  exit 1
else
  echo "warning: actool unavailable; using the legacy icon only (grey plate on macOS 26+)." >&2
fi

echo "Created $app"
