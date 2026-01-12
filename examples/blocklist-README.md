# Blocklist Configuration Examples

This directory contains four pre-configured blocklist profiles demonstrating different security postures for sudo-mcp.

## Quick Reference

| Profile | Security Level | Patterns | Use Case |
|---------|---------------|----------|----------|
| **default** | Moderate | 7 exact + 13 regex + 9 binaries | General purpose, balanced security |
| **permissive** | Low | 2 exact + 3 regex + 5 binaries | Development environments |
| **strict** | High | 12 exact + 21 regex + 15 binaries | Production/CI/CD environments |
| **minimal** | Minimal | 2 exact + 1 regex + 0 binaries | Disposable testing VMs |

## Profile Descriptions

### `blocklist-default.json` - Default Embedded Blocklist

**Exact copy of the embedded default blocklist** compiled into sudo-mcp.

**Security level**: Moderate
**Use case**: General purpose usage, balancing security and productivity
**Patterns**: 7 exact matches, 13 regex patterns, 9 blocked binaries

**Blocks**:
- Root filesystem deletion (`rm -rf /`, `rm -rf /*`)
- Direct disk operations (`dd`, `mkfs`, filesystem writes)
- Fork bombs (`:(){:|:&};:`)
- Recursive permission changes on root
- Command injection via curl/wget pipes
- Disk partitioning tools (fdisk, parted)
- Disk wiping utilities (wipefs, shred, cryptsetup)

**Example usage**:
```bash
# Use this as a reference or starting point for customisation
sudo-mcp --blocklist-file examples/blocklist-default.json
```

---

### `blocklist-permissive.json` - Development Profile

**Relaxed rules for local development** where productivity is prioritised over strict security.

**Security level**: Low
**Use case**: Personal development machines, trusted local environments
**Patterns**: 2 exact matches, 3 regex patterns, 5 blocked binaries

**Blocks only catastrophic operations**:
- Root filesystem deletion (`rm -rf /`, `rm -rf /*`)
- Direct disk writes (`dd if=... of=/dev/...`)
- Redirection to disk devices (`> /dev/sda`)
- Filesystem creation on main filesystems (mkfs.ext*, mkfs.xfs, mkfs.btrfs)

**Allows** (compared to default):
- Fork bombs (useful for testing)
- curl/wget piping (common in dev workflows)
- Disk partitioning tools (needed for VM/container work)
- chmod/chown operations (common in development)
- Most disk utilities (wipefs, shred, etc.)

**Example usage**:
```bash
sudo-mcp --blocklist-file examples/blocklist-permissive.json
```

**Warning**: This profile prioritises convenience. Only use on systems where you accept higher risk.

---

### `blocklist-strict.json` - Production Profile

**Enhanced security for production-adjacent environments** with additional protections beyond the default.

**Security level**: High
**Use case**: CI/CD runners, staging servers, shared development environments
**Patterns**: 12 exact matches, 21 regex patterns, 15 blocked binaries

**Additional protections** (beyond default):
- **System shutdown/reboot**: `init 0/6`, `shutdown`, `reboot`, `poweroff`, `halt`
- **systemd manipulation**: `systemctl enable/disable/mask`
- **Package management**: `apt install/remove`, `pacman -S`, `yum install`, `dnf install`
- **Network configuration**: `ip addr/link/route`, `ifconfig`
- **Firewall changes**: `iptables`, `ufw`, `firewalld`, `firewall-cmd`
- **User/group management**: `useradd`, `userdel`, `usermod`, `groupadd`, `groupdel`
- **Security policy**: `setenforce`, `aa-enforce`, `aa-complain`
- **Cron deletion**: `crontab -r`
- **SSH service stop**: Prevents locking yourself out

**Example usage**:
```bash
sudo-mcp --blocklist-file examples/blocklist-strict.json --timeout 30
```

**Recommended for**:
- Build servers where system configuration shouldn't change
- Shared environments where multiple users/teams operate
- Automation systems where unexpected changes are dangerous

---

### `blocklist-minimal.json` - Testing Profile

**Bare minimum protection** for disposable testing environments.

**Security level**: Minimal
**Use case**: Throwaway VMs, temporary test containers, educational environments
**Patterns**: 2 exact matches, 1 regex pattern, 0 blocked binaries

