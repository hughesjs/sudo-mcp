using System.Text.Json;
using System.Text.Json.Serialization;

namespace SudoMcp.Tests.Integration;

/// <summary>
/// Helper class for constructing and parsing JSON-RPC 2.0 messages for MCP protocol
/// </summary>
public static class JsonRpcHelper
{
    /// <summary>
    /// Creates a JSON-RPC 2.0 tool call request
    /// </summary>
    /// <param name="id">Request ID (for matching responses)</param>
    /// <param name="toolName">Name of the MCP tool to call</param>
    /// <param name="arguments">Arguments object to pass to the tool</param>
    /// <returns>JSON-RPC request as a string</returns>
    public static string CreateToolCallRequest(int id, string toolName, object arguments)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };

        return JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Creates a JSON-RPC 2.0 tools/list request
    /// </summary>
    /// <param name="id">Request ID (for matching responses)</param>
    /// <returns>JSON-RPC request as a string</returns>
    public static string CreateListToolsRequest(int id)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/list"
        };

        return JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Creates a JSON-RPC 2.0 initialize request
    /// </summary>
    /// <param name="id">Request ID (for matching responses)</param>
    /// <returns>JSON-RPC request as a string</returns>
    public static string CreateInitializeRequest(int id)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "sudo-mcp-test-client",
                    version = "1.0.0"
                }
            }
        };

        return JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Parses a JSON-RPC 2.0 response
    /// </summary>
    /// <param name="json">JSON string to parse</param>
    /// <returns>Parsed JsonDocument</returns>
    public static JsonDocument ParseResponse(string json)
    {
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Checks if a JSON-RPC response contains an error
    /// </summary>
    /// <param name="response">Parsed JSON response</param>
    /// <returns>True if response contains an error</returns>
    public static bool IsError(JsonDocument response)
    {
        return response.RootElement.TryGetProperty("error", out _);
    }

    /// <summary>
    /// Extracts error message from JSON-RPC error response
    /// </summary>
    /// <param name="response">Parsed JSON response</param>
    /// <returns>Error message string</returns>
    public static string? GetErrorMessage(JsonDocument response)
    {
        if (response.RootElement.TryGetProperty("error", out JsonElement error))
        {
            if (error.TryGetProperty("message", out JsonElement message))
            {
                return message.GetString();
            }
        }
        return null;
    }
}
