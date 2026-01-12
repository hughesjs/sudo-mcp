# sudo-mcp

[![CI Pipeline](https://img.shields.io/github/actions/workflow/status/hughesjs/sudo-mcp/ci-pipeline.yml?style=for-the-badge&label=CI)](https://github.com/hughesjs/sudo-mcp/actions/workflows/ci-pipeline.yml)
[![CD Pipeline](https://img.shields.io/github/actions/workflow/status/hughesjs/sudo-mcp/cd-pipeline.yml?style=for-the-badge&label=CD)](https://github.com/hughesjs/sudo-mcp/actions/workflows/cd-pipeline.yml)
[![AUR Version](https://img.shields.io/aur/version/sudo-mcp?style=for-the-badge&logo=arch-linux&color=1793d1)](https://aur.archlinux.org/packages/sudo-mcp)
[![AUR Votes](https://img.shields.io/aur/votes/sudo-mcp?style=for-the-badge&logo=arch-linux)](https://aur.archlinux.org/packages/sudo-mcp)
[![AUR Popularity](https://img.shields.io/aur/popularity/sudo-mcp?style=for-the-badge&logo=arch-linux)](https://aur.archlinux.org/packages/sudo-mcp)
[![License](https://img.shields.io/github/license/hughesjs/sudo-mcp?style=for-the-badge)](https://github.com/hughesjs/sudo-mcp/blob/master/LICENSE)
[![Made in Scotland](https://raw.githubusercontent.com/hughesjs/custom-badges/master/made-in/made-in-scotland.svg)](https://github.com/hughesjs/custom-badges)

MCP (Model Context Protocol) server that allows AI models to execute commands with elevated privileges via sudo and pkexec. When you attempt to run an elevated command your configured polkit agent will interactively prompt for your password.

> [!CAUTION]
> ## <img src="https://i.gifer.com/ULMC.gif" width="20"> SECURITY WARNING <img src="https://i.gifer.com/ULMC.gif" width="20">
>
> As G.K. Chesterton said: "Don't ever take a fence down until you know the reason it was put up". 
> 
> **This is an inherently dangerous tool.** By design, it allows an AI model to execute commands with root privileges on your system. While basic safeguards can be configured (command blocklist, audit logging), **NO SECURITY MEASURE IS PERFECT**.
> 
> **USE AT YOUR OWN RISK.** This tool could potentially:
> - **Destroy data** through file system operations
> - **Compromise system security** by modifying critical configurations
> - **Modify or delete critical system files** including kernel and boot files
> - **Execute malicious commands** if the blocklist is bypassed or disabled
> - **Escalate privileges** beyond what you intended
> - **Expose sensitive information** through command output
> 
> **The `--no-blocklist` flag removes ALL command validation. Using this option gives the AI unrestricted root access to your system.**
> 
> **You have been warned.**

## Overview

sudo-mcp is a C# MCP server that integrates with Claude Desktop (or any MCP client) to enable execution of privileged commands. It uses polkit/pkexec for authentication and supports configurable command validation.

**Key Features:**
- 🔐 **Polkit Integration** - Uses pkexec for secure privilege escalation with user authentication
- ✅ **Configurable Command Validation** - Optional blocklist to prevent dangerous operations
- 📝 **Comprehensive Audit Logging** - Every command attempt logged with full details
- ⚙️ **Runtime Configuration** - Command-line arguments for blocklist, timeouts, and logging
- 🚀 **.NET 10** - Built on the latest .NET platform with C# 12

## Quick Start (Claude Code)

**Arch Linux:**
```bash
yay -S sudo-mcp                            # Install from AUR
claude mcp add sudo-mcp /usr/bin/sudo-mcp  # Configure Claude Code
```

**Other Linux distributions:** Download from [releases](https://github.com/hughesjs/sudo-mcp/releases/latest), extract, run `./install.sh`, then use `claude mcp add`.

Restart Claude Code and approve polkit authentication prompts when commands execute. **Read the [security warning](#%EF%B8%8F-security-warning) above before use.**

## Prerequisites

- **(If Building) .NET 10 SDK** - Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/)
- **Linux** - This tool requires polkit/pkexec (Linux-only)
- **Polkit authentication agent** - Required for graphical authentication (typically included in desktop environments)
- **Claude Desktop** - Or any MCP-compatible client

## Installation

### Binary Release (Recommended)

**Step 1**: Visit the [Releases Page](https://github.com/hughesjs/sudo-mcp/releases/latest) to download the latest version for your architecture.

**Step 2**: Extract and install:

**x86_64 (Intel/AMD)**:
```bash
# Replace VERSION with the version you downloaded (e.g., 0.1.0)
tar -xzf sudo-mcp-x64-vVERSION.tar.gz
cd sudo-mcp-x64-vVERSION
chmod +x install.sh
./install.sh
```

**ARM64 (aarch64)**:
```bash
# Replace VERSION with the version you downloaded (e.g., 0.1.0)
tar -xzf sudo-mcp-arm64-vVERSION.tar.gz
cd sudo-mcp-arm64-vVERSION
chmod +x install.sh
./install.sh
```

### Arch Linux (AUR)

**From AUR** (Recommended):
```bash
yay -S sudo-mcp
```

**Manual PKGBUILD**:

If you prefer to build manually, visit the [Releases Page](https://github.com/hughesjs/sudo-mcp/releases/latest), download the PKGBUILD, and run:
```bash
makepkg -si
```

### From Source (Development)

```bash
git clone https://github.com/hughesjs/sudo-mcp.git
cd sudo-mcp
chmod +x scripts/install.sh
./scripts/install.sh
```

This builds from source and installs to `/usr/bin/sudo-mcp` with default configuration.

### Manual Build and Installation

```bash
# Clone and build
git clone https://github.com/hughesjs/sudo-mcp.git
cd sudo-mcp

# Build self-contained binary
dotnet publish src/SudoMcp/SudoMcp.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o ./publish \
    /p:PublishSingleFile=true

# Install system-wide
sudo cp publish/SudoMcp /usr/bin/sudo-mcp
sudo chmod +x /usr/bin/sudo-mcp

# Set up log directory
sudo mkdir -p /var/log/sudo-mcp
sudo chown $USER:$USER /var/log/sudo-mcp
```

### Verify Installation

```bash
sudo-mcp --help
```

### Uninstall

```bash
chmod +x scripts/uninstall.sh
./scripts/uninstall.sh
```

## Command-Line Options

sudo-mcp supports the following command-line arguments for runtime configuration:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--blocklist-file <path>` | `-b` | Path to custom blocklist JSON file | Embedded default |
| `--no-blocklist` | - | **DANGEROUS**: Disable all command validation | `false` |
| `--audit-log <path>` | `-a` | Path to audit log file | `/var/log/sudo-mcp/audit.log` |
| `--timeout <seconds>` | `-t` | Command execution timeout in seconds | `15` |

### Examples

**Default configuration (with blocklist):**
```bash
sudo-mcp
```

**Custom blocklist:**
```bash
sudo-mcp --blocklist-file /path/to/custom-blocklist.json
```

**No blocklist (MAXIMUM DANGER):**
```bash
sudo-mcp --no-blocklist
```

**Custom timeout and audit log:**
```bash
sudo-mcp --timeout 60 --audit-log /home/user/.sudo-mcp/audit.log
```

**Display help:**
```bash
sudo-mcp --help
```

## Configuration

### Blocklist Configuration

sudo-mcp includes an embedded default blocklist that prevents execution of dangerous commands using three strategies:

1. **Exact Matches** - Specific dangerous commands (e.g., `rm -rf /`)
2. **Regex Patterns** - Pattern-based blocking for classes of operations
3. **Blocked Binaries** - Specific executables regardless of arguments

**Example blocklist:**
```json
{
  "BlockedCommands": {
    "ExactMatches": [
      "rm -rf /",
      "mkfs",
      "dd"
    ],
    "RegexPatterns": [
      "^rm\\s+(-rf?|--recursive)\\s+/\\s*$",
      "^dd\\s+if=.+\\s+of=/dev/(sd[a-z]|nvme[0-9]n[0-9]).*$",
      "^mkfs\\..*"
    ],
    "BlockedBinaries": [
      "mkfs.ext4",
      "shred",
      "cryptsetup"
    ]
  }
}
```

**Example blocklist files** are provided in the `examples/` directory:

- **`blocklist-default.json`** - Exact copy of embedded default (reference implementation)
- **`blocklist-permissive.json`** - Relaxed rules for development environments
- **`blocklist-strict.json`** - Enhanced security for production-adjacent environments
- **`blocklist-minimal.json`** - Bare minimum for testing in disposable VMs

See [`examples/blocklist-README.md`](examples/blocklist-README.md) for detailed documentation.

**Using an example blocklist:**
```bash
sudo-mcp --blocklist-file examples/blocklist-permissive.json
```

**Customising the blocklist:**

Create your own JSON file and pass it via `--blocklist-file`:
```bash
sudo-mcp --blocklist-file /etc/sudo-mcp/my-blocklist.json
```

**Disabling the blocklist:**

⚠️ **WARNING**: Only use `--no-blocklist` in isolated/test environments where you fully accept the risk.

```bash
sudo-mcp --no-blocklist
```

### Audit Logging

All command execution attempts (both allowed and denied) are logged in JSON format:

```json
{
  "Timestamp": "2026-01-11T18:45:00Z",
  "EventType": "CommandExecuted",
  "Command": "systemctl restart nginx",
  "User": "james",
  "ExitCode": 0,
  "Success": true
}
```

**Custom log location:**
```bash
sudo-mcp --audit-log /home/user/.sudo-mcp/audit.log
```

**Ensure the log directory exists and is writable:**
```bash
sudo mkdir -p /var/log/sudo-mcp
sudo chown $USER:$USER /var/log/sudo-mcp
```

## MCP Client Integration

After installation, configure your MCP client to use sudo-mcp.

### Claude Desktop

**Configuration file location:**
- **Linux**: `~/.config/Claude/claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

**Basic configuration:**
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/bin/sudo-mcp"
    }
  }
}
```

**Custom configuration:**
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/bin/sudo-mcp",
      "args": [
        "--blocklist-file",
        "/home/YOUR_USERNAME/.config/sudo-mcp/blocklist.json",
        "--timeout",
        "30"
      ]
    }
  }
}
```

### Claude Code

**Using the CLI (Recommended):**

After installing the binary, simply run:
```bash
claude mcp add sudo-mcp /usr/bin/sudo-mcp
```

This automatically configures sudo-mcp in your Claude Code settings.

**With custom arguments:**
```bash
claude mcp add sudo-mcp /usr/bin/sudo-mcp -- --timeout 30 --blocklist-file /path/to/custom-blocklist.json
```

**Manual configuration:**

Alternatively, you can manually edit the configuration file:
- **Linux**: `~/.config/claude/config.json`
- **macOS**: `~/Library/Application Support/Claude/config.json`

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/bin/sudo-mcp"
    }
  }
}
```

### Cursor

Add to your Cursor MCP settings or project-specific `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/bin/sudo-mcp"
    }
  }
}
```

**See [examples/](examples/) directory for more configuration examples.**

## Usage Examples

Once configured with Claude Desktop, you can ask Claude to execute commands:

**Safe command:**
```
User: "Check if nginx is running"
Claude: [Uses sudo-mcp to execute: systemctl status nginx]
```

**Command requiring authentication:**
```
User: "Restart the nginx service"
Claude: [Uses sudo-mcp to execute: systemctl restart nginx]
[Polkit authentication dialog appears]
[User approves the action]
```

**Blocked command (with blocklist enabled):**
```
User: "Format /dev/sda"
Claude: [sudo-mcp blocks the command]
"This command is blocked for safety: matches dangerous pattern"
```

## How It Works

1. **Claude requests command execution** via MCP protocol over stdio
2. **sudo-mcp receives the request** and validates the command
3. **CommandValidator checks** the command against the blocklist (if enabled)
4. **If allowed**, PkexecExecutor spawns `pkexec sudo <command>`
5. **Polkit authentication dialog** appears asking for user approval
6. **User approves or denies** the privilege escalation
7. **Command executes** (if approved) and output is captured
8. **AuditLogger records** the execution attempt with full details
9. **Results returned** to Claude with stdout/stderr/exit code

## Architecture

```
Claude Desktop (MCP Client)
    ↓ JSON-RPC over stdio
MCP Server (stdio transport)
    ↓
SudoExecutionTool
    ↓
CommandValidator → [validate against blocklist]
    ↓
PkexecExecutor → [spawn pkexec → sudo process]
    ↓
AuditLogger → [log to audit file]
```

## Security Considerations

**See [SECURITY.md](SECURITY.md) for comprehensive security documentation.**

### Key Limitations

- **Blocklist bypass**: Regex patterns can potentially be circumvented
- **LLM context**: The AI may not fully understand command implications
- **No rollback**: Executed commands cannot be undone
- **Log tampering**: Audit logs can be deleted with sudo access
- **GUI required**: pkexec requires a polkit authentication agent (graphical session)

### Recommended Practices

- **Run in isolated environment**: Use a dedicated VM or container
- **Review audit logs regularly**: Check `/var/log/sudo-mcp/audit.log`
- **Start with blocklist**: Only use `--no-blocklist` when absolutely necessary
- **Test in safe environment**: Verify behaviour before production use
- **Monitor system changes**: Use file integrity monitoring (e.g., AIDE, Tripwire)

## Troubleshooting

### pkexec authentication fails

**Problem**: `Authorization failed (exit code 127)`

**Solution**: Ensure your user is in the appropriate group (e.g., `sudo`, `wheel`) and polkit policies allow the action:
```bash
groups $USER
pkexec whoami  # Test pkexec authentication
```

### Authentication dialog doesn't appear

**Problem**: No polkit dialog shown

**Solution**: Ensure a polkit authentication agent is running:
```bash
# Check for polkit agent
ps aux | grep polkit

# Install polkit-gnome (GNOME) or polkit-kde (KDE)
sudo pacman -S polkit-gnome  # Arch Linux
sudo apt install policykit-1-gnome  # Debian/Ubuntu
```

### Audit log not created

**Problem**: Audit log file not being written

**Solution**: Ensure the directory exists and is writable:
```bash
sudo mkdir -p /var/log/sudo-mcp
sudo chown $USER:$USER /var/log/sudo-mcp
```

Or use a custom location:
```bash
dotnet run -- --audit-log /home/$USER/.sudo-mcp/audit.log
```

### Command timeout

**Problem**: Long-running commands are terminated

**Solution**: Increase the timeout value:
```bash
dotnet run -- --timeout 1800  # 30 minutes
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

**All Tests** (requires Docker):
```bash
dotnet test
```

**Unit Tests Only** (fast, no Docker):
```bash
dotnet test --filter "Category!=Integration"
```

**Integration Tests Only** (requires Docker):
```bash
dotnet test --filter "Category=Integration"
```

Tests use [TestContainers.NET](https://dotnet.testcontainers.org/) to automatically manage Docker containers. See [src/SudoMcp.Tests/Integration/README.md](src/SudoMcp.Tests/Integration/README.md) for details.

### Project Structure

```
sudo-mcp/
├── src/
│   ├── SudoMcp/              # Main application
│   │   ├── Tools/            # MCP tool implementations
│   │   ├── Services/         # Core services (validator, executor, logger)
│   │   ├── Models/           # Data models
│   │   └── Configuration/    # Config files (blocklist, appsettings)
│   └── SudoMcp.Tests/        # Unit and integration tests
├── examples/                 # Example configurations
├── README.md
├── SECURITY.md
└── CLAUDE_DESKTOP_SETUP.md
```

## Contributing

Contributions are welcome! Please:

1. Read [SECURITY.md](SECURITY.md) to understand security implications
2. Follow existing code style (C# conventions)
3. Add tests for new functionality
4. Update documentation as needed

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

- Built with [Model Context Protocol](https://modelcontextprotocol.io/)
- Uses [System.CommandLine](https://github.com/dotnet/command-line-api) for argument parsing
- Powered by [.NET 10](https://dot.net)

## Related Documentation

- [SECURITY.md](SECURITY.md) - Comprehensive security documentation and threat model
- [CLAUDE_DESKTOP_SETUP.md](CLAUDE_DESKTOP_SETUP.md) - Detailed Claude Desktop integration guide

---

**Remember: This tool gives an AI model root access to your system. Use with extreme caution.**