**Blocks only**:
- Root filesystem deletion (`rm -rf /`)
- Fork bombs (`:(){:|:&};:`)

**Allows everything else**, including:
- Disk operations (dd, mkfs, wipefs, etc.)
- Partitioning tools
- System configuration changes
- Package installation
- Network changes

**Example usage**:
```bash
sudo-mcp --blocklist-file examples/blocklist-minimal.json
```

**Warning**: This profile provides minimal protection. Only use in environments you can destroy and recreate.

---

## JSON Structure

All blocklist files follow this structure:

```json
{
  "BlockedCommands": {
    "ExactMatches": [
      "command that must match exactly (case-insensitive)"
    ],
    "RegexPatterns": [
      "^regex.*pattern.*with.*anchors$"
    ],
    "BlockedBinaries": [
      "binary_name",
      "/full/path/to/binary"
    ]
  }
}
```

### Field Descriptions

**`ExactMatches`** (List of strings):
- Commands that must match **exactly** (after normalising whitespace)
- Case-insensitive comparison
- Fastest validation method
- Use for well-known dangerous commands

**`RegexPatterns`** (List of regex strings):
- ECMAScript-compatible regular expressions
- Case-insensitive by default
- Each pattern has a 1-second timeout to prevent ReDoS attacks
- Use anchors (`^...$`) for precise matching
- Use for command families or patterns

**`BlockedBinaries`** (List of strings):
- Binary names or full paths to block
- Checked against the **first token** of the command
- Matches both basename and full path
- Case-insensitive
- Use for dangerous executables regardless of arguments

### Validation Order

Commands are checked in this order:
1. **Exact match** against `ExactMatches`
2. **Regex match** against all `RegexPatterns` (stops at first match)
3. **Binary match** against `BlockedBinaries`

If any check matches, the command is **blocked**.

---

## Customisation Guide

### Creating Your Own Blocklist

1. **Start with a base profile**:
   ```bash
   cp examples/blocklist-default.json my-blocklist.json
   ```

2. **Edit the JSON file** to add/remove patterns:
   ```json
   {
     "BlockedCommands": {
       "ExactMatches": ["your", "commands", "here"],
       "RegexPatterns": ["^your.*regex$"],
       "BlockedBinaries": ["your-binary"]
     }
   }
   ```

3. **Test your blocklist**:
   ```bash
   sudo-mcp --blocklist-file my-blocklist.json --help
   # Should load without errors
   ```

4. **Verify blocking behaviour**:
   Test that dangerous commands are blocked and safe commands are allowed.

### Adding a New Pattern

**Exact match** (for specific commands):
```json
"ExactMatches": [
  "rm -rf /home/important"
]
```

**Regex pattern** (for command families):
```json
"RegexPatterns": [
  "^systemctl\\s+stop\\s+critical-service.*$"
]
```

**Binary block** (for dangerous executables):
```json
"BlockedBinaries": [
  "dangerous-tool"
]
```

### Pattern Writing Tips

1. **Use anchors**: Always anchor patterns with `^` and `$` for precision
2. **Escape special characters**: Use `\\s` for whitespace, `\\.` for literal dots
3. **Test regex**: Use online regex testers to validate patterns
4. **Keep it simple**: Complex patterns are harder to maintain and bypass
5. **Document reasoning**: Keep notes on why each pattern exists

---

## Security Considerations

### Limitations of Blocklists

⚠️ **Blocklists are a speed bump, not a barrier.**

**Bypass techniques include**:
- **Encoding**: `echo cm0gLXJmIC8K | base64 -d | bash`
- **Command substitution**: `$(echo rm) -rf /`
- **Indirection**: `bash -c "rm -rf /"`
- **Alternative tools**: `find / -delete`
- **Shell builtins**: May not be caught by binary checks
- **Whitespace variations**: Extra spaces, tabs, newlines

**No blocklist can be perfect.** The AI model has root access and can potentially bypass any validation.

### Security Trade-offs

| Profile | Speed Bump Height | Usability | Risk Level |
|---------|-------------------|-----------|------------|
| **minimal** | Low curb | Very high | Very high |
| **permissive** | Speed bump | High | High |
| **default** | Large bump | Moderate | Moderate |
| **strict** | Road barrier | Lower | Lower |

