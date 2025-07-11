using System.Text.Json;
using System.Text.RegularExpressions;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetEscapades.Configuration.Yaml;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Infrastructure.Configuration;

/// <summary>
/// Service for managing dotnet-prompt configuration with hierarchical loading
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    public async Task<DotPromptConfiguration> LoadConfigurationAsync(
        string? cliProvider = null,
        string? cliModel = null,
        bool? cliVerbose = null,
        string? cliConfigFile = null,
        string? projectPath = null,
        string? workflowModel = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading configuration with CLI overrides: Provider={Provider}, Model={Model}, Verbose={Verbose}, WorkflowModel={WorkflowModel}",
            cliProvider, cliModel, cliVerbose, workflowModel);

        var configBuilder = new ConfigurationBuilder();

        // 1. Load environment variables (they need to be loaded first to support substitution)
        configBuilder.AddEnvironmentVariables("DOTNET_PROMPT_");

        // 2. Load global configuration (lowest priority)
        var globalConfigPath = GetGlobalConfigurationPath();
        LoadConfigurationFile(configBuilder, globalConfigPath, "global");

        // 3. Load project configuration
        if (!string.IsNullOrEmpty(projectPath))
        {
            var projectConfigPath = GetProjectConfigurationPath(projectPath);
            LoadConfigurationFile(configBuilder, projectConfigPath, "project");
        }

        // 4. Load custom config file if specified
        if (!string.IsNullOrEmpty(cliConfigFile))
        {
            LoadConfigurationFile(configBuilder, cliConfigFile, "custom");
        }

        var configuration = configBuilder.Build();

        // Build the final configuration object
        var dotPromptConfig = new DotPromptConfiguration();

        // Apply configuration from files
        BindConfigurationToModel(configuration, dotPromptConfig);

        // Apply workflow frontmatter model override (before CLI overrides)
        ApplyWorkflowModelOverride(dotPromptConfig, workflowModel);

        // Apply CLI overrides (highest priority)
        ApplyCliOverrides(dotPromptConfig, cliProvider, cliModel, cliVerbose);

        // Apply environment variable substitution
        SubstituteEnvironmentVariables(dotPromptConfig);

        // Set defaults
        ApplyDefaults(dotPromptConfig);

        _logger.LogDebug("Configuration loaded successfully. Provider={Provider}, Model={Model}",
            dotPromptConfig.DefaultProvider, dotPromptConfig.DefaultModel);

        return await Task.FromResult(dotPromptConfig);
    }

    public ConfigurationValidationResult ValidateConfiguration(DotPromptConfiguration configuration)
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        // Validate provider configurations
        foreach (var provider in configuration.Providers)
        {
            ValidateProvider(provider.Key, provider.Value, result);
        }

        // Validate logging configuration
        if (configuration.Logging != null)
        {
            ValidateLogging(configuration.Logging, result);
        }

        // Validate timeout
        if (configuration.Timeout.HasValue && configuration.Timeout.Value <= 0)
        {
            result.Errors.Add(new ConfigurationValidationError
            {
                Field = "timeout",
                Message = "Timeout must be greater than 0",
                Code = "INVALID_TIMEOUT"
            });
        }

        // Validate cache directory
        if (!string.IsNullOrEmpty(configuration.CacheDirectory))
        {
            ValidatePath(configuration.CacheDirectory, "cache_directory", result);
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public async Task SaveConfigurationAsync(
        DotPromptConfiguration configuration,
        bool isGlobal = true,
        string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var filePath = isGlobal ? GetGlobalConfigurationPath() : GetProjectConfigurationPath(projectPath!);
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        // If the file path is YAML, convert and save as YAML
        if (Path.GetExtension(filePath).ToLowerInvariant() is ".yaml" or ".yml")
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
                .Build();
            
            var yaml = serializer.Serialize(configuration);
            await File.WriteAllTextAsync(filePath, yaml, cancellationToken);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }

        _logger.LogInformation("Configuration saved to {FilePath}", filePath);
    }

    public string? GetConfigurationValue(string key, string? defaultValue = null)
    {
        var envKey = $"DOTNET_PROMPT_{key.ToUpper()}";
        return Environment.GetEnvironmentVariable(envKey) ?? defaultValue;
    }

    public virtual string GetGlobalConfigurationPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".dotnet-prompt", "config.yaml");
    }

    public virtual string GetProjectConfigurationPath(string projectPath)
    {
        return Path.Combine(projectPath, "dotnet-prompt.yaml");
    }

    private void LoadConfigurationFile(IConfigurationBuilder builder, string filePath, string configType)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Configuration file not found: {FilePath} (type: {ConfigType})", filePath, configType);
            return;
        }

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (extension == ".yaml" || extension == ".yml")
            {
                builder.AddYamlFile(filePath, optional: true);
                _logger.LogDebug("Loaded YAML configuration: {FilePath} (type: {ConfigType})", filePath, configType);
            }
            else if (extension == ".json")
            {
                builder.AddJsonFile(filePath, optional: true);
                _logger.LogDebug("Loaded JSON configuration: {FilePath} (type: {ConfigType})", filePath, configType);
            }
            else
            {
                _logger.LogWarning("Unsupported configuration file format: {FilePath} (type: {ConfigType})", filePath, configType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration file: {FilePath} (type: {ConfigType})", filePath, configType);
        }
    }

    private static void BindConfigurationToModel(IConfiguration configuration, DotPromptConfiguration dotPromptConfig)
    {
        // Map from environment variables and config files
        dotPromptConfig.DefaultProvider = configuration["default_provider"] ?? configuration["PROVIDER"];
        dotPromptConfig.DefaultModel = configuration["default_model"] ?? configuration["MODEL"];
        
        if (bool.TryParse(configuration["cache_enabled"], out var cacheEnabled))
            dotPromptConfig.CacheEnabled = cacheEnabled;
        
        if (bool.TryParse(configuration["telemetry_enabled"] ?? configuration["NO_TELEMETRY"], out var telemetryEnabled))
            dotPromptConfig.TelemetryEnabled = !telemetryEnabled; // NO_TELEMETRY is inverted
        
        if (int.TryParse(configuration["timeout"] ?? configuration["TIMEOUT"], out var timeout))
            dotPromptConfig.Timeout = timeout;

        dotPromptConfig.CacheDirectory = configuration["cache_directory"] ?? configuration["CACHE_DIR"];

        // Load providers section
        var providersSection = configuration.GetSection("providers");
        foreach (var providerSection in providersSection.GetChildren())
        {
            var providerConfig = new ProviderConfiguration();
            providerSection.Bind(providerConfig);
            dotPromptConfig.Providers[providerSection.Key] = providerConfig;
        }

        // Load logging section
        var loggingSection = configuration.GetSection("logging");
        if (loggingSection.Exists())
        {
            var loggingConfig = new LoggingConfiguration();
            loggingSection.Bind(loggingConfig);
            dotPromptConfig.Logging = loggingConfig;
        }

        // Load tool configuration section
        var toolConfigSection = configuration.GetSection("tool_configuration");
        foreach (var toolSection in toolConfigSection.GetChildren())
        {
            // Store as generic objects for now - tools will deserialize as needed
            dotPromptConfig.ToolConfiguration[toolSection.Key] = toolSection.Value ?? string.Empty;
        }
    }

    private static void ApplyWorkflowModelOverride(DotPromptConfiguration config, string? workflowModel)
    {
        if (string.IsNullOrEmpty(workflowModel))
            return;

        // Parse provider/model format from workflow frontmatter
        if (workflowModel.Contains('/'))
        {
            var parts = workflowModel.Split('/', 2);
            config.DefaultProvider = parts[0];
            config.DefaultModel = parts[1];
        }
        else
        {
            // Model only - use configured provider, only override model
            config.DefaultModel = workflowModel;
        }
    }

    private static void ApplyCliOverrides(
        DotPromptConfiguration config,
        string? cliProvider,
        string? cliModel,
        bool? cliVerbose)
    {
        if (!string.IsNullOrEmpty(cliProvider))
            config.DefaultProvider = cliProvider;

        if (!string.IsNullOrEmpty(cliModel))
            config.DefaultModel = cliModel;

        if (cliVerbose.HasValue)
        {
            config.Logging ??= new LoggingConfiguration();
            config.Logging.Level = cliVerbose.Value ? "Debug" : "Information";
        }
    }

    private void SubstituteEnvironmentVariables(DotPromptConfiguration config)
    {
        // Substitute environment variables in provider configurations
        foreach (var provider in config.Providers.Values)
        {
            provider.ApiKey = SubstituteEnvironmentVariable(provider.ApiKey);
            provider.BaseUrl = SubstituteEnvironmentVariable(provider.BaseUrl);
            provider.Endpoint = SubstituteEnvironmentVariable(provider.Endpoint);
            provider.Token = SubstituteEnvironmentVariable(provider.Token);
        }

        // Substitute in other string properties
        config.CacheDirectory = SubstituteEnvironmentVariable(config.CacheDirectory);
        
        if (config.Logging != null)
        {
            config.Logging.File = SubstituteEnvironmentVariable(config.Logging.File);
        }
    }

    private static string? SubstituteEnvironmentVariable(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Pattern to match ${VAR_NAME} or $VAR_NAME
        var pattern = @"\$\{([^}]+)\}|\$([A-Za-z_][A-Za-z0-9_]*)";
        
        return Regex.Replace(value, pattern, match =>
        {
            var varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return Environment.GetEnvironmentVariable(varName) ?? match.Value;
        });
    }

    private static void ApplyDefaults(DotPromptConfiguration config)
    {
        config.DefaultProvider ??= "github";
        // Removed default model assignment - model must be explicitly specified
        config.Timeout ??= 300;
        config.CacheEnabled ??= true;
        config.TelemetryEnabled ??= true;
        config.CacheDirectory ??= "./.dotnet-prompt/cache";

        config.Logging ??= new LoggingConfiguration
        {
            Level = "Information",
            Console = true,
            Structured = false,
            IncludeScopes = false
        };
    }

    private static void ValidateProvider(string name, ProviderConfiguration provider, ConfigurationValidationResult result)
    {
        if (string.IsNullOrEmpty(provider.ApiKey) && string.IsNullOrEmpty(provider.Token))
        {
            result.Warnings.Add(new ConfigurationValidationWarning
            {
                Field = $"providers.{name}",
                Message = $"Provider '{name}' has no API key or token configured",
                Code = "MISSING_CREDENTIALS"
            });
        }

        if (!string.IsNullOrEmpty(provider.BaseUrl) && !Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out _))
        {
            result.Errors.Add(new ConfigurationValidationError
            {
                Field = $"providers.{name}.base_url",
                Message = $"Invalid URL format for provider '{name}' base URL",
                Code = "INVALID_URL"
            });
        }

        if (!string.IsNullOrEmpty(provider.Endpoint) && !Uri.TryCreate(provider.Endpoint, UriKind.Absolute, out _))
        {
            result.Errors.Add(new ConfigurationValidationError
            {
                Field = $"providers.{name}.endpoint",
                Message = $"Invalid URL format for provider '{name}' endpoint",
                Code = "INVALID_URL"
            });
        }
    }

    private static void ValidateLogging(LoggingConfiguration logging, ConfigurationValidationResult result)
    {
        var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        
        if (!string.IsNullOrEmpty(logging.Level) && !validLevels.Contains(logging.Level, StringComparer.OrdinalIgnoreCase))
        {
            result.Errors.Add(new ConfigurationValidationError
            {
                Field = "logging.level",
                Message = $"Invalid logging level '{logging.Level}'. Valid values: {string.Join(", ", validLevels)}",
                Code = "INVALID_LOG_LEVEL"
            });
        }

        if (!string.IsNullOrEmpty(logging.File))
        {
            ValidatePath(logging.File, "logging.file", result);
        }
    }

    private static void ValidatePath(string path, string fieldName, ConfigurationValidationResult result)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                result.Warnings.Add(new ConfigurationValidationWarning
                {
                    Field = fieldName,
                    Message = $"Directory does not exist: {directory}",
                    Code = "DIRECTORY_NOT_FOUND"
                });
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ConfigurationValidationError
            {
                Field = fieldName,
                Message = $"Invalid path format: {ex.Message}",
                Code = "INVALID_PATH"
            });
        }
    }
}