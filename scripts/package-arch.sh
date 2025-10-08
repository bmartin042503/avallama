#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  exit 1
fi

PROJECT="./avallama/avallama.csproj"

rm -rf src pkg
mkdir -p src

dotnet publish "${PROJECT}" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./src/avallama"

cat > src/avallama.desktop <<EOF
[Desktop Entry]
Name=Avallama
Comment=User-friendly GUI for Ollama
Icon=avallama
Exec=avallama
Terminal=false
Type=Application
Categories=Utility
GenericName=Avallama
Keywords=ollama; gui; avallama; artificial; intelligence
EOF

cp scripts/debian/pixmaps/avallama.png src/

cat > PKGBUILD <<EOF
# Maintainer: Márk Csörgő, Martin Bartos (4foureyes)
pkgname=avallama
pkgver=${VERSION}
pkgrel=1
arch=('x86_64')
pkgdesc="User-friendly GUI for Ollama"
url="https://www.github.com/4foureyes/avallama"
license=('MIT')
source=()
sha256sums=()
options=(!debug !strip)

package() {
    mkdir -p "\${pkgdir}/opt/avallama"
    cp -r "\${srcdir}/../src/avallama/"* "\${pkgdir}/opt/avallama/"
    chmod +x "\${pkgdir}/opt/avallama/avallama"

    mkdir -p "\${pkgdir}/usr/bin"
    ln -s /opt/avallama/avallama "\${pkgdir}/usr/bin/avallama"

    install -Dm644 "\${srcdir}/../src/avallama.desktop" "\${pkgdir}/usr/share/applications/avallama.desktop"
    install -Dm644 "\${srcdir}/../src/avallama.png" "\${pkgdir}/usr/share/icons/hicolor/512x512/apps/avallama.png"
}
EOF

makepkg -fs --noconfirm
