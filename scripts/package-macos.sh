#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  exit 1
fi

PROJECT="avallama/avallama.csproj"

dotnet publish "$PROJECT" -c Release -r osx-x64 --self-contained true -o mac-dist-x64 /p:PublishSingleFile=true

dotnet publish "$PROJECT" -c Release -r osx-arm64 --self-contained true -o mac-dist-arm64 /p:PublishSingleFile=true

create_app_structure() {
  local arch_dir="$1"
  local output_app="$2"

  mkdir -p "$output_app/Contents/"{_CodeSignature,MacOS,Resources}
  cp -a "$arch_dir"/. "$output_app/Contents/MacOS/"
  cp avallama/Assets/Avallama.icns "$output_app/Contents/Resources"

  cat > "$output_app/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>CFBundleIconFile</key>
    <string>Avallama.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.4foureyes.avallama</string>
    <key>CFBundleName</key>
    <string>Avallama</string>
    <key>CFBundleDisplayName</key>
    <string>Avallama</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.12</string>
    <key>CFBundleExecutable</key>
    <string>avallama</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>NSHighResolutionCapable</key>
    <true/>
  </dict>
</plist>
EOF
}

create_app_structure "mac-dist-x64" "Avallama.app"
zip -r "avallama_${VERSION}_osx_x64.zip" Avallama.app

rm -rf Avallama.app/Contents/MacOS/*
cp -a mac-dist-arm64/. Avallama.app/Contents/MacOS/
zip -r "avallama_${VERSION}_osx_arm64.zip" Avallama.app

echo "Done: Created avallama_${VERSION}_osx_x64.zip and avallama_${VERSION}_osx_arm64.zip"
