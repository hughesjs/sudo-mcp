# sudo-mcp

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

MCP (Model Context Protocol) server that allows AI models to execute commands with elevated privileges via sudo and pkexec.

## ⚠️ SECURITY WARNING

**This is an inherently dangerous tool.** By design, it allows an AI model to execute commands with root privileges on your system. While basic safeguards can be configured (command blocklist, audit logging), **NO SECURITY MEASURE IS PERFECT**.

**USE AT YOUR OWN RISK.** This tool could potentially:
- **Destroy data** through file system operations
- **Compromise system security** by modifying critical configurations
- **Modify or delete critical system files** including kernel and boot files
- **Execute malicious commands** if the blocklist is bypassed or disabled
- **Escalate privileges** beyond what you intended
- **Expose sensitive information** through command output

**The `--no-blocklist` flag removes ALL command validation. Using this option gives the AI unrestricted root access to your system.**

**You have been warned.**

## Overview

sudo-mcp is a C# MCP server that integrates with Claude Desktop (or any MCP client) to enable execution of privileged commands. It uses polkit/pkexec for authentication and supports configurable command validation.

**Key Features:**
- 🔐 **Polkit Integration** - Uses pkexec for secure privilege escalation with user authentication
- ✅ **Configurable Command Validation** - Optional blocklist to prevent dangerous operations
- 📝 **Comprehensive Audit Logging** - Every command attempt logged with full details
- ⚙️ **Runtime Configuration** - Command-line arguments for blocklist, timeouts, and logging
- 🚀 **.NET 10** - Built on the latest .NET platform with C# 12

## Prerequisites

- **.NET 10 SDK** - Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/)
- **Linux** - This tool requires polkit/pkexec (Linux-only)
- **Polkit authentication agent** - Required for graphical authentication (typically included in desktop environments)
- **Claude Desktop** - Or any MCP-compatible client

## Installation

### From Source

```bash
# Clone the repository
git clone https://github.com/hughesjs/sudo-mcp.git
cd sudo-mcp

# Build the project
dotnet build

# Run directly (development)
dotnet run --project src/SudoMcp/SudoMcp.csproj

# Or publish for production
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

## Command-Line Options

sudo-mcp supports the following command-line arguments for runtime configuration:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--blocklist-file <path>` | `-b` | Path to custom blocklist JSON file | `Configuration/BlockedCommands.json` |
| `--no-blocklist` | - | **DANGEROUS**: Disable all command validation | `false` |
| `--audit-log <path>` | `-a` | Path to audit log file | `/var/log/sudo-mcp/audit.log` |
| `--timeout <seconds>` | `-t` | Command execution timeout in seconds | `300` (5 minutes) |

### Examples

**Default configuration (with blocklist):**
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj
```

**Custom blocklist:**
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- \
  --blocklist-file /path/to/custom-blocklist.json
```

**No blocklist (MAXIMUM DANGER):**
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- --no-blocklist
```

**Custom timeout and audit log:**
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- \
  --timeout 600 \
  --audit-log /var/log/sudo-mcp/custom-audit.log
```

**Display help:**
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- --help
```

## Configuration

### Blocklist Configuration

The default blocklist (`src/SudoMcp/Configuration/BlockedCommands.json`) prevents execution of dangerous commands using three strategies:

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

**Customising the blocklist:**

Create your own JSON file and pass it via `--blocklist-file`:
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- \
  --blocklist-file /etc/sudo-mcp/my-blocklist.json
```

**Disabling the blocklist:**

⚠️ **WARNING**: Only use `--no-blocklist` in isolated/test environments where you fully accept the risk.

```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- --no-blocklist
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
dotnet run --project src/SudoMcp/SudoMcp.csproj -- \
  --audit-log /home/user/.sudo-mcp/audit.log
```

**Ensure the log directory exists and is writable:**
```bash
sudo mkdir -p /var/log/sudo-mcp
sudo chown $USER:$USER /var/log/sudo-mcp
```

## Claude Desktop Integration

Add sudo-mcp to your Claude Desktop configuration file.

**Configuration file location:**
- **Linux**: `~/.config/Claude/mcp.json`
- **macOS**: `~/Library/Application Support/Claude/mcp.json`

### Development Setup (Default Blocklist)

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/james/repos/sudo-mcp/src/SudoMcp/SudoMcp.csproj"
      ]
    }
  }
}
```

### Development Setup (Custom Blocklist)

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/james/repos/sudo-mcp/src/SudoMcp/SudoMcp.csproj",
        "--",
        "--blocklist-file",
        "/home/james/.config/sudo-mcp/my-blocklist.json",
        "--timeout",
        "600"
      ]
    }
  }
}
```

### Development Setup (No Blocklist - DANGER)

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/james/repos/sudo-mcp/src/SudoMcp/SudoMcp.csproj",
        "--",
        "--no-blocklist"
      ]
    }
  }
}
```

### Production Setup (Published Binary)

After publishing the application:

```bash
dotnet publish -c Release -r linux-x64 --self-contained -o /opt/sudo-mcp
```

```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/opt/sudo-mcp/SudoMcp",
      "args": [
        "--blocklist-file",
        "/etc/sudo-mcp/blocklist.json",
        "--audit-log",
        "/var/log/sudo-mcp/audit.log"
      ]
    }
  }
}
```

**See [CLAUDE_DESKTOP_SETUP.md](CLAUDE_DESKTOP_SETUP.md) for detailed integration instructions.**

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

```bash
dotnet test
```

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
