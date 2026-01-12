using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SudoMcp.Models;
using SudoMcp.Services;

namespace SudoMcp.Tools;

/// <summary>
/// MCP tool for executing commands with elevated privileges.
/// </summary>
[McpServerToolType]
public class SudoExecutionTool
{
    private readonly CommandValidator _validator;
    private readonly PkexecExecutor _executor;
    private readonly AuditLogger _auditLogger;

    /// <summary>
    /// Initialises a new instance of the <see cref="SudoExecutionTool"/> class.
    /// </summary>
    /// <param name="validator">Command validator service.</param>
    /// <param name="executor">Pkexec executor service.</param>
    /// <param name="auditLogger">Audit logger service.</param>
    public SudoExecutionTool(
        CommandValidator validator,
        PkexecExecutor executor,
        AuditLogger auditLogger)
    {
        _validator = validator;
        _executor = executor;
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Executes a command with elevated privileges using sudo.
    /// Commands are validated against a blocklist before execution (if blocklist is enabled).
    /// </summary>
    /// <param name="command">The command to execute with sudo privileges.</param>
    /// <param name="timeoutSeconds">Command execution timeout in seconds (default: 15).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing execution results (stdout, stderr, exit code).</returns>
    [McpServerTool]
    [Description("Executes a command with elevated privileges using sudo. Commands are validated against a blocklist before execution. Returns stdout, stderr, and exit code.")]
    public async Task<string> ExecuteSudoCommand(
        [Description("The command to execute with sudo privileges")] string command,
        [Description("Command execution timeout in seconds (default: 15)")] int timeoutSeconds = 15,
        CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = _validator.ValidateCommand(command);

        if (!validationResult.IsValid)
        {
            await _auditLogger.LogDeniedCommand(command, validationResult.Reason ?? "Unknown reason");

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

        CommandExecutionResult result = await _executor.ExecuteWithPkexec(command, timeoutSeconds, cancellationToken);

        await _auditLogger.LogExecutedCommand(command, result);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
