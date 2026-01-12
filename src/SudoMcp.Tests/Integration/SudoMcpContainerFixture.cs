using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Xunit;

namespace SudoMcp.Tests.Integration;

/// <summary>
/// TestContainers fixture for managing Docker container lifecycle during integration tests
/// </summary>
public sealed class SudoMcpContainerFixture : IAsyncLifetime
{
    private IFutureDockerImage? _image;
    private IContainer? _container;
    private const string BinaryPath = "/usr/bin/sudo-mcp";

    public async Task InitializeAsync()
    {
        // Build Docker image from Dockerfile
        // No image name specified - TestContainers generates unique name each run
        // Docker's layer cache still provides fast rebuilds when source unchanged
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("src/SudoMcp.Tests/Integration/Dockerfile")
            .WithCleanUp(true)  // Clean up image after tests
            .Build();

        await _image.CreateAsync().ConfigureAwait(false);

        // Create container from built image
        // Wait for dbus to be running (required for polkit/pkexec)
        _container = new ContainerBuilder(_image)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted("pgrep", "-x", "dbus-daemon"))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync().ConfigureAwait(false);

        // Verify sudo-mcp binary exists
        ExecResult checkResult = await _container.ExecAsync(["test", "-f", BinaryPath])
            .ConfigureAwait(false);

        if (checkResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"sudo-mcp binary not found at {BinaryPath} in container");
        }
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a new MCP test client for communicating with sudo-mcp via JSON-RPC
    /// </summary>
    /// <returns>Initialized MCP test client</returns>
    public async Task<McpTestClient> CreateMcpClientAsync()
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Container not initialized");
        }

        McpTestClient client = new(_container);
        await client.InitialiseAsync().ConfigureAwait(false);
        return client;
    }
}
