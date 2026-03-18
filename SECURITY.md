# Security

## This Tool is Not Secure

**sudo-mcp gives an AI model root access to your system.** There is no way to make this safe. The blocklist is a speed bump, not a barrier.

By using this tool, you accept that:

- Your system could be destroyed
- Your data could be deleted or exfiltrated
- Malware could be installed
- The AI could do anything root can do

**If you don't understand and accept these risks, don't use this tool.**

---

## The Blocklist Will Be Bypassed

The blocklist cannot prevent a determined or creative attacker. Common bypass techniques include:

```bash
# Command substitution
$(echo "rm -rf /")

# Variable expansion
cmd="rm -rf /"; $cmd

# Absolute paths
/bin/rm -rf /

# Flag reordering
rm -r -f /

# Long-form flags
rm --recursive --force /

# Writing and executing scripts
echo "rm -rf /" > /tmp/x.sh && bash /tmp/x.sh

# Base64 encoding
echo "cm0gLXJmIC8=" | base64 -d | bash
```

The blocklist uses regex patterns which:

- Can have edge cases
- Cannot anticipate all obfuscation techniques
- Cannot prevent indirect execution
- Cannot stop multi-step attacks

---

## `--no-blocklist` Mode

Using `--no-blocklist` removes all command validation. The AI has completely unrestricted root access.

Only use this in disposable environments where you don't care what happens.

---

## macOS Security Notes

On macOS, sudo-mcp uses `sudo -A` with an osascript-based askpass helper to prompt for authentication via a native macOS password dialogue. This is the macOS analogue of polkit's graphical auth dialogue on Linux.

**Key differences from Linux:**

- **Credential caching**: macOS `sudo` caches credentials for a configurable timeout period (default 5 minutes). During this window, subsequent commands execute without prompting. This means the user may only see one password dialogue per session.
- **No polkit**: macOS has no polkit/pkexec. Privilege escalation is handled entirely through `sudo`.
- **SIP protection**: macOS System Integrity Protection prevents modification of `/usr/bin/` and other protected paths. The binary installs to `/usr/local/bin/` instead.
- **Askpass script**: A temporary shell script is created at runtime that invokes `osascript` to display the password dialogue. The script is stored in a temp directory with user-only permissions (0700) and persists for the process lifetime.
- **Graphical session required**: The osascript dialogue requires a graphical session. Running sudo-mcp via SSH or in a headless environment will fail to display the prompt.

---

## Windows Security Notes

On Windows, sudo-mcp uses the built-in `sudo` command introduced in Windows 11 24H2 to execute commands via `sudo cmd /c <command>`. A UAC (User Account Control) elevation prompt appears for each invocation.

**Key considerations:**

- **New feature, less battle-tested**: Windows `sudo` is a relatively recent addition (24H2+) and has not undergone the decades of hardening that Unix sudo and polkit have. Expect rough edges.
- **UAC auto-approval risk**: If an administrator account has UAC disabled (a known Windows misconfiguration), elevation prompts will not appear and commands will execute silently with full privileges. Ensure UAC is enabled.
- **`cmd /c` shell**: Commands are executed through `cmd /c`, so blocklist patterns must account for Windows command syntax (e.g., `format`, `diskpart`, `reg`, `bcdedit`). Users can still invoke PowerShell from within `cmd /c`.
- **No fine-grained policy control**: Windows has no equivalent of polkit's per-command policy rules. UAC is all-or-nothing per elevation request — you cannot selectively allow certain commands while denying others at the OS level.
- **Credential caching**: UAC may remember elevation consent for the duration of a session, meaning subsequent commands may not prompt the user again.

---

## No Security Support

This tool is intentionally dangerous by design. If you find a blocklist bypass, that's expected. There is no security contact because there is no expectation of security.

Open a GitHub issue if you want, but don't expect it to be treated as a vulnerability.

---

## Liability

This software is provided under the MIT License with **NO WARRANTY**.

You are solely responsible for any damage, data loss, or security breaches that occur from using this tool.