using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Configuration;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Service for managing dotnet-prompt configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from all sources in hierarchical order
    /// </summary>
    /// <param name="cliProvider">Provider specified via CLI argument</param>
    /// <param name="cliModel">Model specified via CLI argument</param>
    /// <param name="cliVerbose">Verbose flag from CLI</param>
    /// <param name="cliConfigFile">Custom config file path from CLI</param>
    /// <param name="projectPath">Project directory path</param>
    /// <param name="workflowModel">Model specification from workflow frontmatter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved configuration</returns>
    Task<DotPromptConfiguration> LoadConfigurationAsync(
        string? cliProvider = null,
        string? cliModel = null,
        bool? cliVerbose = null,
        string? cliConfigFile = null,
        string? projectPath = null,
        string? workflowModel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration and returns validation results
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>Validation result</returns>
    ConfigurationValidationResult ValidateConfiguration(DotPromptConfiguration configuration);

    /// <summary>
    /// Saves configuration to the appropriate file (global or project)
    /// </summary>
    /// <param name="configuration">Configuration to save</param>
    /// <param name="isGlobal">Whether to save as global configuration</param>
    /// <param name="projectPath">Project path for project configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigurationAsync(
        DotPromptConfiguration configuration,
        bool isGlobal = true,
        string? projectPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective configuration value by key, resolving environment variables
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Resolved configuration value</returns>
    string? GetConfigurationValue(string key, string? defaultValue = null);

    /// <summary>
    /// Gets global configuration file path
    /// </summary>
    string GetGlobalConfigurationPath();

    /// <summary>
    /// Gets project configuration file path
    /// </summary>
    /// <param name="projectPath">Project directory path</param>
    string GetProjectConfigurationPath(string projectPath);
}