using FluentAssertions;
using SudoMcp.Configuration;
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
        // Use the embedded default blocklist
        _validator = new CommandValidator(DefaultBlocklist.Configuration);
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

    [Fact]
    public void Validator_Disabled_AllowsEverything()
    {
        CommandValidator disabledValidator = new();

        ValidationResult result = disabledValidator.ValidateCommand("rm -rf /");

        result.IsValid.Should().BeTrue();
    }

    // macOS blocklist tests

    [Fact]
    public void Validator_DdToMacDisk_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("dd if=/dev/zero of=/dev/disk0 bs=1M");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilEraseDisk_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil eraseDisk JHFS+ Untitled /dev/disk2");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilPartitionDisk_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil partitionDisk /dev/disk1 GPT JHFS+ Macintosh 100%");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilSecureErase_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil secureErase 0 /dev/disk0");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilZeroDisk_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil zeroDisk /dev/disk0");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilApfsDeleteContainer_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil apfs deleteContainer disk1");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilApfsDeleteVolume_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil apfs deleteVolume disk1s1");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskutilList_IsAllowed()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil list");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_DiskutilInfo_IsAllowed()
    {
        ValidationResult result = _validator.ValidateCommand("diskutil info /dev/disk0");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_NewfsHfs_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("newfs_hfs /dev/disk2s1");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_NewfsApfs_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("newfs_apfs /dev/disk2s1");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_NewfsExfat_BinaryBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("newfs_exfat /dev/disk2s1");

        result.IsValid.Should().BeFalse();
    }
}