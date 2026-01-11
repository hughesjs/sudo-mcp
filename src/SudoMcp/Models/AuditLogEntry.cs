namespace SudoMcp.Models;

/// <summary>
/// Audit log entry for command execution attempts.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Timestamp of the command execution attempt (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Type of event: "CommandExecuted" or "CommandDenied".
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// The command that was executed or denied.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// User who initiated the command.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Exit code of the command (for executed commands).
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Whether the command succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (if command failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Reason for denial (for denied commands).
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Standard output from the command (for executed commands).
    /// </summary>
    public string? StandardOutput { get; set; }

    /// <summary>
    /// Standard error from the command (for executed commands).
    /// </summary>
    public string? StandardError { get; set; }
}
