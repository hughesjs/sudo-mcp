#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: $0 <version>" >&2
    exit 1
fi

VERSION="$1"

cat << EOF
# Maintainer: James Hughes <james@pyrosoftsolutions.co.uk>
pkgname=sudo-mcp
pkgver=${VERSION}
pkgrel=1
pkgdesc="MCP server for privileged command execution via sudo/pkexec"
arch=('x86_64' 'aarch64')
url="https://github.com/hughesjs/sudo-mcp"
license=('MIT')
depends=('polkit' 'sudo')
source_x86_64=("\${pkgname}-\${pkgver}-x64.tar.gz::https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-x64-v\${pkgver}.tar.gz")
source_aarch64=("\${pkgname}-\${pkgver}-arm64.tar.gz::https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-arm64-v\${pkgver}.tar.gz")
sha256sums_x86_64=('SKIP')
sha256sums_aarch64=('SKIP')

package() {
    case "\$CARCH" in
        x86_64)
            cd "sudo-mcp-x64-v\${pkgver}"
            ;;
        aarch64)
            cd "sudo-mcp-arm64-v\${pkgver}"
            ;;
    esac

    # Install binary
    install -Dm755 sudo-mcp "\$pkgdir/usr/local/bin/sudo-mcp"

    # Install documentation
    install -Dm644 README.md "\$pkgdir/usr/share/doc/\$pkgname/README.md"
    install -Dm644 SECURITY.md "\$pkgdir/usr/share/doc/\$pkgname/SECURITY.md"
    install -Dm644 LICENSE "\$pkgdir/usr/share/licenses/\$pkgname/LICENSE"

    # Create log directory
    install -dm755 "\$pkgdir/var/log/sudo-mcp"
}
EOF
