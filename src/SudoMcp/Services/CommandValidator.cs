using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace SudoMcp.Services;

/// <summary>
/// Validates commands against a blocklist of dangerous operations.
/// Can be enabled or disabled via configuration.
/// </summary>
public class CommandValidator
{
    private readonly bool _enabled;
    private readonly List<string> _exactMatches;
    private readonly List<Regex> _regexPatterns;
    private readonly List<string> _blockedBinaries;

    /// <summary>
    /// Creates a disabled validator that allows all commands.
    /// </summary>
    /// <param name="enabled">Whether validation is enabled. If false, all commands pass validation.</param>
    public CommandValidator(bool enabled = true)
    {
        _enabled = enabled;
        _exactMatches = [];
        _regexPatterns = [];
        _blockedBinaries = [];
    }

    /// <summary>
    /// Creates a validator with blocklist loaded from configuration.
    /// </summary>
    /// <param name="configuration">Configuration containing the BlockedCommands section.</param>
    /// <param name="enabled">Whether validation is enabled.</param>
    public CommandValidator(IConfiguration configuration, bool enabled = true)
    {
        _enabled = enabled;

        if (!_enabled)
        {
            _exactMatches = [];
            _regexPatterns = [];
            _blockedBinaries = [];
            return;
        }

        var config = configuration.GetSection("BlockedCommands");

        _exactMatches = config.GetSection("ExactMatches").Get<List<string>>() ?? [];
        _blockedBinaries = config.GetSection("BlockedBinaries").Get<List<string>>() ?? [];

        var patterns = config.GetSection("RegexPatterns").Get<List<string>>() ?? [];
        _regexPatterns = patterns
            .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Validates a command against the blocklist.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A validation result indicating whether the command is allowed.</returns>
    public ValidationResult ValidateCommand(string command)
    {
        // If validation is disabled, allow everything
        if (!_enabled)
        {
            return ValidationResult.Allowed();
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return ValidationResult.Denied("Command cannot be empty");
        }

        var trimmedCommand = command.Trim();

        // Check exact matches
        foreach (var blocked in _exactMatches)
        {
            if (trimmedCommand.Equals(blocked, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Denied($"Command exactly matches blocklist: '{blocked}'");
            }
        }

        // Check regex patterns
        foreach (var pattern in _regexPatterns)
        {
            if (pattern.IsMatch(trimmedCommand))
            {
                return ValidationResult.Denied($"Command matches dangerous pattern: {pattern}");
            }
        }

        // Check binary name
        var tokens = trimmedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 0)
        {
            var binary = tokens[0];
            // Check both just the binary name and the full path
            var binaryName = Path.GetFileName(binary);

            if (_blockedBinaries.Contains(binary, StringComparer.OrdinalIgnoreCase) ||
                _blockedBinaries.Contains(binaryName, StringComparer.OrdinalIgnoreCase))
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
