using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SudoMcp.Models;
using SudoMcp.Services;

Option<string?> blocklistFileOption = new(
    aliases: ["--blocklist-file", "-b"],
    description: "Path to blocklist JSON file",
    getDefaultValue: () => "Configuration/BlockedCommands.json");

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

    builder.Configuration.AddJsonFile("Configuration/appsettings.json", optional: true, reloadOnChange: false);

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
        if (noBlocklist)
        {
            ILogger<CommandValidator> logger = sp.GetRequiredService<ILogger<CommandValidator>>();
            logger.LogWarning("⚠️ BLOCKLIST DISABLED - All commands will be allowed!");
            return new(enabled: false);
        }

        if (string.IsNullOrWhiteSpace(blocklistFile) || !File.Exists(blocklistFile))
        {
            ILogger<CommandValidator> logger = sp.GetRequiredService<ILogger<CommandValidator>>();
            logger.LogError("Blocklist file not found: {Path}", blocklistFile);
            logger.LogWarning("Creating a disabled validator - ALL COMMANDS WILL BE ALLOWED");
            return new(enabled: false);
        }

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(blocklistFile, optional: false, reloadOnChange: false)
            .Build();

        return new(config, enabled: true);
    });

    builder.Services.AddScoped<PkexecExecutor>();
    builder.Services.AddScoped<AuditLogger>();

    IHost host = builder.Build();

    ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("sudo-mcp starting...");
    logger.LogInformation("Blocklist: {Blocklist}", noBlocklist ? "DISABLED" : blocklistFile);
    logger.LogInformation("Audit log: {AuditLog}", auditLog);
    logger.LogInformation("Timeout: {Timeout} seconds", timeout);

    await host.RunAsync();

}, blocklistFileOption, noBlocklistOption, auditLogOption, timeoutOption);

return await rootCommand.InvokeAsync(args);
