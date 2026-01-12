#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 <version> [--local-source <path>]" >&2
    exit 1
}

if [ $# -lt 1 ]; then
    usage
fi

VERSION="$1"
LOCAL_SOURCE=""

shift
while [[ $# -gt 0 ]]; do
    case $1 in
        --local-source)
            LOCAL_SOURCE="$2"
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done

if [[ -n "$LOCAL_SOURCE" ]]; then
    SOURCE_X64="file://${LOCAL_SOURCE}/sudo-mcp-x64-v${VERSION}.tar.gz"
    SOURCE_ARM64="file://${LOCAL_SOURCE}/sudo-mcp-arm64-v${VERSION}.tar.gz"
else
    SOURCE_X64="https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-x64-v\${pkgver}.tar.gz"
    SOURCE_ARM64="https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-arm64-v\${pkgver}.tar.gz"
fi

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
source_x86_64=("\${pkgname}-\${pkgver}-x64.tar.gz::${SOURCE_X64}")
source_aarch64=("\${pkgname}-\${pkgver}-arm64.tar.gz::${SOURCE_ARM64}")
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
