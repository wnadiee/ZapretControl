#!/bin/sh
set -eu
ROOT=$(CDPATH= cd -- "$(dirname "$0")" && pwd)
BUILD="$ROOT/build"
APP="$BUILD/ZapretControl.app"
CONTENTS="$APP/Contents"
MACOS="$CONTENTS/MacOS"
RES="$CONTENTS/Resources"
SDK=$(/usr/bin/xcrun --sdk macosx --show-sdk-path)
SWIFTC=$(/usr/bin/xcrun -f swiftc)

/bin/rm -rf "$BUILD"
/bin/mkdir -p "$MACOS" "$RES"

"$SWIFTC" -O -parse-as-library -sdk "$SDK" -target x86_64-apple-macos13.0 -framework AppKit -framework Foundation "$ROOT/ZapretControlApp.swift" -o "$BUILD/ZapretControl-x86_64"
"$SWIFTC" -O -parse-as-library -sdk "$SDK" -target arm64-apple-macos13.0 -framework AppKit -framework Foundation "$ROOT/ZapretControlApp.swift" -o "$BUILD/ZapretControl-arm64"
/usr/bin/lipo -create "$BUILD/ZapretControl-x86_64" "$BUILD/ZapretControl-arm64" -output "$MACOS/ZapretControl"
/bin/cp "$ROOT/Info.plist" "$CONTENTS/Info.plist"
/bin/chmod +x "$MACOS/ZapretControl"
/usr/bin/codesign --force --deep --sign - "$APP" || true
echo "Built: $APP"