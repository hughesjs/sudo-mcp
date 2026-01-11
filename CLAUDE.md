# Claude Code Guidelines for sudo-mcp

## Project Overview

sudo-mcp is a C# MCP server that allows AI models to execute privileged commands via sudo/pkexec. This is a **production tool** despite inherent security risks.

## Key Technologies

- **.NET 10** with C# 12
- **MCP (Model Context Protocol)** - stdio transport
- **System.CommandLine** - argument parsing
- **polkit/pkexec** - privilege escalation
- **xUnit** - testing framework

## Project Structure

```
src/SudoMcp/
├── Program.cs                  # Entry point with System.CommandLine
├── Tools/
│   └── SudoExecutionTool.cs   # MCP tool implementation
├── Services/
│   ├── CommandValidator.cs     # Blocklist validation
│   ├── PkexecExecutor.cs      # pkexec/sudo execution
│   └── AuditLogger.cs         # Structured audit logging
├── Models/
│   ├── ExecutionOptions.cs
│   ├── CommandExecutionResult.cs
│   └── AuditLogEntry.cs
└── Configuration/
    ├── BlockedCommands.json    # Dangerous command patterns
    └── appsettings.json        # Logging config
```

## Development Guidelines

### When Making Changes

1. **Run tests** after modifications: `dotnet test`
2. **Update blocklist** if adding new security patterns
3. **Update SECURITY.md** if changing security model
4. **Update README.md** if changing CLI arguments or features
5. **Update CLAUDE.md** (this file) when project structure changes

### Security Considerations

- **Never disable blocklist by default** in code
- **Always log** command execution attempts (success and failure)
- **British spellings** throughout documentation (authorisation, honour, etc.)
- **Validate inputs** even in non-critical paths
- **Test security features** thoroughly before merging

### Code Style

- Use **nullable reference types** (`string?` vs `string`)
- **XML comments** on public APIs
- **Async/await** for I/O operations
- **Dependency injection** via constructor
- **Structured logging** with ILogger

### Testing Requirements

- **Unit tests** for CommandValidator patterns
- **Integration tests** for end-to-end execution
- **Security tests** for blocklist bypass attempts
- Test with `--no-blocklist` flag behaviour

## Important Implementation Details

### MCP Tool Registration

- Tool class must be marked with `[McpServerToolType]`
- Tool methods marked with `[McpServerTool]`
- Use `[Description]` attributes for MCP metadata
- Namespace: `ModelContextProtocol.Server`

### Timeout Handling

- **Global default**: 15 seconds (CLI `--timeout`)
- **Per-request override**: `timeoutSeconds` parameter in tool
- **Three levels**: CLI default → tool parameter → cancellation token

### Logging to stderr

**CRITICAL**: All logging must go to stderr for MCP stdio transport:
```csharp
builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);
```

### Blocklist Configuration

Three validation strategies:
1. **ExactMatches**: Fast, specific commands
2. **RegexPatterns**: Pattern matching for classes of operations
3. **BlockedBinaries**: Block specific executables

## Common Tasks

### Adding a New Blocked Command Pattern

1. Edit `src/SudoMcp/Configuration/BlockedCommands.json`
2. Add to appropriate section (ExactMatches, RegexPatterns, or BlockedBinaries)
3. Test with `CommandValidatorTests.cs`
4. Document in SECURITY.md

### Changing Default Timeout

1. Update `Program.cs`: `getDefaultValue: () => 15`
2. Update README.md documentation
3. Update SECURITY.md if security implications

### Adding New Command-Line Options

1. Add `Option<T>` in `Program.cs`
2. Add to `rootCommand`
3. Update `SetHandler` lambda parameters
4. Update README.md CLI options table
5. Update `examples/mcp-config.json`

## Do NOT

- ❌ Add Claude as co-author on commits
- ❌ Commit audit.log files
- ❌ Change default to `--no-blocklist`
- ❌ Remove security warnings from README
- ❌ Use American spellings in documentation
- ❌ Run commands requiring sudo without understanding this guideline

## Deployment Notes

### Development

```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj
```

### Production Build

```bash
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

### Testing Integration

Add to Claude Desktop config (~/.config/Claude/mcp.json):
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/sudo-mcp/src/SudoMcp/SudoMcp.csproj"]
    }
  }
}
```

## Maintenance

### Regular Tasks

- Review and update blocklist patterns
- Monitor audit logs for bypass attempts
- Update dependencies: `dotnet list package --outdated`
- Security review before major releases
- Test with latest .NET versions

### Before Releasing

1. Run full test suite: `dotnet test`
2. Build in Release mode: `dotnet build -c Release`
3. Test CLI: `dotnet run -- --help`
4. Verify MCP integration with Claude Desktop
5. Review SECURITY.md for accuracy
6. Update version numbers if applicable

## Resources

- MCP Specification: https://modelcontextprotocol.io/
- .NET 10 Documentation: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/
- System.CommandLine: https://github.com/dotnet/command-line-api
- polkit Documentation: https://www.freedesktop.org/software/polkit/docs/latest/

## Notes

- This project accepts inherent security risks for functionality
- Users must understand and accept all risks (see SECURITY.md)
- Production use is intentional despite dangers
- Comprehensive audit logging is mandatory
