using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Interface for executing commands with elevated privileges.
/// </summary>
public interface IPrivilegedExecutor
{
    /// <summary>
    /// Executes a command with elevated privileges and captures output.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutSeconds">Timeout in seconds (overrides default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command execution result with stdout, stderr, and exit code.</returns>
    Task<CommandExecutionResult> ExecuteAsync(
        string command,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);
}
