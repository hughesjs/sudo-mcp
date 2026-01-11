#!/usr/bin/env bash
set -euo pipefail

# sudo-mcp installation script
# Builds and installs sudo-mcp to /usr/local/bin

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
INSTALL_DIR="/usr/local/bin"
CONFIG_DIR="/etc/sudo-mcp"
LOG_DIR="/var/log/sudo-mcp"

echo "=== sudo-mcp Installation Script ==="
echo ""

# Check for .NET 10 SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET 10 SDK first."
    echo "Visit: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "Found .NET SDK: $DOTNET_VERSION"

# Detect architecture
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)
        RID="linux-x64"
        ;;
    aarch64|arm64)
        RID="linux-arm64"
        ;;
    *)
        echo "Error: Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

echo "Architecture: $ARCH (Runtime ID: $RID)"
echo ""

# Build the project
echo "Building sudo-mcp..."
cd "$PROJECT_ROOT"
dotnet publish src/SudoMcp/SudoMcp.csproj \
    -c Release \
    -r "$RID" \
    --self-contained \
    -o "$PROJECT_ROOT/publish" \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=false

echo ""
echo "Build complete. Installing to $INSTALL_DIR..."

# Install binary (requires sudo)
sudo cp "$PROJECT_ROOT/publish/SudoMcp" "$INSTALL_DIR/sudo-mcp"
sudo chmod +x "$INSTALL_DIR/sudo-mcp"

echo "Binary installed to $INSTALL_DIR/sudo-mcp"

# Install default configuration
if [ ! -d "$CONFIG_DIR" ]; then
    echo "Creating configuration directory: $CONFIG_DIR"
    sudo mkdir -p "$CONFIG_DIR"
    sudo cp "$PROJECT_ROOT/src/SudoMcp/Configuration/BlockedCommands.json" "$CONFIG_DIR/"
    echo "Default blocklist installed to $CONFIG_DIR/BlockedCommands.json"
else
    echo "Configuration directory already exists: $CONFIG_DIR"
    if [ ! -f "$CONFIG_DIR/BlockedCommands.json" ]; then
        sudo cp "$PROJECT_ROOT/src/SudoMcp/Configuration/BlockedCommands.json" "$CONFIG_DIR/"
        echo "Default blocklist installed to $CONFIG_DIR/BlockedCommands.json"
    else
        echo "Blocklist already exists (not overwriting): $CONFIG_DIR/BlockedCommands.json"
    fi
fi

# Create log directory
if [ ! -d "$LOG_DIR" ]; then
    echo "Creating log directory: $LOG_DIR"
    sudo mkdir -p "$LOG_DIR"
    sudo chown "$USER:$USER" "$LOG_DIR"
    echo "Log directory created (owned by $USER)"
else
    echo "Log directory already exists: $LOG_DIR"
fi

echo ""
echo "=== Installation Complete ==="
echo ""
echo "Binary location: $INSTALL_DIR/sudo-mcp"
echo "Config location: $CONFIG_DIR/BlockedCommands.json"
echo "Log location: $LOG_DIR/audit.log"
echo ""
echo "Test the installation:"
echo "  $INSTALL_DIR/sudo-mcp --help"
echo ""
echo "Example MCP configuration:"
echo '  {
    "mcpServers": {
      "sudo-mcp": {
        "command": "/usr/local/bin/sudo-mcp"
      }
    }
  }'
echo ""
echo "See examples/ directory for more configuration examples."
