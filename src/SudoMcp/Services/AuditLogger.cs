using System.Text.Json;
using Microsoft.Extensions.Logging;
using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Logs all command execution attempts to an audit log file.
/// </summary>
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly ExecutionOptions _options;
    private readonly Lazy<string?> _logDirectory;

    public AuditLogger(ILogger<AuditLogger> logger, ExecutionOptions options)
    {
        _logger = logger;
        _options = options;
        _logDirectory = new(() =>
        {
            string? logDirectory = Path.GetDirectoryName(_options.AuditLogPath);
            if (string.IsNullOrEmpty(logDirectory) || Directory.Exists(logDirectory)) return logDirectory;
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create audit log directory: {Directory}", logDirectory);
            }
            return logDirectory;
        });
    }

    /// <summary>
    /// Logs a successfully executed command.
    /// </summary>
    public async Task LogExecutedCommand(string command, CommandExecutionResult result)
    {
        AuditLogEntry entry = new()
        {
            Timestamp = DateTime.UtcNow,
            EventType = "CommandExecuted",
            Command = command,
            User = Environment.UserName,
            ExitCode = result.ExitCode,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError
        };

        _logger.LogInformation(
            "Sudo command executed: {Command} | Success: {Success} | ExitCode: {ExitCode}",
            command, result.Success, result.ExitCode);

        await WriteAuditEntry(entry);
    }

    /// <summary>
    /// Logs a denied command (blocked by validator).
    /// </summary>
    public async Task LogDeniedCommand(string command, string reason)
    {
        AuditLogEntry entry = new()
        {
            Timestamp = DateTime.UtcNow,
            EventType = "CommandDenied",
            Command = command,
            User = Environment.UserName,
            Success = false,
            DenialReason = reason
        };

        _logger.LogWarning(
            "Sudo command denied: {Command} | Reason: {Reason}",
            command, reason);

        await WriteAuditEntry(entry);
    }

    /// <summary>
    /// Writes an audit entry to the log file.
    /// </summary>
    private async Task WriteAuditEntry(AuditLogEntry entry)
    {
        // Access lazy property to ensure directory exists
        _ = _logDirectory.Value;

        try
        {
            string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            await File.AppendAllTextAsync(
                _options.AuditLogPath,
                json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to audit log: {Path}", _options.AuditLogPath);
        }
    }
}
