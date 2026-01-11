# sudo-mcp Configuration Examples

This directory contains example MCP configuration files for different AI coding tools.

## Configuration Files

### Claude Desktop (`claude-desktop-config.json`)

**Location**: `~/.config/Claude/claude_desktop_config.json` (Linux/macOS)
**Location**: `%APPDATA%\Claude\claude_desktop_config.json` (Windows)

Copy the contents of `claude-desktop-config.json` to your Claude Desktop configuration file.

**Available configurations**:
- `sudo-mcp` - Standard configuration using installed binary
- `sudo-mcp-custom` - Custom blocklist and timeout settings
- `sudo-mcp-no-blocklist` - **DANGEROUS**: Disables all command validation

### Claude Code (`claude-code-config.json`)

**Location**: `~/.config/claude/mcp.json` (Linux)
**Location**: `~/Library/Application Support/Claude/mcp.json` (macOS)
**Location**: `%APPDATA%\Claude\mcp.json` (Windows)

Copy the contents of `claude-code-config.json` to your Claude Code configuration file.

**Available configurations**:
- `sudo-mcp` - Standard configuration using installed binary
- `sudo-mcp-custom` - Custom blocklist and timeout settings
- `sudo-mcp-no-blocklist` - **DANGEROUS**: Disables all command validation

### Cursor (`cursor-config.json`)

**Location**: Project-specific `.cursor/mcp.json` or global Cursor settings

Copy the contents of `cursor-config.json` to your Cursor MCP configuration.

**Available configurations**:
- `sudo-mcp` - Standard configuration using installed binary
- `sudo-mcp-project-specific` - Project-specific configuration using workspace variables
- `sudo-mcp-no-blocklist` - **DANGEROUS**: Disables all command validation

## Usage

### Setup (All Tools)

1. Install sudo-mcp using the installation script:
   ```bash
   git clone https://github.com/hughesjs/sudo-mcp.git
   cd sudo-mcp
   chmod +x scripts/install.sh
   ./scripts/install.sh
   ```

2. Copy the appropriate configuration file to your MCP client's configuration location

3. Update `YOUR_USERNAME` in paths if using custom configurations

4. Restart your AI coding tool to load the MCP server

5. Test the connection by asking: *"Can you list the available MCP tools?"*

## Command-Line Options

All configurations support these command-line options:

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--blocklist-file <path>` | `-b` | Path to custom blocklist JSON file | Embedded default |
| `--no-blocklist` | | **DANGEROUS**: Disable all validation | `false` |
| `--audit-log <path>` | `-a` | Path to audit log file | `/var/log/sudo-mcp/audit.log` |
| `--timeout <seconds>` | `-t` | Default command timeout | `15` |

## Security Profiles

### Conservative (Recommended)

Use the default `sudo-mcp` configuration with the standard blocklist:
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/local/bin/sudo-mcp"
    }
  }
}
```

### Permissive (Development/Testing)

Use `sudo-mcp-custom` with a relaxed blocklist and longer timeout:
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/local/bin/sudo-mcp",
      "args": [
        "--blocklist-file",
        "/path/to/permissive-blocklist.json",
        "--timeout",
        "60"
      ]
    }
  }
}
```

### No Restrictions (MAXIMUM DANGER)

Use `sudo-mcp-no-blocklist` **ONLY** in isolated test environments:
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/local/bin/sudo-mcp",
      "args": ["--no-blocklist"]
    }
  }
}
```

⚠️ **WARNING**: This configuration gives the AI unrestricted root access. Use only in disposable VMs.

## Troubleshooting

### Server Not Appearing

1. Check the MCP configuration file location for your tool
2. Verify the project path is correct and absolute
3. Check logs in stderr for error messages
4. Ensure .NET 10 SDK is installed: `dotnet --version`

### Authentication Failures

1. Verify pkexec is installed: `which pkexec`
2. Ensure a polkit authentication agent is running (required for GUI auth)
3. Check polkit policies: `pkaction --action-id org.freedesktop.policykit.exec`

### Commands Being Blocked

1. Check the audit log for denial reasons: `/var/log/sudo-mcp/audit.log`
2. The default blocklist is embedded in the binary
3. Create a custom blocklist JSON file with more permissive patterns and use `--blocklist-file`
4. **Last resort**: Use `--no-blocklist` in an isolated environment

### Timeout Issues

1. Increase the global timeout: `--timeout 60`
2. Ask the AI to pass a longer timeout in the tool request
3. Check if the command is hanging (requires user input, etc.)

## Example Prompts

Once configured, try these prompts with your AI coding tool:

### Safe Commands
- *"What user am I running as?"* (executes `whoami`)
- *"Show me the system uptime"* (executes `uptime`)
- *"List running services"* (executes `systemctl list-units --type=service`)

### Blocked Commands (should be denied)
- *"Delete everything in the root directory"* (should block `rm -rf /`)
- *"Format my hard drive"* (should block `mkfs` commands)

### Custom Timeout
- *"Run a system update with a 5 minute timeout"* (AI should pass `timeoutSeconds: 300`)

## Support

For issues, questions, or security concerns, see the main [README.md](../README.md) and [SECURITY.md](../SECURITY.md).
