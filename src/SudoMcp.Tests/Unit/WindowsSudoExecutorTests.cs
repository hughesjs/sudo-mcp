using FluentAssertions;
using SudoMcp.Services;
using Xunit;

namespace SudoMcp.Tests.Unit;

/// <summary>
/// Unit tests for WindowsSudoExecutor.
/// Validates interface compliance and construction.
/// Actual execution requires Windows 11 24H2+ with sudo enabled.
/// CA1416 suppressed because we're testing the class compiles and constructs correctly
/// on all platforms, not that it can execute Windows commands.
/// </summary>
#pragma warning disable CA1416 // Platform compatibility
public sealed class WindowsSudoExecutorTests
{
    [Fact]
    public void WindowsSudoExecutor_ImplementsIPrivilegedExecutor()
    {
        typeof(WindowsSudoExecutor).Should().Implement<IPrivilegedExecutor>();
    }

    [Fact]
    public void WindowsSudoExecutor_CanBeConstructed()
    {
        var options = new Models.ExecutionOptions
        {
            AuditLogPath = "test.log",
            TimeoutSeconds = 15
        };

        var executor = new WindowsSudoExecutor(options);

        executor.Should().NotBeNull();
    }
}
#pragma warning restore CA1416
