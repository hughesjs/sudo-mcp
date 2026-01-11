namespace SudoMcp.Models;

/// <summary>
/// Runtime configuration options for command execution.
/// </summary>
public class ExecutionOptions
{
    /// <summary>
    /// Path to the audit log file where all command execution attempts are recorded.
    /// </summary>
    public required string AuditLogPath { get; init; }

    /// <summary>
    /// Command execution timeout in seconds.
    /// </summary>
    public required int TimeoutSeconds { get; init; }
}
