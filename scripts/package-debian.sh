#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  exit 1
fi

PROJECT="avallama/avallama.csproj"

dotnet publish ${PROJECT} \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./out/linux-x64"

rm -rf staging_folder
mkdir staging_folder

mkdir ./staging_folder/DEBIAN
cat > ./staging_folder/DEBIAN/control <<EOF
Package: avallama
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: amd64
Description: User-friendly GUI for Ollama
Maintainer: Márk Csörgő, Martin Bartos - 4foureyes
Homepage: https://github.com/4foureyes/avallama
Copyright: 2025 Márk Csörgő, Martin Bartos
EOF

# Starter script
mkdir ./staging_folder/usr
mkdir ./staging_folder/usr/bin
cp scripts/debian/avallama.sh ./staging_folder/usr/bin/avallama
chmod +x ./staging_folder/usr/bin/avallama # set executable permissions to starter script

# Other files
mkdir ./staging_folder/usr/lib
mkdir ./staging_folder/usr/lib/avallama
cp -f -a ./out/linux-x64/. ./staging_folder/usr/lib/avallama/ # copies all files from publish dir
chmod -R a+rX ./staging_folder/usr/lib/avallama/ # set read permissions to all files
chmod +x ./staging_folder/usr/lib/avallama/avallama # set executable permissions to main executable

# Desktop shortcut
mkdir ./staging_folder/usr/share
mkdir ./staging_folder/usr/share/applications
cp scripts/debian/Avallama.desktop ./staging_folder/usr/share/applications/

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
mkdir ./staging_folder/usr/share/pixmaps
cp scripts/debian/pixmaps/avallama.png ./staging_folder/usr/share/pixmaps/

# Hicolor icons
mkdir ./staging_folder/usr/share/icons
mkdir ./staging_folder/usr/share/icons/hicolor
cp -a scripts/debian/icons/hicolor/. ./staging_folder/usr/share/icons/hicolor/

# Make .deb file
dpkg-deb --root-owner-group --build ./staging_folder/ ./avallama_"${VERSION}"_amd64.deb

