using System.CommandLine;
using System.Text.Json;
using DotnetPrompt.Core;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Cli.Commands;

/// <summary>
/// Command for managing dotnet-prompt configuration
/// </summary>
public class ConfigCommand : Command
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(IConfigurationService configurationService, ILogger<ConfigCommand> logger)
        : base("config", "Manage dotnet-prompt configuration")
    {
        _configurationService = configurationService;
        _logger = logger;

        SetupSubcommands();
    }

    private void SetupSubcommands()
    {
        // config show command
        var showCommand = new Command("show", "Show current effective configuration");
        var showGlobalOption = new Option<bool>("--global", "Show global configuration only");
        var showProjectOption = new Option<bool>("--project", "Show project configuration only");
        showCommand.AddOption(showGlobalOption);
        showCommand.AddOption(showProjectOption);
        showCommand.SetHandler(ShowConfigurationAsync, showGlobalOption, showProjectOption);
        AddCommand(showCommand);

        // config validate command
        var validateCommand = new Command("validate", "Validate configuration files");
        var validatePathOption = new Option<string?>("--path", "Path to validate (defaults to current directory)");
        validateCommand.AddOption(validatePathOption);
        validateCommand.SetHandler(ValidateConfigurationAsync, validatePathOption);
        AddCommand(validateCommand);

        // config init command
        var initCommand = new Command("init", "Initialize configuration file");
        var initGlobalOption = new Option<bool>("--global", "Initialize global configuration");
        var initMinimalOption = new Option<bool>("--minimal", "Create minimal configuration");
        initCommand.AddOption(initGlobalOption);
        initCommand.AddOption(initMinimalOption);
        initCommand.SetHandler(InitializeConfigurationAsync, initGlobalOption, initMinimalOption);
        AddCommand(initCommand);

        // config set command
        var setCommand = new Command("set", "Set configuration value");
        var setKeyArgument = new Argument<string>("key", "Configuration key (e.g., 'default_provider')");
        var setValueArgument = new Argument<string>("value", "Configuration value");
        var setGlobalOption = new Option<bool>("--global", "Set in global configuration");
        setCommand.AddArgument(setKeyArgument);
        setCommand.AddArgument(setValueArgument);
        setCommand.AddOption(setGlobalOption);
        setCommand.SetHandler(SetConfigurationValueAsync, setKeyArgument, setValueArgument, setGlobalOption);
        AddCommand(setCommand);
    }

    private async Task<int> ShowConfigurationAsync(bool showGlobal, bool showProject)
    {
        try
        {
            if (showGlobal && showProject)
            {
                Console.Error.WriteLine("Error: Cannot specify both --global and --project options");
                return ExitCodes.InvalidArguments;
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            var config = await _configurationService.LoadConfigurationAsync(projectPath: currentDirectory);

            string title;
            if (showGlobal)
            {
                title = "Global Configuration";
                // Load only global config by passing non-existent project path
                config = await _configurationService.LoadConfigurationAsync(projectPath: null);
            }
            else if (showProject)
            {
                title = "Project Configuration";
                // This would ideally load only project config, but our current implementation loads hierarchically
                // We could enhance this later to load only specific levels
            }
            else
            {
                title = "Effective Configuration (All Sources)";
            }

            Console.WriteLine($"=== {title} ===");
            Console.WriteLine();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var json = JsonSerializer.Serialize(config, options);
            Console.WriteLine(json);

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show configuration");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> ValidateConfigurationAsync(string? path)
    {
        try
        {
            var currentPath = path ?? Directory.GetCurrentDirectory();
            var config = await _configurationService.LoadConfigurationAsync(projectPath: currentPath);
            var result = _configurationService.ValidateConfiguration(config);

            Console.WriteLine("Configuration Validation Results:");
            Console.WriteLine();

            if (result.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Configuration is valid");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Configuration has errors");
                Console.ResetColor();
            }

            if (result.Errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("  ✗ ");
                    Console.ResetColor();
                    Console.WriteLine($"{error.Field}: {error.Message}");
                    if (!string.IsNullOrEmpty(error.Code))
                    {
                        Console.WriteLine($"    Code: {error.Code}");
                    }
                }
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  ⚠ ");
                    Console.ResetColor();
                    Console.WriteLine($"{warning.Field}: {warning.Message}");
                    if (!string.IsNullOrEmpty(warning.Code))
                    {
                        Console.WriteLine($"    Code: {warning.Code}");
                    }
                }
            }

            return result.IsValid ? ExitCodes.Success : ExitCodes.ValidationError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> InitializeConfigurationAsync(bool isGlobal, bool isMinimal)
    {
        try
        {
            var config = new DotPromptConfiguration
            {
                DefaultProvider = "github",
                DefaultModel = "gpt-4o"
            };

            if (!isMinimal)
            {
                // Add more comprehensive default configuration
                config.Timeout = 300;
                config.CacheEnabled = true;
                config.TelemetryEnabled = true;
                config.CacheDirectory = isGlobal ? "~/.dotnet-prompt/cache" : "./.dotnet-prompt/cache";

                config.Providers["github"] = new ProviderConfiguration
                {
                    Token = "${GITHUB_TOKEN}",
                    BaseUrl = "https://models.inference.ai.azure.com"
                };

                config.Providers["openai"] = new ProviderConfiguration
                {
                    ApiKey = "${OPENAI_API_KEY}",
                    BaseUrl = "https://api.openai.com/v1"
                };

                config.Logging = new LoggingConfiguration
                {
                    Level = "Information",
                    Console = true,
                    Structured = false
                };
            }
            else
            {
                // Minimal configuration
                config.Providers["github"] = new ProviderConfiguration
                {
                    Token = "${GITHUB_TOKEN}"
                };
            }

            var currentPath = Directory.GetCurrentDirectory();
            await _configurationService.SaveConfigurationAsync(config, isGlobal, isGlobal ? null : currentPath);

            var configPath = isGlobal 
                ? _configurationService.GetGlobalConfigurationPath()
                : _configurationService.GetProjectConfigurationPath(currentPath);

            Console.WriteLine($"Configuration initialized at: {configPath}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Set your provider credentials using environment variables");
            Console.WriteLine("2. Run 'dotnet-prompt config validate' to verify your setup");
            Console.WriteLine("3. Run 'dotnet-prompt config show' to view effective configuration");

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize configuration");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }

    private async Task<int> SetConfigurationValueAsync(string key, string value, bool isGlobal)
    {
        try
        {
            var currentPath = Directory.GetCurrentDirectory();
            var config = await _configurationService.LoadConfigurationAsync(projectPath: currentPath);

            // Simple key-value setting - this could be enhanced to support nested keys
            switch (key.ToLowerInvariant())
            {
                case "default_provider":
                case "defaultprovider":
                    config.DefaultProvider = value;
                    break;
                case "default_model":
                case "defaultmodel":
                    config.DefaultModel = value;
                    break;
                case "timeout":
                    if (int.TryParse(value, out var timeout))
                        config.Timeout = timeout;
                    else
                        throw new ArgumentException($"Invalid timeout value: {value}");
                    break;
                case "cache_enabled":
                case "cacheenabled":
                    if (bool.TryParse(value, out var cacheEnabled))
                        config.CacheEnabled = cacheEnabled;
                    else
                        throw new ArgumentException($"Invalid boolean value: {value}");
                    break;
                default:
                    Console.Error.WriteLine($"Warning: Unknown configuration key '{key}'. Value not set.");
                    return ExitCodes.InvalidArguments;
            }

            await _configurationService.SaveConfigurationAsync(config, isGlobal, isGlobal ? null : currentPath);

            Console.WriteLine($"Configuration updated: {key} = {value}");
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration value");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.GeneralError;
        }
    }
}