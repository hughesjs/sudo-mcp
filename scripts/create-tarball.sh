#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

usage() {
    echo "Usage: $0 --arch <x64|arm64> --version <version> --binary <path>" >&2
    exit 1
}

ARCH=""
VERSION=""
BINARY=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --arch)
            ARCH="$2"
            shift 2
            ;;
        --version)
            VERSION="$2"
            shift 2
            ;;
        --binary)
            BINARY="$2"
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done

if [[ -z "$ARCH" || -z "$VERSION" || -z "$BINARY" ]]; then
    usage
fi

if [[ ! -f "$BINARY" ]]; then
    echo "Error: Binary not found: $BINARY" >&2
    exit 1
fi

TARBALL_DIR="sudo-mcp-${ARCH}-v${VERSION}"
TARBALL_NAME="${TARBALL_DIR}.tar.gz"

# Clean up any existing directory
rm -rf "$TARBALL_DIR"
mkdir -p "$TARBALL_DIR"

# Copy binary
cp "$BINARY" "${TARBALL_DIR}/sudo-mcp"
chmod +x "${TARBALL_DIR}/sudo-mcp"

# Copy scripts
cp "${PROJECT_ROOT}/scripts/install.sh" "${TARBALL_DIR}/"
cp "${PROJECT_ROOT}/scripts/uninstall.sh" "${TARBALL_DIR}/"
chmod +x "${TARBALL_DIR}/install.sh"
chmod +x "${TARBALL_DIR}/uninstall.sh"

# Copy documentation
cp "${PROJECT_ROOT}/README.md" "${TARBALL_DIR}/"
cp "${PROJECT_ROOT}/SECURITY.md" "${TARBALL_DIR}/"
cp "${PROJECT_ROOT}/LICENSE" "${TARBALL_DIR}/"

# Create tarball
tar -czf "$TARBALL_NAME" "$TARBALL_DIR"

# Cleanup
rm -rf "$TARBALL_DIR"

echo "$TARBALL_NAME"
