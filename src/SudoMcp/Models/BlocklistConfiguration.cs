using System.Text.RegularExpressions;

namespace SudoMcp.Models;

/// <summary>
/// Runtime configuration for blocked command patterns with pre-compiled regexes.
/// </summary>
public class BlocklistConfiguration
{
    /// <summary>
    /// Commands that are blocked by exact match (case-insensitive).
    /// </summary>
    public required List<string> ExactMatches { get; init; }

    /// <summary>
    /// Pre-compiled regex patterns that block matching commands.
    /// </summary>
    public required List<Regex> RegexPatterns { get; init; }

    /// <summary>
    /// Binary names that are completely blocked.
    /// </summary>
    public required List<string> BlockedBinaries { get; init; }
}