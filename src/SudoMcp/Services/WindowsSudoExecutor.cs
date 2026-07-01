using System.Diagnostics;
using System.Runtime.Versioning;
using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Executes commands with elevated privileges using the Windows 11 built-in sudo command.
/// Requires Windows 11 24H2 or later with sudo enabled in Developer Settings.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsSudoExecutor : IPrivilegedExecutor
{
    private readonly ExecutionOptions _options;

    public WindowsSudoExecutor(ExecutionOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task<CommandExecutionResult> ExecuteAsync(
        string command,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "sudo",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("cmd");
        startInfo.ArgumentList.Add("/c");
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
                    _ when stderr.Contains("is not recognized", StringComparison.OrdinalIgnoreCase)
                        => "sudo is not available. Windows 11 24H2+ required with sudo enabled in Developer Settings.",
                    _ when stderr.Contains("access is denied", StringComparison.OrdinalIgnoreCase)
                        => "Authorisation failed - access denied",
                    _ when stderr.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
                        => "Authorisation cancelled - user dismissed elevation prompt",
                    _ => $"Command failed with exit code {exitCode}"
                }
            };
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            // ERROR_FILE_NOT_FOUND — sudo binary not found
            return new()
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = "sudo command not found. Windows 11 24H2+ required with sudo enabled in Settings > System > For Developers.",
                StandardError = ex.Message
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
}
