using System.Diagnostics;
using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Executes commands with elevated privileges using pkexec and sudo.
/// </summary>
public class PkexecExecutor
{
    private readonly ExecutionOptions _options;

    public PkexecExecutor(ExecutionOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Executes a command with pkexec/sudo and captures output.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (overrides default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command execution result with stdout, stderr, and exit code.</returns>
    public async Task<CommandExecutionResult> ExecuteWithPkexec(
        string command,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pkexec",
            Arguments = $"sudo -S -- {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add environment variables that might be useful
        startInfo.Environment["PKEXEC_UID"] = Environment.GetEnvironmentVariable("UID") ?? "0";

        try
        {
            using var process = new Process { StartInfo = startInfo };

            // Create tasks to read stdout and stderr asynchronously
            // This prevents deadlock when both streams fill their buffers
            Task<string> stdoutTask;
            Task<string> stderrTask;

            process.Start();

            // Start reading stdout and stderr immediately and asynchronously
            stdoutTask = Task.Run(async () =>
                await process.StandardOutput.ReadToEndAsync(cancellationToken), cancellationToken);
            stderrTask = Task.Run(async () =>
                await process.StandardError.ReadToEndAsync(cancellationToken), cancellationToken);

            // Close stdin since pkexec doesn't need it for authentication (uses polkit agent)
            process.StandardInput.Close();

            // Wait for process to exit with timeout
            var effectiveTimeout = timeoutSeconds ?? _options.TimeoutSeconds;
            var timeout = TimeSpan.FromSeconds(effectiveTimeout);
            var exitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);

            var completedTask = await Task.WhenAny(exitTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout occurred
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Process may have already exited
                }

                return new CommandExecutionResult
                {
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = $"Command execution timed out after {effectiveTimeout} seconds"
                };
            }

            // Wait for stdout and stderr tasks to complete
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.ExitCode;

            return new CommandExecutionResult
            {
                Success = exitCode == 0,
                StandardOutput = stdout,
                StandardError = stderr,
                ExitCode = exitCode,
                ErrorMessage = exitCode switch
                {
                    0 => null,
                    126 => "Authorisation cancelled - user dismissed authentication dialogue",
                    127 => "Authorisation failed - user not authorised to execute this command",
                    _ => $"Command failed with exit code {exitCode}"
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new CommandExecutionResult
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = "Command execution was cancelled"
            };
        }
        catch (Exception ex)
        {
            return new CommandExecutionResult
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = $"Failed to execute command: {ex.Message}",
                StandardError = ex.ToString()
            };
        }
    }
}
