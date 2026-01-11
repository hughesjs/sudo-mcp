#!/usr/bin/env bash
set -euo pipefail

# sudo-mcp uninstallation script

INSTALL_DIR="/usr/local/bin"
CONFIG_DIR="/etc/sudo-mcp"
LOG_DIR="/var/log/sudo-mcp"

echo "=== sudo-mcp Uninstallation Script ==="
echo ""

# Remove binary
if [ -f "$INSTALL_DIR/sudo-mcp" ]; then
    echo "Removing binary: $INSTALL_DIR/sudo-mcp"
    sudo rm "$INSTALL_DIR/sudo-mcp"
    echo "Binary removed"
else
    echo "Binary not found (already removed?): $INSTALL_DIR/sudo-mcp"
fi

# Ask about configuration
if [ -d "$CONFIG_DIR" ]; then
    echo ""
    read -p "Remove configuration directory $CONFIG_DIR? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        sudo rm -rf "$CONFIG_DIR"
        echo "Configuration removed"
    else
        echo "Configuration kept"
    fi
else
    echo "Configuration directory not found: $CONFIG_DIR"
fi

# Ask about logs
if [ -d "$LOG_DIR" ]; then
    echo ""
    read -p "Remove log directory $LOG_DIR (contains audit logs)? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        sudo rm -rf "$LOG_DIR"
        echo "Logs removed"
    else
        echo "Logs kept"
    fi
else
    echo "Log directory not found: $LOG_DIR"
fi

echo ""
echo "=== Uninstallation Complete ==="
