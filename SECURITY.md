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

## No Security Support

This tool is intentionally dangerous by design. If you find a blocklist bypass, that's expected. There is no security contact because there is no expectation of security.

Open a GitHub issue if you want, but don't expect it to be treated as a vulnerability.

---

## Liability

This software is provided under the MIT License with **NO WARRANTY**.

You are solely responsible for any damage, data loss, or security breaches that occur from using this tool.