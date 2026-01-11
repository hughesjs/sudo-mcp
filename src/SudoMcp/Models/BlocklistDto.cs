using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SudoMcp.Models;

/// <summary>
/// DTO for deserializing blocklist configuration from JSON.
/// </summary>
public class BlocklistDto
{
    [JsonPropertyName("ExactMatches")]
    public List<string> ExactMatches { get; init; } = [];

    [JsonPropertyName("RegexPatterns")]
    public List<string> RegexPatterns { get; init; } = [];

    [JsonPropertyName("BlockedBinaries")]
    public List<string> BlockedBinaries { get; init; } = [];

    /// <summary>
    /// Converts the DTO to a runtime configuration with pre-compiled regexes.
    /// </summary>
    /// <param name="regexTimeout">Timeout for regex matching to prevent ReDoS.</param>
    public BlocklistConfiguration ToConfiguration(TimeSpan? regexTimeout = null)
    {
        TimeSpan timeout = regexTimeout ?? TimeSpan.FromSeconds(1);

        return new BlocklistConfiguration
        {
            ExactMatches = ExactMatches,
            RegexPatterns = RegexPatterns
                .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase, timeout))
                .ToList(),
            BlockedBinaries = BlockedBinaries
        };
    }
}