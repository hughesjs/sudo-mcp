using System.Text.Json;
using FluentAssertions;
using SudoMcp.Models;
using Xunit;

namespace SudoMcp.Tests.Integration;

/// <summary>
/// Integration tests that validate the full MCP protocol end-to-end
/// These tests actually communicate with sudo-mcp via JSON-RPC 2.0
/// </summary>
[Trait("Category", "Integration")]
public sealed class McpProtocolTests : IClassFixture<SudoMcpContainerFixture>
{
    private readonly SudoMcpContainerFixture _fixture;

    public McpProtocolTests(SudoMcpContainerFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    #region Protocol Basics

    [Fact]
    public async Task ToolDiscovery_ListTools_Returnsexecute_sudo_command()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.ListToolsAsync();

        // Assert
        response.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        response.RootElement.TryGetProperty("result", out JsonElement result).Should().BeTrue();

        result.TryGetProperty("tools", out JsonElement tools).Should().BeTrue();
        List<JsonElement> toolsArray = tools.EnumerateArray().ToList();
        toolsArray.Should().NotBeEmpty();

        JsonElement sudoTool = toolsArray
            .FirstOrDefault(t => t.GetProperty("name").GetString() == "execute_sudo_command");
        sudoTool.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task ToolCall_ValidJsonRpc_ReturnsValidResponse()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "echo 'test'",
            timeoutSeconds = 15
        });

        // Assert
        response.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        response.RootElement.TryGetProperty("result", out _).Should().BeTrue();
        JsonRpcHelper.IsError(response).Should().BeFalse();
    }

    #endregion

    #region Safe Command Execution

    [Fact]
    public async Task execute_sudo_command_WhoAmI_ReturnsRoot()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "whoami",
            timeoutSeconds = 15
        });

        // Assert
        response.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");

        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        resultText.Should().NotBeNullOrWhiteSpace();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain("root");
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task execute_sudo_command_Echo_ReturnsCorrectOutput()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();
        const string testMessage = "Hello from MCP integration test!";

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = $"echo '{testMessage}'",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain(testMessage);
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task execute_sudo_command_Id_ShowsUid0()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "id -u",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain("0");  // root uid
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task execute_sudo_command_ReadRootFile_Succeeds()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "cat /etc/shadow | head -1",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().NotBeNullOrEmpty();  // Can read root-only file
        result.ExitCode.Should().Be(0);
    }

    #endregion

    #region Blocklist Enforcement

    [Fact]
    public async Task execute_sudo_command_RmRfRoot_IsBlocked()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "rm -rf /",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task execute_sudo_command_MkfsCommand_IsBlocked()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "mkfs.ext4 /dev/sda1",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task execute_sudo_command_DangerousDd_IsBlocked()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "dd if=/dev/zero of=/dev/sda bs=1M",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task execute_sudo_command_Cryptsetup_IsBlocked()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "cryptsetup luksFormat /dev/sda1",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task execute_sudo_command_NonExistentCommand_ReturnsError()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "this-command-does-not-exist-12345",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task execute_sudo_command_NonZeroExit_ReturnsExitCode()
    {
        // Arrange
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        // Act - false command always returns exit code 1
        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "false",
            timeoutSeconds = 15
        });

        // Assert
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        CommandExecutionResult? result = JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ExitCode.Should().Be(1);
    }

    #endregion
}
