using System.Text.Json;
using DotNet.Testcontainers.Containers;

namespace SudoMcp.Tests.Integration;

/// <summary>
/// Test client for communicating with sudo-mcp MCP server via JSON-RPC 2.0
/// </summary>
public sealed class McpTestClient : IAsyncDisposable
{
    private const string BinaryPath = "/usr/bin/sudo-mcp";
    private const string AuditLogPath = "/var/log/sudo-mcp/audit.log";

    private readonly IContainer _container;
    private readonly string _workDir;
    private int _requestId;

    public McpTestClient(IContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _workDir = $"/tmp/mcp-test-{Guid.NewGuid():N}";
        _requestId = 0;
    }

    /// <summary>
    /// Initialises the test client by creating working directory in container
    /// </summary>
    public async Task InitialiseAsync()
    {
        // Create working directory for this client with permissions for testuser
        await _container.ExecAsync(["mkdir", "-p", _workDir]).ConfigureAwait(false);
        await _container.ExecAsync(["chmod", "777", _workDir]).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a JSON-RPC tool call request to the MCP server
    /// </summary>
    /// <param name="toolName">Name of the MCP tool to call</param>
    /// <param name="arguments">Arguments to pass to the tool</param>
    /// <param name="timeout">Timeout for the request</param>
    /// <returns>Parsed JSON-RPC response</returns>
    public async Task<JsonDocument> SendToolCallAsync(
        string toolName,
        object arguments,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        int id = Interlocked.Increment(ref _requestId);

        // Create JSON-RPC request
        string request = JsonRpcHelper.CreateToolCallRequest(id, toolName, arguments);

        return await SendRequestAsync(request, timeout.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a tools/list request to the MCP server
    /// </summary>
    /// <param name="timeout">Timeout for the request</param>
    /// <returns>Parsed JSON-RPC response with available tools</returns>
    public async Task<JsonDocument> ListToolsAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        int id = Interlocked.Increment(ref _requestId);

        string request = JsonRpcHelper.CreateListToolsRequest(id);

        return await SendRequestAsync(request, timeout.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a raw JSON-RPC request to the MCP server
    /// </summary>
    private async Task<JsonDocument> SendRequestAsync(string request, TimeSpan timeout)
    {
        string requestFile = $"{_workDir}/request-{_requestId}.json";
        string responseFile = $"{_workDir}/response-{_requestId}.json";
        string stderrFile = $"{_workDir}/stderr-{_requestId}.log";

        try
        {
            // Write request to file in container
            ExecResult writeResult = await _container.ExecAsync(new[]
            {
                "bash", "-c",
                $"echo '{request.Replace("'", "'\\''")}' > {requestFile}"
            }).ConfigureAwait(false);

            if (writeResult.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to write request file: {writeResult.Stderr}");
            }

            // Execute MCP server with request piped to stdin, capture stdout
            // Write a script to avoid quoting issues with nested shells
            string scriptFile = $"{_workDir}/run-{_requestId}.sh";
            string scriptContent = $@"#!/bin/bash
(cat {requestFile}; sleep 2) | {BinaryPath} --audit-log {AuditLogPath} > {responseFile} 2> {stderrFile}
";
            await _container.ExecAsync(new[]
            {
                "bash", "-c", $"cat > {scriptFile} << 'SCRIPT'\n{scriptContent}SCRIPT"
            }).ConfigureAwait(false);

            await _container.ExecAsync(new[] { "chmod", "+x", scriptFile }).ConfigureAwait(false);

            // Run script as testuser with timeout
            ExecResult execResult = await _container.ExecAsync(new[]
            {
                "su", "-", "testuser", "-c", $"timeout 3s {scriptFile}"
            }).ConfigureAwait(false);

            // Read response file
            ExecResult readResult = await _container.ExecAsync(new[]
            {
                "cat", responseFile
            }).ConfigureAwait(false);

            if (readResult.ExitCode != 0)
            {
                // Try to read stderr for debugging
                ExecResult stderrResult = await _container.ExecAsync(new[]
                {
                    "cat", stderrFile
                }).ConfigureAwait(false);

                throw new InvalidOperationException(
                    $"Failed to read response file. MCP server exit code: {execResult.ExitCode}. " +
                    $"Stderr: {stderrResult.Stdout}");
            }

            string response = readResult.Stdout.Trim();

            if (string.IsNullOrWhiteSpace(response))
            {
                // Read stderr for error details
                ExecResult stderrResult = await _container.ExecAsync(new[]
                {
                    "cat", stderrFile
                }).ConfigureAwait(false);

                throw new InvalidOperationException(
                    $"MCP server returned empty response. Exit code: {execResult.ExitCode}. " +
                    $"Stderr: {stderrResult.Stdout}");
            }

            // Parse JSON-RPC response
            return JsonRpcHelper.ParseResponse(response);
        }
        finally
        {
            // Clean up temporary files (best effort, don't fail if this errors)
            await _container.ExecAsync(new[]
            {
                "rm", "-f", requestFile, responseFile, stderrFile
            }).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up working directory (best effort)
        try
        {
            await _container.ExecAsync(new[] { "rm", "-rf", _workDir })
                .ConfigureAwait(false);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
