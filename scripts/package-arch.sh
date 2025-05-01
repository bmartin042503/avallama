#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  exit 1
fi

PROJECT="avallama/avallama.csproj"
SOURCE_TAR="avallama_${VERSION}_arch_x64.tar.gz"

dotnet publish ${PROJECT} \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./out/linux-x64"

rm -rf avallama
mkdir -p avallama
cp -r ./out/linux-x64/* avallama/

cat > avallama/avallama.desktop <<EOF
[Desktop Entry]
Name=Avallama
Comment=User-friendly GUI for Ollama
Icon=avallama
Exec=avallama
Terminal=false
Type=Application
Categories=Utility
GenericName=Avallama
Keywords=ollama; gui; avallama; artifical; intelligence
EOF

cp scripts/debian/pixmaps/avallama.png avallama/

tar -czvf "$SOURCE_TAR" avallama/

SHA256_SUM=$(sha256sum "$SOURCE_TAR" | awk '{ print $1 }')

cat > PKGBUILD <<EOF
# Maintainer: Márk Csörgő, Martin Bartos (4foureyes)
pkgname=avallama
pkgver=${VERSION}
pkgrel=1
arch=('x86_64')
pkgdesc="User-friendly GUI for Ollama"
url="https://www.github.com/4foureyes/avallama"
license=('MIT')
source=(${SOURCE_TAR})
sha256sums=("${SHA256_SUM}")

package() {
    install -Dm755 "${srcdir}/avallama/avallama" "${pkgdir}/usr/bin/avallama"
    install -Dm644 "${srcdir}/avallama/avallama.desktop" "${pkgdir}/usr/share/applications/avallama.desktop"
    install -Dm644 "${srcdir}/avallama/avallama.png" "${pkgdir}/usr/share/icons/hicolor/512x512/apps/avallama.png"
    echo "Avallama ${VERSION} is installed"
}
EOF