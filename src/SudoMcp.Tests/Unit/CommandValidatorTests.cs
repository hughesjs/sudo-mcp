using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SudoMcp.Services;
using Xunit;

namespace SudoMcp.Tests.Unit;

/// <summary>
/// Unit tests that validate CommandValidator properly blocks dangerous commands
/// </summary>
public sealed class CommandValidatorTests
{
    private readonly CommandValidator _validator;

    public CommandValidatorTests()
    {
        // Load real blocklist configuration from JSON file
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("Configuration/BlockedCommands.json", optional: false)
            .Build();

        _validator = new CommandValidator(configuration, enabled: true);
    }

    [Fact]
    public void Validator_RmRfRoot_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("rm -rf /");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("blocklist");
    }

    [Fact]
    public void Validator_MkfsCommand_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("mkfs.ext4 /dev/sda1");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DdToDisk_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("dd if=/dev/zero of=/dev/sda bs=1M");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ShredCommand_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("shred -vfz -n 10 /dev/sda");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_CryptsetupCommand_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("cryptsetup luksFormat /dev/sda");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_SafeCommand_IsAllowed()
    {
        ValidationResult result = _validator.ValidateCommand("systemctl restart nginx");

        result.IsValid.Should().BeTrue();
    }
}
