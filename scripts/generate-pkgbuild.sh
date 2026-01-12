#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 <version> [--local-source <path>] [--sha256-x64 <hash>] [--sha256-arm64 <hash>]" >&2
    exit 1
}

if [ $# -lt 1 ]; then
    usage
fi

VERSION="$1"
LOCAL_SOURCE=""
SHA256_X64="SKIP"
SHA256_ARM64="SKIP"

shift
while [[ $# -gt 0 ]]; do
    case $1 in
        --local-source)
            LOCAL_SOURCE="$2"
            shift 2
            ;;
        --sha256-x64)
            SHA256_X64="$2"
            shift 2
            ;;
        --sha256-arm64)
            SHA256_ARM64="$2"
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done

if [[ -n "$LOCAL_SOURCE" ]]; then
    # For local sources, use just the filename (tarball must exist in PKGBUILD directory)
    SOURCE_LINE_X64="sudo-mcp-x64-v${VERSION}.tar.gz"
    SOURCE_LINE_ARM64="sudo-mcp-arm64-v${VERSION}.tar.gz"
else
    # For remote sources, use full URL with rename syntax for consistency
    SOURCE_LINE_X64="\${pkgname}-\${pkgver}-x64.tar.gz::https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-x64-v\${pkgver}.tar.gz"
    SOURCE_LINE_ARM64="\${pkgname}-\${pkgver}-arm64.tar.gz::https://github.com/hughesjs/sudo-mcp/releases/download/v\${pkgver}/sudo-mcp-arm64-v\${pkgver}.tar.gz"
fi

cat << EOF
# Maintainer: James Hughes <james@pyrosoftsolutions.co.uk>
pkgname=sudo-mcp
pkgver=${VERSION}
pkgrel=1
pkgdesc="⚠️ INHERENTLY UNSAFE: MCP server allowing AI models to execute privileged commands via sudo/pkexec"
arch=('x86_64' 'aarch64')
url="https://github.com/hughesjs/sudo-mcp"
license=('MIT')
depends=('polkit' 'sudo')
options=('!strip')  # .NET single-file bundles are destroyed by strip
source_x86_64=("${SOURCE_LINE_X64}")
source_aarch64=("${SOURCE_LINE_ARM64}")
sha256sums_x86_64=('${SHA256_X64}')
sha256sums_aarch64=('${SHA256_ARM64}')

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
    install -Dm755 sudo-mcp "\$pkgdir/usr/bin/sudo-mcp"

    # Install documentation
    install -Dm644 README.md "\$pkgdir/usr/share/doc/\$pkgname/README.md"
    install -Dm644 SECURITY.md "\$pkgdir/usr/share/doc/\$pkgname/SECURITY.md"
    install -Dm644 LICENSE "\$pkgdir/usr/share/licenses/\$pkgname/LICENSE"

    # Create log directory
    install -dm755 "\$pkgdir/var/log/sudo-mcp"
}
EOF
