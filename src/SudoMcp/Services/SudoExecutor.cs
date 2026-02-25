using System.Diagnostics;
using System.Runtime.Versioning;
using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Executes commands with elevated privileges using sudo -A with an osascript askpass helper (macOS).
/// </summary>
[SupportedOSPlatform("macos")]
public class SudoExecutor : IPrivilegedExecutor
{
    private readonly ExecutionOptions _options;
    private string? _askpassPath;
    private readonly object _askpassLock = new();

    public SudoExecutor(ExecutionOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task<CommandExecutionResult> ExecuteAsync(
        string command,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        string askpass = EnsureAskpassScript();

        ProcessStartInfo startInfo = new()
        {
            FileName = "sudo",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["SUDO_ASKPASS"] = askpass;
        startInfo.ArgumentList.Add("-A");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("bash");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);

        try
        {
            using Process process = new();
            process.StartInfo = startInfo;
            process.Start();

            Task<string> stdoutTask = Task.Run(async () =>
                await process.StandardOutput.ReadToEndAsync(cancellationToken), cancellationToken);
            Task<string> stderrTask = Task.Run(async () =>
                await process.StandardError.ReadToEndAsync(cancellationToken), cancellationToken);

            process.StandardInput.Close();

            int effectiveTimeout = timeoutSeconds ?? _options.TimeoutSeconds;
            TimeSpan timeout = TimeSpan.FromSeconds(effectiveTimeout);
            Task exitTask = process.WaitForExitAsync(cancellationToken);
            Task timeoutTask = Task.Delay(timeout, cancellationToken);

            Task completedTask = await Task.WhenAny(exitTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return new()
                {
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = $"Command execution timed out after {effectiveTimeout} seconds"
                };
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;
            int exitCode = process.ExitCode;

            return new()
            {
                Success = exitCode == 0,
                StandardOutput = stdout,
                StandardError = stderr,
                ExitCode = exitCode,
                ErrorMessage = exitCode switch
                {
                    0 => null,
                    1 when stderr.Contains("incorrect password", StringComparison.OrdinalIgnoreCase)
                        => "Authorisation failed - incorrect password",
                    1 when stderr.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
                        => "Authorisation cancelled - user dismissed authentication dialogue",
                    _ => $"Command failed with exit code {exitCode}"
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new()
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = "Command execution was cancelled"
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = $"Failed to execute command: {ex.Message}",
                StandardError = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Creates the askpass script on first use and returns its path.
    /// The script uses osascript to display a native macOS password dialogue.
    /// </summary>
    private string EnsureAskpassScript()
    {
        if (_askpassPath is not null)
            return _askpassPath;

        lock (_askpassLock)
        {
            if (_askpassPath is not null)
                return _askpassPath;

            string tempDir = Path.Combine(Path.GetTempPath(), $"sudo-mcp-{Environment.ProcessId}");
            Directory.CreateDirectory(tempDir);

            string scriptPath = Path.Combine(tempDir, "askpass.sh");
            File.WriteAllText(scriptPath, """
                #!/bin/bash
                exec osascript -e 'display dialog "sudo-mcp requires administrator privileges to execute a command." default answer "" with hidden answer with title "sudo-mcp" with icon caution buttons {"Cancel", "OK"} default button "OK"' -e 'text returned of result'
                """);

            // Set user-only executable permissions (0700)
            File.SetUnixFileMode(scriptPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            _askpassPath = scriptPath;
            return scriptPath;
        }
    }
}
