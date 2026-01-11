# Security Documentation

## ⚠️ Critical Security Notice

**This tool is inherently dangerous**. It provides an AI model with the ability to execute arbitrary commands with root privileges on your system. No amount of safeguards can make this completely safe.

**By using this tool, you accept full responsibility for any damage, data loss, or security breaches that may occur.**

---

## Threat Model

### Attack Vectors

#### 1. **Blocklist Bypass**
**Risk Level**: High

**Description**: Attackers (or the AI itself) may find ways to circumvent the command blocklist through:
- Command obfuscation (e.g., encoding, variable substitution)
- Shell features (aliases, functions, command substitution)
- Indirect execution (writing scripts and executing them)
- Path manipulation

**Example Bypass Attempts**:
```bash
# Instead of: rm -rf /
$(echo "rm -rf /")                    # Command substitution
/bin/rm -rf /                          # Absolute path
rm -r -f /                             # Reordered flags
rm --recursive --force /               # Long form flags
```

**Mitigation**:
- Blocklist includes regex patterns for common bypass attempts
- Consider switching to allowlist mode for production
- Regular review and updates of blocklist patterns
- Monitor audit logs for suspicious patterns

#### 2. **Prompt Injection / Jailbreaking**
**Risk Level**: Critical

**Description**: Malicious prompts could instruct the AI to execute dangerous commands, potentially bypassing its safety guidelines.

**Example Scenarios**:
- User-provided data containing malicious instructions
- Chain-of-thought reasoning that accidentally constructs dangerous commands
- Multi-step attacks where the AI is tricked into building dangerous commands progressively

**Mitigation**:
- Use `--no-blocklist` only in isolated, disposable environments
- Monitor audit logs in real-time
- Implement external monitoring/alerting for critical operations
- Consider running in a sandboxed VM

#### 3. **Privilege Escalation Beyond Sudo**
**Risk Level**: Medium

**Description**: Commands executed with sudo have full root access and could:
- Modify polkit policies to grant broader access
- Install malware or backdoors
- Create new administrative users
- Disable security controls

**Mitigation**:
- Use dedicated service accounts with limited sudo access
- Implement SELinux/AppArmor policies
- Monitor for unexpected privilege changes
- Regular security audits

#### 4. **Data Exfiltration**
**Risk Level**: High

**Description**: Commands could be used to:
- Read sensitive files (/etc/shadow, SSH keys, application secrets)
- Exfiltrate data over the network
- Access databases or other services

**Mitigation**:
- Run in isolated network environment
- Implement egress filtering
- Use file integrity monitoring
- Encrypt sensitive data at rest

#### 5. **Denial of Service**
**Risk Level**: Medium

**Description**: Commands could:
- Fill disk space
- Consume all CPU/memory resources
- Kill critical services
- Corrupt system files

**Mitigation**:
- Resource limits (cgroups, ulimits)
- Timeout enforcement (default 15s)
- Monitor system resources
- Regular backups

#### 6. **Audit Log Tampering**
**Risk Level**: Medium

**Description**: With sudo access, an attacker could:
- Delete or modify audit logs
- Disable logging
- Fill audit log directory to cause DoS

**Mitigation**:
- Stream logs to remote syslog server
- Use immutable file attributes (`chattr +i`)
- Separate audit log storage
- Log rotation and retention policies

---

## `--no-blocklist` Mode

### When to Use

**Acceptable Use Cases**:
- Isolated test environments (VMs, containers)
- Development/debugging in disposable systems
- Automated testing frameworks
- Personal systems where you fully understand the risks

**NEVER Use For**:
- Production servers
- Systems with sensitive data
- Multi-user environments
- Publicly accessible systems

### Risks

When `--no-blocklist` is enabled:
- **NO command validation** occurs
- AI has **unrestricted root access**
- Single malicious or erroneous prompt could cause catastrophic damage
- **No safety net whatsoever**

### Recommendations

If you must use `--no-blocklist`:

1. **Isolated Environment**: Run in a dedicated VM or container
2. **Snapshots**: Create system snapshots before use
3. **Network Isolation**: Disconnect from production networks
4. **Data Protection**: Ensure no irreplaceable data exists on the system
5. **Monitoring**: Implement aggressive real-time monitoring
6. **Time Limits**: Use for minimal time necessary

