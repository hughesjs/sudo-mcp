using System.Text.Json;
using FluentAssertions;
using SudoMcp.Models;

namespace SudoMcp.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class McpProtocolTests : IClassFixture<SudoMcpContainerFixture>
{
    private readonly SudoMcpContainerFixture _fixture;

    public McpProtocolTests(SudoMcpContainerFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    [Fact]
    public async Task ListToolsReturnsExecuteSudoCommand()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.ListToolsAsync();

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
    public async Task ValidJsonRpcReturnsValidResponse()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "echo 'test'",
            timeoutSeconds = 15
        });

        response.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        response.RootElement.TryGetProperty("result", out _).Should().BeTrue();
        JsonRpcHelper.IsError(response).Should().BeFalse();
    }

    [Fact]
    public async Task WhoAmIReturnsRoot()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "whoami",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain("root");
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task EchoReturnsCorrectOutput()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();
        const string testMessage = "Hello from MCP integration test!";

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = $"echo '{testMessage}'",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain(testMessage);
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task IdShowsUid0()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "id -u",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().Contain("0");
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task ReadRootFileSucceeds()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "head -1 /etc/shadow",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput.Should().NotBeNullOrEmpty();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task PipedCommandProcessesPipeCorrectly()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = @"echo -e 'line1\nline2\nline3' | wc -l",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput!.Trim().Should().Be("3");
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task MultipleSudoWithPipesExecutesCorrectly()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "sudo cat /etc/shadow | sudo head -1 | sudo wc -c",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StandardOutput!.Trim().Should().NotBeEmpty();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task RmRfRootIsBlocked()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "rm -rf /",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task MkfsCommandIsBlocked()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "mkfs.ext4 /dev/sda1",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task DangerousDdIsBlocked()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "dd if=/dev/zero of=/dev/sda bs=1M",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }

    [Fact]
    public async Task CryptsetupIsBlocked()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "cryptsetup luksFormat /dev/sda1",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("blocked");
    }


    [Fact]
    public async Task NonExistentCommandReturnsError()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "this-command-does-not-exist-12345",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task NonZeroExitReturnsExitCode()
    {
        await using McpTestClient client = await _fixture.CreateMcpClientAsync();

        JsonDocument response = await client.SendToolCallAsync("execute_sudo_command", new
        {
            command = "false",
            timeoutSeconds = 15
        });

        CommandExecutionResult? result = ParseResult(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ExitCode.Should().Be(1);
    }


    private static CommandExecutionResult? ParseResult(JsonDocument response)
    {
        string? resultText = response.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return JsonSerializer.Deserialize<CommandExecutionResult>(resultText!);
    }
}
