using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SudoMcp.Services;

namespace SudoMcp.Tools;

/// <summary>
/// MCP tool for executing commands with elevated privileges.
/// </summary>
[McpServerToolType]
public static class SudoExecutionTool
{
    /// <summary>
    /// Executes a command with elevated privileges using sudo.
    /// Commands are validated against a blocklist before execution (if blocklist is enabled).
    /// </summary>
    /// <param name="command">The command to execute with sudo privileges.</param>
    /// <param name="timeoutSeconds">Command execution timeout in seconds (default: 15).</param>
    /// <param name="validator">Command validator service.</param>
    /// <param name="executor">Pkexec executor service.</param>
    /// <param name="auditLogger">Audit logger service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing execution results (stdout, stderr, exit code).</returns>
    [McpServerTool]
    [Description("Executes a command with elevated privileges using sudo. Commands are validated against a blocklist before execution. Returns stdout, stderr, and exit code.")]
    public static async Task<string> ExecuteSudoCommand(
        [Description("The command to execute with sudo privileges")] string command,
        [Description("Command execution timeout in seconds (default: 15)")] int timeoutSeconds = 15,
        CommandValidator validator = null!,
        PkexecExecutor executor = null!,
        AuditLogger auditLogger = null!,
        CancellationToken cancellationToken = default)
    {
        // Validate the command
        var validationResult = validator.ValidateCommand(command);

        if (!validationResult.IsValid)
        {
            // Log denied command
            await auditLogger.LogDeniedCommand(command, validationResult.Reason ?? "Unknown reason");

            // Return error response
            var deniedResult = new
            {
                Success = false,
                ErrorMessage = $"Command blocked: {validationResult.Reason}",
                ExitCode = -1,
                StandardOutput = (string?)null,
                StandardError = (string?)null
            };

            return JsonSerializer.Serialize(deniedResult, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // Execute the command with the specified timeout
        var result = await executor.ExecuteWithPkexec(command, timeoutSeconds, cancellationToken);

        // Log the execution
        await auditLogger.LogExecutedCommand(command, result);

        // Return the result as JSON
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