---

## Blocklist Configuration

### Understanding the Blocklist

The default blocklist (`Configuration/BlockedCommands.json`) uses three strategies:

#### 1. Exact Matches
Blocks specific dangerous command strings exactly as written.

**Pros**: Fast, no false positives
**Cons**: Easy to bypass with minor modifications

#### 2. Regex Patterns
Blocks classes of dangerous operations using regular expressions.

**Pros**: Catches variations and obfuscation attempts
**Cons**: Complex patterns may have edge cases, potential false positives

#### 3. Binary Blocklist
Blocks specific executables regardless of arguments.

**Pros**: Prevents entire classes of tools
**Cons**: May block legitimate use cases

### Customising Your Blocklist

#### Conservative (Recommended for Production)

Create a strict blocklist that only allows known-safe operations:

```json
{
  "BlockedCommands": {
    "ExactMatches": [
      "rm -rf /",
      "mkfs",
      "dd",
      "fdisk",
      "parted"
    ],
    "RegexPatterns": [
      "^rm\\s+.*",
      "^dd\\s+.*",
      "^mkfs.*",
      ".*>/dev/(sd|nvme|vd).*",
      ".*\\|.*sh.*",
      ".*&&.*rm.*",
      ".*\\$\\(.*\\).*"
    ],
    "BlockedBinaries": [
      "rm",
      "mkfs.ext4",
      "mkfs.xfs",
      "dd",
      "shred",
      "fdisk",
      "parted",
      "wipefs"
    ]
  }
}
```

#### Permissive (Development/Testing)

Allow more operations but still block catastrophic commands:

```json
{
  "BlockedCommands": {
    "ExactMatches": [
      "rm -rf /",
      "rm -rf /*"
    ],
    "RegexPatterns": [
      "^rm\\s+(-rf|-fr)\\s+/\\s*$",
      "^dd.*of=/dev/(sd|nvme|vd)[a-z]\\s*$",
      "^mkfs\\."
    ],
    "BlockedBinaries": [
      "mkfs.ext4",
      "mkfs.xfs",
      "wipefs"
    ]
  }
}
```

### Adding Custom Patterns

**To block a specific command**:
```json
"ExactMatches": [
  "your-dangerous-command-here"
]
```

**To block a pattern**:
```json
"RegexPatterns": [
  "^your-pattern-here$"
]
```

**Testing your regex patterns**:
```bash
# Test if your pattern matches correctly
echo "rm -rf /" | grep -P "^rm\\s+(-rf|-fr)\\s+/\\s*$"
```

---

## Recommended Deployment Practices

### 1. Dedicated Service Account

Create a limited service account:

```bash
# Create service account
sudo useradd -r -s /bin/bash -m sudo-mcp-service

# Create limited sudoers entry
echo "sudo-mcp-service ALL=(ALL) NOPASSWD: /usr/bin/systemctl, /usr/bin/journalctl" | \
  sudo tee /etc/sudoers.d/sudo-mcp

# Run as service account
sudo -u sudo-mcp-service dotnet run --project /opt/sudo-mcp/SudoMcp.csproj
```

### 2. Containerisation

Run in Docker with limited capabilities:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY ./publish .
RUN apt-get update && apt-get install -y sudo polkit-1

# Limited capabilities
ENTRYPOINT ["dotnet", "SudoMcp.dll"]
```

### 3. Virtual Machine Isolation

- Run sudo-mcp in a dedicated VM
- Use snapshots before risky operations
- Limit network access between VM and host
- Regular VM cloning for testing

### 4. Audit Log Management

#### Remote Logging

Stream logs to remote syslog server:

```bash
# Install rsyslog
sudo apt-get install rsyslog

# Configure forwarding
echo "*.* @@remote-log-server:514" | sudo tee -a /etc/rsyslog.conf
sudo systemctl restart rsyslog
```

#### Log Rotation

```bash
# Create logrotate config
sudo tee /etc/logrotate.d/sudo-mcp << EOF
/var/log/sudo-mcp/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 0640 sudo-mcp-service sudo-mcp-service
}
EOF
```

#### Log Analysis

Parse audit logs for suspicious activity:

```bash
# Find all denied commands
jq 'select(.EventType == "CommandDenied")' /var/log/sudo-mcp/audit.log

