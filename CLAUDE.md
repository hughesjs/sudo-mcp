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
│   ├── AuditLogEntry.cs
│   ├── BlocklistConfiguration.cs  # Runtime blocklist model
│   └── BlocklistDto.cs            # JSON deserialization DTO
└── Configuration/
    ├── DefaultBlocklist.cs     # Embedded default blocklist (source-generated regex)
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

### Running Tests

#### Unit Tests

Run fast unit tests without privilege escalation:
```bash
dotnet test --filter "Category!=Integration"
```

#### Integration Tests

Run full privilege escalation tests using TestContainers:
```bash
# Run all tests (TestContainers manages Docker automatically)
dotnet test

# Run only integration tests
dotnet test --filter "Category=Integration"
```

**Requirements**:
- Docker installed and running
- .NET 10 SDK
- TestContainers automatically builds and manages Docker containers
- No `--privileged` mode needed - polkit configured for internal auth

**How It Works**:
1. xUnit starts test
2. TestContainers builds custom Docker image from `Dockerfile.integration-test`
3. Container started (no `--privileged` needed - polkit configured for passwordless auth)
4. Tests execute commands inside container via `ExecAsync()`
5. Container automatically cleaned up after tests

**Test Structure**:
- `src/SudoMcp.Tests/Integration/SudoMcpContainerFixture.cs` - TestContainers lifecycle management
- `src/SudoMcp.Tests/Integration/PkexecIntegrationTests.cs` - Privilege escalation tests
- `src/SudoMcp.Tests/Integration/BlocklistIntegrationTests.cs` - Command validation tests
- `src/SudoMcp.Tests/Integration/Dockerfile.integration-test` - Docker test environment
- `src/SudoMcp.Tests/Integration/README.md` - Integration test documentation

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
2. **RegexPatterns**: Pattern matching for classes of operations (with 1-second timeout to prevent ReDoS)
3. **BlockedBinaries**: Block specific executables

**Embedded Default Blocklist:**
- Default blocklist is compiled into the binary using source-generated regex
- No external configuration files required for basic operation
- Use `--blocklist-file` to override with a custom JSON blocklist
- Use `--no-blocklist` to disable all validation (dangerous)

**ReDoS Protection:**
- All regex patterns have a 1-second timeout to prevent catastrophic backtracking
- Protects against malicious inputs designed to cause denial of service
- Source-generated regex in `DefaultBlocklist.cs` with `matchTimeoutMilliseconds: 1000`

## Common Tasks

### Adding a New Blocked Command Pattern

1. Edit `src/SudoMcp/Configuration/DefaultBlocklist.cs`
2. Add to appropriate section (ExactMatches, RegexPatterns, or BlockedBinaries)
3. For regex patterns, add a new `[GeneratedRegex]` partial method
4. Test with `CommandValidatorTests.cs`
5. Document in SECURITY.md

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

For development, run directly from source:
```bash
dotnet run --project src/SudoMcp/SudoMcp.csproj -- --help
```

### Production Installation

Use the installation script:
```bash
chmod +x scripts/install.sh
./scripts/install.sh
```

Or build manually:
```bash
dotnet publish src/SudoMcp/SudoMcp.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o ./publish \
    /p:PublishSingleFile=true
sudo cp publish/SudoMcp /usr/bin/sudo-mcp
```

### Testing Integration

Add to Claude Desktop/Code config:
```json
{
  "mcpServers": {
    "sudo-mcp": {
      "command": "/usr/bin/sudo-mcp"
    }
  }
}
```

For development testing with `dotnet run`:
```json
{
  "mcpServers": {
    "sudo-mcp-dev": {
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
3. Test installation script: `./scripts/install.sh`
4. Test installed binary: `sudo-mcp --help`
5. Verify MCP integration with Claude Desktop/Code/Cursor
6. Review SECURITY.md for accuracy
7. Test uninstall script: `./scripts/uninstall.sh`
8. Update version numbers if applicable

## CD/CI Pipeline

### Automated Releases

The project uses GitHub Actions for automated releases on every push to `master`:

**Workflow**: `.github/workflows/cd-pipeline.yml`

**Trigger Conditions**:
- Push to `master` branch
- Changes to `src/**` or `scripts/**`
- Manual workflow dispatch

**Release Process**:
1. **Semantic Versioning**: Version calculated from commit messages
   - `feat:`, `feature:`, `minor:` → minor version bump
   - `fix:`, `bugfix:`, `patch:`, `chore:` → patch version bump
   - `major:`, `breaking:` → major version bump
2. **Multi-Architecture Build**: Parallel builds for x64 and ARM64
3. **Tarball Creation**: Complete packages with binaries, config, scripts, docs
4. **PKGBUILD Generation**: Arch Linux package build file
5. **Git Tag**: Version tag created (e.g., `v0.1.0`)
6. **GitHub Release**: Automatic release with changelog and artifacts

**Release Artifacts**:
- `sudo-mcp-x64-v{version}.tar.gz` - x86_64 complete package
- `sudo-mcp-arm64-v{version}.tar.gz` - ARM64 complete package
- `PKGBUILD` - Arch Linux package file

### Commit Message Guidelines

For proper semantic versioning, use these commit message prefixes:

**Minor version bump** (0.x.0):
```
feat: add new blocklist pattern for systemd commands
feature: implement custom timeout per MCP request
```

**Patch version bump** (0.0.x):
```
fix: correct regex pattern for dd command blocking
bugfix: handle pkexec authentication cancellation properly
perf: optimize blocklist validation regex compilation
chore: update .NET dependencies to 10.0.1
```

**Major version bump** (x.0.0):
```
breaking: change default timeout from 300s to 15s
major: remove support for --no-blocklist flag
```

### Testing Releases

**Manual workflow trigger**:
```bash
# From GitHub UI: Actions → CD Pipeline → Run workflow
```

**Test installation from release**:
```bash
# Download and test x64 tarball
curl -LO https://github.com/hughesjs/sudo-mcp/releases/latest/download/sudo-mcp-x64-v0.1.0.tar.gz
tar -tzf sudo-mcp-x64-v0.1.0.tar.gz  # Verify contents
tar -xzf sudo-mcp-x64-v0.1.0.tar.gz
cd sudo-mcp-x64-v0.1.0
./sudo-mcp --help
./install.sh
```

**Test PKGBUILD** (on Arch Linux):
```bash
curl -LO https://github.com/hughesjs/sudo-mcp/releases/latest/download/PKGBUILD
makepkg -si
```

### Versioning Strategy

**Current version**: `0.1.0` (early development, not production-ready)

**Version progression**:
- `0.x.x` - Pre-1.0 development releases
- `1.0.0` - First stable production release (when ready)
- `1.x.x` - Stable releases with backward compatibility
- `2.0.0+` - Major releases with breaking changes

### CI for Pull Requests

**Workflow**: `.github/workflows/ci-tests.yml`

Runs on all PRs and pushes to master:
- **Unit tests**: Fast tests without Docker (`dotnet test --filter "Category!=Integration"`)
- **Integration tests**: Full privilege escalation tests using TestContainers (`dotnet test --filter "Category=Integration"`)
- Uploads test results as artifacts
- Uses .NET 10 preview on Ubuntu runners

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
