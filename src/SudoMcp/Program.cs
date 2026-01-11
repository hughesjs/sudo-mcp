using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using SudoMcp.Models;
using SudoMcp.Services;

// Define command-line options
var blocklistFileOption = new Option<string?>(
    aliases: ["--blocklist-file", "-b"],
    description: "Path to blocklist JSON file",
    getDefaultValue: () => "Configuration/BlockedCommands.json");

var noBlocklistOption = new Option<bool>(
    aliases: ["--no-blocklist"],
    description: "Disable blocklist validation (DANGEROUS)",
    getDefaultValue: () => false);

var auditLogOption = new Option<string>(
    aliases: ["--audit-log", "-a"],
    description: "Path to audit log file",
    getDefaultValue: () => "/var/log/sudo-mcp/audit.log");

var timeoutOption = new Option<int>(
    aliases: ["--timeout", "-t"],
    description: "Default command execution timeout in seconds",
    getDefaultValue: () => 15);

var rootCommand = new RootCommand("sudo-mcp: MCP server for privileged command execution");
rootCommand.AddOption(blocklistFileOption);
rootCommand.AddOption(noBlocklistOption);
rootCommand.AddOption(auditLogOption);
rootCommand.AddOption(timeoutOption);

rootCommand.SetHandler(async (blocklistFile, noBlocklist, auditLog, timeout) =>
{
    var builder = Host.CreateApplicationBuilder(args);

    // CRITICAL: All logs to stderr for stdio transport compatibility
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    // Load appsettings.json for logging configuration
    builder.Configuration.AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: false);

    // Register MCP server with stdio transport
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    // Register ExecutionOptions as a singleton
    builder.Services.AddSingleton(new ExecutionOptions
    {
        AuditLogPath = auditLog,
        TimeoutSeconds = timeout
    });

    // Register CommandValidator
    builder.Services.AddSingleton<CommandValidator>(sp =>
    {
        if (noBlocklist)
        {
            var logger = sp.GetRequiredService<ILogger<CommandValidator>>();
            logger.LogWarning("⚠️ BLOCKLIST DISABLED - All commands will be allowed!");
            return new CommandValidator(enabled: false);
        }

        if (string.IsNullOrWhiteSpace(blocklistFile) || !File.Exists(blocklistFile))
        {
            var logger = sp.GetRequiredService<ILogger<CommandValidator>>();
            logger.LogError("Blocklist file not found: {Path}", blocklistFile);
            logger.LogWarning("Creating a disabled validator - ALL COMMANDS WILL BE ALLOWED");
            return new CommandValidator(enabled: false);
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(blocklistFile, optional: false, reloadOnChange: false)
            .Build();

        return new CommandValidator(config, enabled: true);
    });

    // Register other services
    builder.Services.AddScoped<PkexecExecutor>();
    builder.Services.AddScoped<AuditLogger>();

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("sudo-mcp starting...");
    logger.LogInformation("Blocklist: {Blocklist}", noBlocklist ? "DISABLED" : blocklistFile);
    logger.LogInformation("Audit log: {AuditLog}", auditLog);
    logger.LogInformation("Timeout: {Timeout} seconds", timeout);

    await host.RunAsync();

}, blocklistFileOption, noBlocklistOption, auditLogOption, timeoutOption);

return await rootCommand.InvokeAsync(args);
