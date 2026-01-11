using System.Text.RegularExpressions;
using SudoMcp.Models;

namespace SudoMcp.Services;

/// <summary>
/// Validates commands against a blocklist of dangerous operations.
/// Can be enabled or disabled via configuration.
/// </summary>
public class CommandValidator
{
    private readonly bool _enabled;
    private readonly BlocklistConfiguration? _blocklist;

    /// <summary>
    /// Creates a disabled validator that allows all commands.
    /// </summary>
    public CommandValidator()
    {
        _enabled = false;
        _blocklist = null;
    }

    /// <summary>
    /// Creates a validator with the specified blocklist configuration.
    /// </summary>
    /// <param name="blocklist">The blocklist configuration with pre-compiled patterns.</param>
    public CommandValidator(BlocklistConfiguration blocklist)
    {
        _enabled = true;
        _blocklist = blocklist;
    }

    /// <summary>
    /// Validates a command against the blocklist.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A validation result indicating whether the command is allowed.</returns>
    public ValidationResult ValidateCommand(string command)
    {
        if (!_enabled || _blocklist is null)
        {
            return ValidationResult.Allowed();
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return ValidationResult.Denied("Command cannot be empty");
        }

        string trimmedCommand = command.Trim();

        foreach (string blocked in _blocklist.ExactMatches)
        {
            if (trimmedCommand.Equals(blocked, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Denied($"Command exactly matches blocklist: '{blocked}'");
            }
        }

        foreach (Regex pattern in _blocklist.RegexPatterns)
        {
            if (pattern.IsMatch(trimmedCommand))
            {
                return ValidationResult.Denied($"Command matches dangerous pattern: {pattern}");
            }
        }

        string[] tokens = trimmedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 0)
        {
            string binary = tokens[0];
            string binaryName = Path.GetFileName(binary);

            if (_blocklist.BlockedBinaries.Contains(binary, StringComparer.OrdinalIgnoreCase) ||
                _blocklist.BlockedBinaries.Contains(binaryName, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Denied($"Binary '{binary}' is blocked");
            }
        }

        return ValidationResult.Allowed();
    }
}

/// <summary>
/// Result of command validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the command is valid and should be executed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Reason for denial (if command was blocked).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Creates a result indicating the command is allowed.
    /// </summary>
    public static ValidationResult Allowed() => new() { IsValid = true };

    /// <summary>
    /// Creates a result indicating the command is denied.
    /// </summary>
    /// <param name="reason">Reason for denial.</param>
    public static ValidationResult Denied(string reason) => new() { IsValid = false, Reason = reason };
}