# Find failed executions
jq 'select(.Success == false and .EventType == "CommandExecuted")' \
  /var/log/sudo-mcp/audit.log

# Commands by user
jq -r '.User' /var/log/sudo-mcp/audit.log | sort | uniq -c
```

### 5. Monitoring & Alerting

#### Real-time Monitoring

```bash
# Watch audit log in real-time
tail -f /var/log/sudo-mcp/audit.log | jq '.'

# Alert on denied commands
tail -f /var/log/sudo-mcp/audit.log | \
  jq 'select(.EventType == "CommandDenied")' | \
  while read line; do
    notify-send "Command Denied" "$line"
  done
```

#### System Monitoring

Monitor for:
- Unexpected process creation
- Network connections from privileged processes
- File system modifications in sensitive directories
- Privilege escalation attempts

Tools: `auditd`, `osquery`, `OSSEC`, `Wazuh`

---

## Allowlist Approach (Maximum Security)

For production environments, consider switching to an allowlist:

```json
{
  "AllowedCommands": {
    "ExactMatches": [
      "systemctl status nginx",
      "systemctl restart nginx",
      "journalctl -u nginx -n 100"
    ],
    "RegexPatterns": [
      "^systemctl\\s+(status|restart|reload)\\s+nginx$",
      "^journalctl\\s+-u\\s+nginx\\s+-n\\s+\\d+$"
    ]
  }
}
```

**Implementation**: Modify `CommandValidator.cs` to invert the logic - deny by default, allow only matching patterns.

---

## Incident Response

### If a Malicious Command is Executed

1. **Immediate Actions**:
   ```bash
   # Stop the MCP server
   sudo pkill -9 -f SudoMcp

   # Review what was executed
   jq 'select(.Timestamp > "2026-01-11T18:00:00Z")' /var/log/sudo-mcp/audit.log

   # Check for new users/groups
   sudo tail /etc/passwd /etc/group

   # Check for new sudo permissions
   sudo ls -la /etc/sudoers.d/
   ```

2. **System Assessment**:
   ```bash
   # Check for modified system files
   sudo aide --check

   # Review running processes
   ps aux | grep -E "(sudo|root)"

   # Check network connections
   sudo netstat -tulpn

   # Review recent file modifications
   find / -type f -mtime -1 2>/dev/null
   ```

3. **Recovery**:
   - Restore from known-good backup
   - Review and update blocklist
   - Implement additional monitoring
   - Consider switching to allowlist mode

---

## Security Best Practices Checklist

- [ ] Running in isolated environment (VM/container)
- [ ] Using custom blocklist appropriate for your use case
- [ ] Audit logs configured and monitored
- [ ] Regular backups of critical data
- [ ] Network isolation from sensitive systems
- [ ] Resource limits configured (timeout, memory, CPU)
- [ ] Incident response plan documented
- [ ] Security monitoring tools deployed
- [ ] Regular security reviews scheduled
- [ ] Team trained on risks and procedures

---

## Known Limitations

### What the Blocklist CANNOT Prevent

1. **Sophisticated Obfuscation**: Advanced encoding or multi-step command construction
2. **Application-Level Attacks**: Exploiting vulnerabilities in allowed applications
3. **Timing Attacks**: Race conditions or time-of-check-time-of-use issues
4. **Social Engineering**: The AI being manipulated through clever prompting
5. **Zero-Day Exploits**: Unknown vulnerabilities in system tools

### Fundamental Risks

- **AI Unpredictability**: Large language models can behave unexpectedly
- **Context Limitations**: The AI may not fully understand command implications
- **No Perfect Defence**: Every security measure has potential bypasses
- **Trust Requirement**: You must trust the AI not to intentionally cause harm

---

## Reporting Security Issues

If you discover a security vulnerability or bypass technique:

1. **Do NOT** open a public GitHub issue
2. Email: [james@example.com](mailto:james@example.com) with subject "sudo-mcp Security Issue"
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested mitigation

---

## License and Liability

This software is provided under the MIT License. **There is NO WARRANTY**. The authors are not liable for any damages resulting from use of this software.

By using sudo-mcp, you explicitly acknowledge and accept all risks outlined in this document.
