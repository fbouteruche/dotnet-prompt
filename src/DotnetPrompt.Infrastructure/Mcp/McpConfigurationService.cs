using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Service for parsing and processing MCP configuration from workflows
/// </summary>
public class McpConfigurationService
{
    private readonly McpConnectionTypeDetector _connectionTypeDetector;
    private readonly ILogger<McpConfigurationService> _logger;

    public McpConfigurationService(
        McpConnectionTypeDetector connectionTypeDetector,
        ILogger<McpConfigurationService> logger)
    {
        _connectionTypeDetector = connectionTypeDetector;
        _logger = logger;
    }

    /// <summary>
    /// Parses MCP configuration from a workflow and prepares it for registration
    /// </summary>
    /// <param name="workflow">The workflow containing MCP configuration</param>
    /// <returns>Collection of validated and enhanced MCP server configurations</returns>
    public async Task<IEnumerable<McpServerConfig>> ParseMcpConfigurationAsync(DotpromptWorkflow workflow)
    {
        if (workflow.Extensions.Mcp == null || !workflow.Extensions.Mcp.Any())
        {
            return Array.Empty<McpServerConfig>();
        }

        var processedConfigs = new List<McpServerConfig>();

        foreach (var mcpConfig in workflow.Extensions.Mcp)
        {
            try
            {
                var processedConfig = await ProcessMcpServerConfigAsync(mcpConfig);
                processedConfigs.Add(processedConfig);
                
                _logger.LogInformation("Processed MCP server configuration: {ServerName} ({ConnectionType})", 
                    processedConfig.Name, processedConfig.ConnectionType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MCP server configuration for {Server}", mcpConfig.Server);
                // Continue processing other servers - don't fail the entire workflow
            }
        }

        return processedConfigs;
    }

    /// <summary>
    /// Processes an individual MCP server configuration
    /// </summary>
    /// <param name="config">Raw MCP server configuration</param>
    /// <returns>Processed and validated configuration</returns>
    private async Task<McpServerConfig> ProcessMcpServerConfigAsync(McpServerConfig config)
    {
        ValidateBasicConfiguration(config);

        // Detect connection type
        config.ConnectionType = _connectionTypeDetector.DetermineConnectionType(config);

        // Resolve environment variables in configuration
        await ResolveEnvironmentVariablesAsync(config);

        // Validate type-specific configuration
        ValidateConnectionSpecificConfiguration(config);

        return config;
    }

    /// <summary>
    /// Validates basic MCP server configuration
    /// </summary>
    /// <param name="config">MCP server configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    private static void ValidateBasicConfiguration(McpServerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Server))
        {
            throw new InvalidOperationException("MCP server name is required");
        }
    }

    /// <summary>
    /// Validates connection-specific configuration requirements
    /// </summary>
    /// <param name="config">MCP server configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    private static void ValidateConnectionSpecificConfiguration(McpServerConfig config)
    {
        switch (config.ConnectionType)
        {
            case McpConnectionType.Stdio:
                // For stdio connections, we need either a command or a resolvable server name
                // The resolver will handle validation of the actual command
                break;

            case McpConnectionType.Sse:
                ValidateRemoteServerConfiguration(config);
                break;

            case McpConnectionType.Auto:
                throw new InvalidOperationException("Connection type should have been resolved by this point");
        }
    }

    /// <summary>
    /// Validates remote server configuration
    /// </summary>
    /// <param name="config">MCP server configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    private static void ValidateRemoteServerConfiguration(McpServerConfig config)
    {
        // Determine endpoint
        var endpoint = config.Endpoint ?? config.Server;
        
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Remote MCP server requires an endpoint URL");
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException($"Invalid endpoint URL for remote MCP server: {endpoint}");
        }

        // Recommend HTTPS for production
        if (uri.Scheme == "http")
        {
            // This is a warning, not an error - allow HTTP for local development
        }
    }

    /// <summary>
    /// Resolves environment variables in configuration values
    /// </summary>
    /// <param name="config">MCP server configuration to process</param>
    private async Task ResolveEnvironmentVariablesAsync(McpServerConfig config)
    {
        // Resolve auth token environment variables
        if (!string.IsNullOrEmpty(config.AuthToken))
        {
            config.AuthToken = ResolveEnvironmentVariable(config.AuthToken);
        }

        // Resolve environment variables in headers
        if (config.Headers != null)
        {
            var resolvedHeaders = new Dictionary<string, string>();
            foreach (var header in config.Headers)
            {
                resolvedHeaders[header.Key] = ResolveEnvironmentVariable(header.Value);
            }
            config.Headers = resolvedHeaders;
        }

        // Resolve environment variables in config values
        var resolvedConfig = new Dictionary<string, object>();
        foreach (var configItem in config.Config)
        {
            if (configItem.Value is string stringValue)
            {
                resolvedConfig[configItem.Key] = ResolveEnvironmentVariable(stringValue);
            }
            else
            {
                resolvedConfig[configItem.Key] = configItem.Value;
            }
        }
        config.Config = resolvedConfig;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Resolves environment variable references in a string value
    /// </summary>
    /// <param name="value">Value that may contain environment variable references</param>
    /// <returns>Value with environment variables resolved</returns>
    private string ResolveEnvironmentVariable(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Handle ${VAR_NAME} pattern
        if (value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVarName = value[2..^1];
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            
            if (string.IsNullOrEmpty(envValue))
            {
                _logger.LogWarning("Environment variable {EnvVar} is not set or empty", envVarName);
                return string.Empty;
            }
            
            return envValue;
        }

        // Handle $VAR_NAME pattern
        if (value.StartsWith("$") && !value.Contains(" "))
        {
            var envVarName = value[1..];
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            
            if (string.IsNullOrEmpty(envValue))
            {
                _logger.LogWarning("Environment variable {EnvVar} is not set or empty", envVarName);
                return string.Empty;
            }
            
            return envValue;
        }

        // No environment variable pattern found, return as-is
        return value;
    }
}