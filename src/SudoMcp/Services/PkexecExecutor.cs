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
        // Use ArgumentList to pass arguments directly without shell parsing
        // This avoids complex escaping issues with ProcessStartInfo.Arguments
        ProcessStartInfo startInfo = new()
        {
            FileName = "pkexec",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Each argument passed separately - no shell escaping needed
        startInfo.ArgumentList.Add("sudo");
        startInfo.ArgumentList.Add("-S");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("bash");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);  // Command passed directly to bash
        
        startInfo.Environment["PKEXEC_UID"] = Environment.GetEnvironmentVariable("UID") ?? "0";

        try
        {
            using Process process = new();
            process.StartInfo = startInfo;
            process.Start();

            Task<string> stdoutTask = Task.Run(async () =>
                await process.StandardOutput.ReadToEndAsync(cancellationToken), cancellationToken);
            Task<string> stderrTask = Task.Run(async () =>
                await process.StandardError.ReadToEndAsync(cancellationToken), cancellationToken);

            // Close stdin since pkexec doesn't need it for authentication (uses polkit agent)
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
                    126 => "Authorisation cancelled - user dismissed authentication dialogue",
                    127 => "Authorisation failed - user not authorised to execute this command",
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
}
