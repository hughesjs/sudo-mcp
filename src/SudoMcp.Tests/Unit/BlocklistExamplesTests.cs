using System.Text.Json;
using SudoMcp.Configuration;
using SudoMcp.Models;
using Xunit;

namespace SudoMcp.Tests.Unit;

/// <summary>
/// Tests to ensure example blocklist files stay synchronized with the embedded default.
/// </summary>
public class BlocklistExamplesTests
{
    /// <summary>
    /// Ensures that examples/blocklist-default.json matches the embedded DefaultBlocklist.Configuration.
    /// This test prevents documentation drift and ensures users have an accurate reference implementation.
    /// </summary>
    [Fact]
    public void BlocklistDefaultJson_MatchesEmbeddedDefault()
    {
        // Arrange: Load the example blocklist-default.json file
        string examplePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", // Navigate up from bin/Debug/net10.0 to repo root
            "examples",
            "blocklist-default.json"
        );

        Assert.True(File.Exists(examplePath),
            $"blocklist-default.json not found at expected path: {examplePath}");

        string json = File.ReadAllText(examplePath);

        JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);

        Assert.NotNull(root);
        Assert.NotNull(root.BlockedCommands);

        BlocklistConfiguration fromJson = root.BlockedCommands.ToConfiguration();

        // Act: Get the embedded default configuration
        BlocklistConfiguration embedded = DefaultBlocklist.Configuration;

        // Assert: Exact matches must be identical
        Assert.Equal(embedded.ExactMatches.Count, fromJson.ExactMatches.Count);
        Assert.Equal(embedded.ExactMatches.OrderBy(x => x), fromJson.ExactMatches.OrderBy(x => x));

        // Assert: Regex patterns count must match
        Assert.Equal(embedded.RegexPatterns.Count, fromJson.RegexPatterns.Count);

        // Assert: Regex patterns must have the same patterns (compare ToString() representations)
        List<string> embeddedPatterns = embedded.RegexPatterns
            .Select(r => r.ToString())
            .OrderBy(s => s)
            .ToList();

        List<string> jsonPatterns = fromJson.RegexPatterns
            .Select(r => r.ToString())
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(embeddedPatterns, jsonPatterns);

        // Assert: Blocked binaries must be identical
        Assert.Equal(embedded.BlockedBinaries.Count, fromJson.BlockedBinaries.Count);
        Assert.Equal(embedded.BlockedBinaries.OrderBy(x => x), fromJson.BlockedBinaries.OrderBy(x => x));
    }

    /// <summary>
    /// Ensures all example blocklist JSON files are valid and can be loaded without errors.
    /// </summary>
    [Theory]
    [InlineData("blocklist-default.json")]
    [InlineData("blocklist-permissive.json")]
    [InlineData("blocklist-strict.json")]
    [InlineData("blocklist-minimal.json")]
    public void ExampleBlocklistFiles_AreValidJson(string filename)
    {
        // Arrange: Load the example blocklist file
        string examplePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", // Navigate up from bin/Debug/net10.0 to repo root
            "examples",
            filename
        );

        Assert.True(File.Exists(examplePath),
            $"{filename} not found at expected path: {examplePath}");

        string json = File.ReadAllText(examplePath);

        // Act & Assert: Should deserialize without exception
        JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);

        Assert.NotNull(root);
        Assert.NotNull(root.BlockedCommands);

        // Act: Convert to configuration (compiles regexes)
        BlocklistConfiguration config = root.BlockedCommands.ToConfiguration();

        // Assert: Should have at least one protection
        int totalPatterns = config.ExactMatches.Count +
                          config.RegexPatterns.Count +
                          config.BlockedBinaries.Count;

        Assert.True(totalPatterns > 0,
            $"{filename} has no protection patterns (empty blocklist)");
    }

    /// <summary>
    /// Ensures the permissive profile has fewer restrictions than the default.
    /// </summary>
    [Fact]
    public void BlocklistPermissive_HasFewerRestrictionsThanDefault()
    {
        // Arrange: Load both files
        string permissivePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "examples",
            "blocklist-permissive.json"
        );

        string json = File.ReadAllText(permissivePath);
        JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);
        BlocklistConfiguration permissive = root!.BlockedCommands!.ToConfiguration();

        BlocklistConfiguration defaultBlocklist = DefaultBlocklist.Configuration;

        // Assert: Permissive should have fewer patterns
        int permissiveTotal = permissive.ExactMatches.Count +
                             permissive.RegexPatterns.Count +
                             permissive.BlockedBinaries.Count;

        int defaultTotal = defaultBlocklist.ExactMatches.Count +
                          defaultBlocklist.RegexPatterns.Count +
                          defaultBlocklist.BlockedBinaries.Count;

        Assert.True(permissiveTotal < defaultTotal,
            $"Permissive profile should have fewer patterns than default. " +
            $"Permissive: {permissiveTotal}, Default: {defaultTotal}");
    }

    /// <summary>
    /// Ensures the strict profile has more restrictions than the default.
    /// </summary>
    [Fact]
    public void BlocklistStrict_HasMoreRestrictionsThanDefault()
    {
        // Arrange: Load both files
        string strictPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "examples",
            "blocklist-strict.json"
        );

        string json = File.ReadAllText(strictPath);
        JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);
        BlocklistConfiguration strict = root!.BlockedCommands!.ToConfiguration();

        BlocklistConfiguration defaultBlocklist = DefaultBlocklist.Configuration;

        // Assert: Strict should have more patterns
        int strictTotal = strict.ExactMatches.Count +
                         strict.RegexPatterns.Count +
                         strict.BlockedBinaries.Count;

        int defaultTotal = defaultBlocklist.ExactMatches.Count +
                          defaultBlocklist.RegexPatterns.Count +
                          defaultBlocklist.BlockedBinaries.Count;

        Assert.True(strictTotal > defaultTotal,
            $"Strict profile should have more patterns than default. " +
            $"Strict: {strictTotal}, Default: {defaultTotal}");
    }

    /// <summary>
    /// Ensures the minimal profile has the fewest restrictions.
    /// </summary>
    [Fact]
    public void BlocklistMinimal_HasFewestRestrictions()
    {
        // Arrange: Load the minimal file
        string minimalPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "examples",
            "blocklist-minimal.json"
        );

        string json = File.ReadAllText(minimalPath);
        JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);
        BlocklistConfiguration minimal = root!.BlockedCommands!.ToConfiguration();

        // Assert: Minimal should have very few patterns (less than 5 total)
        int minimalTotal = minimal.ExactMatches.Count +
                          minimal.RegexPatterns.Count +
                          minimal.BlockedBinaries.Count;

        Assert.True(minimalTotal <= 5,
            $"Minimal profile should have 5 or fewer patterns. Found: {minimalTotal}");
    }
}

/// <summary>
/// Root element for blocklist JSON files (wraps BlockedCommands).
/// Same structure as in Program.cs but local to tests.
/// </summary>
file class BlocklistFileRoot
{
    public BlocklistDto? BlockedCommands { get; init; }
}
