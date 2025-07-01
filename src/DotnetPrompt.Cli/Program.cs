using System.CommandLine;
using System.Reflection;
using DotnetPrompt.Application.Services;
using DotnetPrompt.Cli.Commands;
using DotnetPrompt.Core;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Cli;

/// <summary>
/// Main program entry point for dotnet-prompt CLI tool
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Handle telemetry environment variable
            if (bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_PROMPT_NO_TELEMETRY"), out var noTelemetry) && noTelemetry)
            {
                // TODO: Disable telemetry when implemented
            }

            // Set up dependency injection
            var services = ConfigureServices();
            using var serviceProvider = services.BuildServiceProvider();

            // Create root command
            var rootCommand = CreateRootCommand(serviceProvider);

            // Execute command
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            
            // Set log level based on environment variable or default to Information
            var verboseEnv = Environment.GetEnvironmentVariable("DOTNET_PROMPT_VERBOSE");
            if (bool.TryParse(verboseEnv, out var verbose) && verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Information);
            }
        });

        // Register application services
        services.AddScoped<IWorkflowService, WorkflowService>();

        // Register parsing services
        services.AddScoped<IDotpromptParser, DotpromptParser>();

        // Register configuration services
        services.AddConfigurationServices();

        // Register commands
        services.AddScoped<RunCommand>();
        services.AddScoped<ConfigCommand>();

        return services;
    }

    private static RootCommand CreateRootCommand(ServiceProvider serviceProvider)
    {
        var rootCommand = new RootCommand("A powerful CLI tool for .NET developers to execute AI-powered workflows");

        // Add global options (note: --version is built-in to System.CommandLine)
        var quietOption = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            description: "Suppress non-essential output");

        var noColorOption = new Option<bool>(
            aliases: new[] { "--no-color" },
            description: "Disable colored output");

        var configFileOption = new Option<string?>(
            aliases: new[] { "--config-file" },
            description: "Use specific configuration file");

        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(noColorOption);
        rootCommand.AddGlobalOption(configFileOption);

        // Add commands
        var runCommand = serviceProvider.GetRequiredService<RunCommand>();
        rootCommand.AddCommand(runCommand);

        var configCommand = serviceProvider.GetRequiredService<ConfigCommand>();
        rootCommand.AddCommand(configCommand);

        return rootCommand;
    }
}
