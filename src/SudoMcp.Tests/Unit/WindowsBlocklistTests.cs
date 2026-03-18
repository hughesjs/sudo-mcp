using FluentAssertions;
using SudoMcp.Configuration;
using SudoMcp.Services;
using Xunit;

namespace SudoMcp.Tests.Unit;

/// <summary>
/// Unit tests for Windows-specific blocklist patterns.
/// </summary>
public sealed class WindowsBlocklistTests
{
    private readonly CommandValidator _validator;

    public WindowsBlocklistTests()
    {
        _validator = new CommandValidator(DefaultBlocklist.Configuration);
    }

    // format command
    [Theory]
    [InlineData("format C:")]
    [InlineData("format D: /fs:NTFS")]
    [InlineData("format E: /fs:FAT32 /q")]
    [InlineData("FORMAT C: /X")]
    public void Validator_FormatDrive_IsBlocked(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeFalse();
    }

    // diskpart
    [Fact]
    public void Validator_Diskpart_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskpart");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_DiskpartWithScript_IsBlocked()
    {
        ValidationResult result = _validator.ValidateCommand("diskpart /s script.txt");
        result.IsValid.Should().BeFalse();
    }

    // bcdedit (boot config)
    [Theory]
    [InlineData("bcdedit")]
    [InlineData("bcdedit /set bootmgr path \\EFI\\file.efi")]
    [InlineData("bcdedit /deletevalue {default} safeboot")]
    public void Validator_Bcdedit_IsBlocked(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeFalse();
    }

    // Registry destructive operations
    [Theory]
    [InlineData("reg delete HKLM\\SOFTWARE\\MyApp /f")]
    [InlineData("reg add HKLM\\SYSTEM\\CurrentControlSet\\Services\\MyService /v Start /t REG_DWORD /d 4 /f")]
    public void Validator_RegSystemHive_IsBlocked(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeFalse();
    }

    // PowerShell destructive cmdlets
    [Theory]
    [InlineData("powershell -Command Remove-Item C:\\ -Recurse -Force")]
    [InlineData("pwsh -Command Remove-Item C:\\ -Recurse -Force")]
    [InlineData("powershell.exe -Command Remove-Item 'C:\\Windows' -Recurse -Force")]
    [InlineData("powershell -Command Remove-Item C:\\ -Force -Recurse")]
    public void Validator_PowerShellDestructiveRecursiveDelete_IsBlocked(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeFalse();
    }

    // cipher (secure wipe)
    [Theory]
    [InlineData("cipher /w:C:\\")]
    [InlineData("cipher.exe /w:D:\\Users")]
    public void Validator_Cipher_IsBlocked(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeFalse();
    }

    // Safe Windows commands should pass
    [Theory]
    [InlineData("net start wuauserv")]
    [InlineData("sc query wuauserv")]
    [InlineData("ipconfig /all")]
    [InlineData("sfc /scannow")]
    [InlineData("reg query HKLM\\SOFTWARE")]
    public void Validator_SafeWindowsCommands_AreAllowed(string command)
    {
        ValidationResult result = _validator.ValidateCommand(command);
        result.IsValid.Should().BeTrue();
    }
}
