namespace SudoMcp.Models;

/// <summary>
/// Result of a command execution attempt.
/// </summary>
public class CommandExecutionResult
{
    /// <summary>
    /// Indicates whether the command executed successfully (exit code 0).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Standard output from the command.
    /// </summary>
    public string? StandardOutput { get; set; }

    /// <summary>
    /// Standard error from the command.
    /// </summary>
    public string? StandardError { get; set; }

    /// <summary>
    /// Exit code from the command execution.
    /// 0 = success
    /// 126 = user cancelled pkexec authentication
    /// 127 = authorisation failed
    /// Other = command-specific failure
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Error message if the command failed or was blocked.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
