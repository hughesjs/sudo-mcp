using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SudoMcp.Configuration;
using SudoMcp.Models;
using SudoMcp.Services;

Option<string?> blocklistFileOption = new(
    aliases: ["--blocklist-file", "-b"],
    description: "Path to custom blocklist JSON file (uses embedded default if not specified)");

Option<bool> noBlocklistOption = new(
    aliases: ["--no-blocklist"],
    description: "Disable blocklist validation (DANGEROUS)",
    getDefaultValue: () => false);

Option<string> auditLogOption = new(
    aliases: ["--audit-log", "-a"],
    description: "Path to audit log file",
    getDefaultValue: () => "/var/log/sudo-mcp/audit.log");

Option<int> timeoutOption = new(
    aliases: ["--timeout", "-t"],
    description: "Default command execution timeout in seconds",
    getDefaultValue: () => 15);

RootCommand rootCommand = new("sudo-mcp: MCP server for privileged command execution");
rootCommand.AddOption(blocklistFileOption);
rootCommand.AddOption(noBlocklistOption);
rootCommand.AddOption(auditLogOption);
rootCommand.AddOption(timeoutOption);

rootCommand.SetHandler(async (blocklistFile, noBlocklist, auditLog, timeout) =>
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    // CRITICAL: All logs to stderr for stdio transport compatibility
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    builder.Services.AddSingleton(new ExecutionOptions
    {
        AuditLogPath = auditLog,
        TimeoutSeconds = timeout
    });

    builder.Services.AddSingleton<CommandValidator>(sp =>
    {
        ILogger<CommandValidator> logger = sp.GetRequiredService<ILogger<CommandValidator>>();

        if (noBlocklist)
        {
            logger.LogWarning("⚠️ BLOCKLIST DISABLED - All commands will be allowed!");
            return new CommandValidator();
        }

        BlocklistConfiguration blocklist;

        if (!string.IsNullOrWhiteSpace(blocklistFile))
        {
            // Custom blocklist file specified
            if (!File.Exists(blocklistFile))
            {
                throw new FileNotFoundException(
                    $"Blocklist file not found: {blocklistFile}");
            }

            logger.LogInformation("Loading custom blocklist from: {BlocklistFile}", blocklistFile);
            string json = File.ReadAllText(blocklistFile);

            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
            BlocklistFileRoot? root = JsonSerializer.Deserialize<BlocklistFileRoot>(json, jsonOptions);

            if (root?.BlockedCommands is null)
            {
                throw new InvalidOperationException(
                    $"Invalid blocklist file format: {blocklistFile}. Expected 'BlockedCommands' root element.");
            }

            blocklist = root.BlockedCommands.ToConfiguration();
        }
        else
        {
            // Use embedded default
            logger.LogInformation("Using embedded default blocklist");
            blocklist = DefaultBlocklist.Configuration;
        }

        return new CommandValidator(blocklist);
    });

    builder.Services.AddScoped<PkexecExecutor>();
    builder.Services.AddScoped<AuditLogger>();

    IHost host = builder.Build();

    ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("sudo-mcp starting...");

    if (noBlocklist)
    {
        logger.LogInformation("Blocklist: DISABLED");
    }
    else if (!string.IsNullOrWhiteSpace(blocklistFile))
    {
        logger.LogInformation("Blocklist: {Blocklist}", blocklistFile);
    }
    else
    {
        logger.LogInformation("Blocklist: embedded default");
    }

    logger.LogInformation("Audit log: {AuditLog}", auditLog);
    logger.LogInformation("Timeout: {Timeout} seconds", timeout);

    await host.RunAsync();

}, blocklistFileOption, noBlocklistOption, auditLogOption, timeoutOption);

return await rootCommand.InvokeAsync(args);

/// <summary>
/// Root element for blocklist JSON files (wraps BlockedCommands).
/// </summary>
file class BlocklistFileRoot
{
    public BlocklistDto? BlockedCommands { get; init; }
}