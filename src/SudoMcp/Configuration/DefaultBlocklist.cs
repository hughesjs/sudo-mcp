using System.Text.RegularExpressions;
using SudoMcp.Models;

namespace SudoMcp.Configuration;

/// <summary>
/// Embedded default blocklist for dangerous commands.
/// This ensures the tool works out of the box without requiring external configuration files.
/// </summary>
public static partial class DefaultBlocklist
{
    /// <summary>
    /// The default blocklist configuration with pre-compiled regex patterns.
    /// </summary>
    public static BlocklistConfiguration Configuration { get; } = new()
    {
        ExactMatches =
        [
            "rm -rf /",
            "rm -rf /*",
            "rm -fr /",
            "rm -fr /*",
            "mkfs",
            "dd",
            ":(){:|:&};:"
        ],

        RegexPatterns =
        [
            // Recursive deletion of root
            RmRfRoot(),
            RmRfRootGlob(),

            // Direct disk writes
            DdToDisk(),
            DdZeroToDisk(),
            RedirectToDisk(),

            // Filesystem creation
            MkfsAny(),

            // Recursive permission changes on root
            ChmodChownRecursiveRoot(),

            // Fork bombs
            ForkBomb(),

            // Pipe to shell (command injection)
            CurlWgetPipeToShell(),

            // Disk partitioning
            Fdisk(),
            Parted(),

            // Disk wiping
            Wipefs(),

            // Moving files to /dev/null
            MvToDevNull()
        ],

        BlockedBinaries =
        [
            "mkfs.ext2",
            "mkfs.ext3",
            "mkfs.ext4",
            "mkfs.xfs",
            "mkfs.btrfs",
            "mkfs.fat",
            "mkfs.vfat",
            "mkfs.ntfs",
            "shred",
            "cryptsetup",
            "wipefs",
            "sgdisk",
            "gdisk"
        ]
    };

    // Recursive deletion of root
    [GeneratedRegex(@"^rm\s+(-rf?|-fr|--recursive)\s+/\s*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RmRfRoot();

    [GeneratedRegex(@"^rm\s+(-rf?|-fr|--recursive)\s+/\*\s*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RmRfRootGlob();

    // Direct disk writes
    [GeneratedRegex(@"^dd\s+if=.+\s+of=/dev/(sd[a-z]|nvme[0-9]n[0-9]|vd[a-z]|hd[a-z]).*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DdToDisk();

    [GeneratedRegex(@"^dd\s+if=/dev/zero\s+of=/dev/(sd[a-z]|nvme[0-9]n[0-9]|vd[a-z]).*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DdZeroToDisk();

    [GeneratedRegex(@"^>(\s*)?/dev/(sd[a-z]|nvme[0-9]n[0-9]|vd[a-z]|hd[a-z]).*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RedirectToDisk();

    // Filesystem creation
    [GeneratedRegex(@"^mkfs\..*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MkfsAny();

    // Recursive permission changes on root
    [GeneratedRegex(@"^(chmod|chown)\s+(-R|--recursive)\s+.*/\s*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ChmodChownRecursiveRoot();

    // Fork bombs
    [GeneratedRegex(@":.*\(.*\).*\{.*\|.*&.*\}.*:", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ForkBomb();

    // Pipe to shell (command injection)
    [GeneratedRegex(@"(wget|curl).*\|.*(sh|bash|zsh|fish)\s*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CurlWgetPipeToShell();

    // Disk partitioning
    [GeneratedRegex(@"^fdisk\s+/dev/(sd[a-z]|nvme[0-9]n[0-9]|vd[a-z]).*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Fdisk();

    [GeneratedRegex(@"^parted\s+/dev/(sd[a-z]|nvme[0-9]n[0-9]|vd[a-z]).*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Parted();

    // Disk wiping
    [GeneratedRegex(@"^wipefs\s+.*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Wipefs();

    // Moving files to /dev/null
    [GeneratedRegex(@"^mv\s+.*\s+/dev/null\s*$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MvToDevNull();
}