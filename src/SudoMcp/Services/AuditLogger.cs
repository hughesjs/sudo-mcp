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
    private static readonly SemaphoreSlim _fileLock = new(1, 1);

    public AuditLogger(ILogger<AuditLogger> logger, ExecutionOptions options)
    {
        _logger = logger;
        _options = options;

        // Ensure audit log directory exists
        string? logDirectory = Path.GetDirectoryName(_options.AuditLogPath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create audit log directory: {Directory}", logDirectory);
            }
        }
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
        try
        {
            string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Use semaphore to ensure thread-safe file writes
            await _fileLock.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(
                    _options.AuditLogPath,
                    json + Environment.NewLine);
            }
            finally
            {
                _fileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to audit log: {Path}", _options.AuditLogPath);
        }
    }
}