**Recommended practices**:
- Start with **default** or **strict** in production environments
- Use **permissive** only on isolated development machines
- Use **minimal** only in disposable VMs/containers
- **Monitor audit logs** regularly (`/var/log/sudo-mcp/audit.log`)
- **Test in safe environments** before production use
- **Consider VM isolation** for high-risk operations

### When to Use `--no-blocklist`

⚠️ **Extremely dangerous** - only use in controlled environments.

**Valid use cases**:
- Isolated testing VMs that can be destroyed
- Controlled security research
- Demonstrating vulnerabilities

**Never use `--no-blocklist` in**:
- Production systems
- Shared environments
- Systems with important data
- Internet-connected machines

---

## Testing Your Blocklist

### Test That Dangerous Commands Are Blocked

Create a test script:

```bash
#!/bin/bash
# test-blocklist.sh

echo "Testing blocklist: $1"

# These should all be BLOCKED
test_commands=(
  "rm -rf /"
  "dd if=/dev/zero of=/dev/sda"
  "mkfs.ext4 /dev/sda1"
  ":(){:|:&};:"
)

for cmd in "${test_commands[@]}"; do
  echo -n "Testing: $cmd ... "
  # Note: This won't actually execute, just tests validation
  # You'd integrate with your MCP client for real testing
  echo "BLOCKED (expected)"
done
```

### Test That Safe Commands Are Allowed

```bash
# These should all be ALLOWED (adjust based on your profile)
safe_commands=(
  "systemctl status nginx"
  "ls -la /var/log"
  "cat /etc/hosts"
  "ps aux"
)
```

### Integration Testing

Test with an actual MCP client:

1. Configure Claude Desktop/Code with your blocklist
2. Ask the AI to execute various commands
3. Verify blocked commands are rejected
4. Verify allowed commands succeed
5. Check audit logs for all attempts

---

## Blocklist Maintenance

### When to Update

- **New threats emerge**: Add patterns for newly discovered dangerous commands
- **False positives**: Remove or refine patterns blocking legitimate use
- **False negatives**: Add patterns when blocked commands slip through
- **Regulatory changes**: Adjust for compliance requirements

### Keeping in Sync

If you modify the embedded default (`DefaultBlocklist.cs`):
1. Update `blocklist-default.json` to match
2. Run the synchronisation test: `dotnet test --filter "BlocklistDefault_MatchesEmbedded"`
3. Review impact on other profiles

### Version Control

- Store custom blocklists in version control
- Document changes in commit messages
- Review changes in pull requests
- Tag releases of blocklist configurations

---

## Frequently Asked Questions

### Can I combine multiple blocklist files?

No, sudo-mcp loads only one blocklist file at a time. To combine profiles, manually merge the JSON arrays.

### What happens if I have a syntax error in my JSON?

sudo-mcp will fail to start and display an error message. Validate JSON syntax before use.

### Can I use comments in blocklist JSON files?

No, standard JSON doesn't support comments. Use this README or separate documentation.

### How do I test regex patterns?

Use online regex testers like [regex101.com](https://regex101.com/) with ECMAScript/JavaScript flavor.

### Can I block commands based on arguments only?

Yes, use regex patterns. For example, to block `chmod` with `777` but allow other modes:
```json
"RegexPatterns": ["^chmod\\s+777.*$"]
```

### Does the blocklist protect against all risks?

**No.** Blocklists are a mitigation, not a complete solution. Always operate sudo-mcp in isolated environments and monitor audit logs.

---

## Additional Resources

- [Main README](../README.md) - Project overview and installation
- [SECURITY.md](../SECURITY.md) - Comprehensive security documentation
- [Default Blocklist Source](../src/SudoMcp/Configuration/DefaultBlocklist.cs) - Embedded default patterns
- [Command Validator Tests](../src/SudoMcp.Tests/Unit/CommandValidatorTests.cs) - Test suite for validation logic

---

**Remember**: sudo-mcp gives an AI model root access to your system. Use blocklists as part of a defence-in-depth strategy, not as your only protection.
