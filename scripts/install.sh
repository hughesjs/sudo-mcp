#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

OS="$(uname -s)"

case "$OS" in
    Darwin)
        INSTALL_DIR="/usr/local/bin"
        LOG_DIR="$HOME/Library/Logs/sudo-mcp"
        ;;
    Linux)
        INSTALL_DIR="/usr/bin"
        LOG_DIR="/var/log/sudo-mcp"
        ;;
    *)
        echo "Error: Unsupported operating system: $OS"
        exit 1
        ;;
esac

echo "Installing sudo-mcp..."

# Check dependencies
if [ "$OS" = "Linux" ]; then
    if ! command -v pkexec &> /dev/null; then
        echo "Error: pkexec not found. Install polkit first."
        exit 1
    fi
fi

if ! command -v sudo &> /dev/null; then
    echo "Error: sudo not found."
    exit 1
fi

# Check binary exists
if [ ! -f "$SCRIPT_DIR/sudo-mcp" ]; then
    echo "Error: sudo-mcp binary not found in $SCRIPT_DIR"
    exit 1
fi

# Install binary
sudo cp "$SCRIPT_DIR/sudo-mcp" "$INSTALL_DIR/sudo-mcp"
sudo chmod 755 "$INSTALL_DIR/sudo-mcp"

# Create log directory
if [ "$OS" = "Darwin" ]; then
    # macOS: user-owned log directory in ~/Library/Logs
    mkdir -p "$LOG_DIR"
else
    # Linux: system log directory
    sudo mkdir -p "$LOG_DIR"
    sudo chown "$(whoami):$(whoami)" "$LOG_DIR"
fi

echo "Installed to $INSTALL_DIR/sudo-mcp"
echo "Log directory: $LOG_DIR"
