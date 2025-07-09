using DotnetPrompt.Core.Models;
using ModelContextProtocol.Client;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Maps enterprise configuration objects to official MCP SDK transport instances.
/// Provides seamless migration from custom implementation to official SDK.
/// </summary>
public class TransportConfigurationMapper
{
    /// <summary>
    /// Creates an MCP client transport from enterprise configuration.
    /// Supports both STDIO and SSE transport types with full configuration mapping.
    /// </summary>
    /// <param name="serverConfig">Enterprise server configuration</param>
    /// <returns>Configured transport instance ready for use with McpClientFactory</returns>
    /// <exception cref="NotSupportedException">Thrown for unsupported transport types</exception>
    public static IClientTransport CreateTransport(McpServerConfig serverConfig)
    {
        var connectionType = DetermineConnectionType(serverConfig);
        
        return connectionType switch
        {
            McpConnectionType.Stdio => CreateStdioTransport(serverConfig),
            McpConnectionType.Sse => CreateSseTransport(serverConfig),
            _ => throw new NotSupportedException($"Connection type '{connectionType}' is not supported")
        };
    }

    /// <summary>
    /// Determines the connection type based on configuration
    /// </summary>
    private static McpConnectionType DetermineConnectionType(McpServerConfig serverConfig)
    {
        if (serverConfig.ConnectionType != McpConnectionType.Auto)
        {
            return serverConfig.ConnectionType;
        }

        // Auto-detect based on configuration properties
        if (!string.IsNullOrEmpty(serverConfig.Command) || !string.IsNullOrEmpty(serverConfig.Server))
        {
            return McpConnectionType.Stdio;
        }

        if (!string.IsNullOrEmpty(serverConfig.Endpoint))
        {
            return McpConnectionType.Sse;
        }

        throw new InvalidOperationException("Cannot auto-detect connection type from configuration");
    }

    private static IClientTransport CreateStdioTransport(McpServerConfig serverConfig)
    {
        var command = serverConfig.Command ?? serverConfig.Server;
        if (string.IsNullOrEmpty(command))
        {
            throw new InvalidOperationException("Command or Server is required for STDIO transport");
        }

        var options = new StdioClientTransportOptions
        {
            Command = command,
            Arguments = serverConfig.Args?.ToList(),
            WorkingDirectory = serverConfig.WorkingDirectory,
            Name = serverConfig.Name
        };

        // Add environment variables from config if present
        var envVars = ExtractEnvironmentVariables(serverConfig);
        if (envVars?.Any() == true)
        {
            options.EnvironmentVariables = envVars;
        }

        // Apply enterprise-specific timeouts if configured
        if (serverConfig.Timeout.HasValue)
        {
            options.ShutdownTimeout = serverConfig.Timeout.Value;
        }

        return new StdioClientTransport(options);
    }

    private static IClientTransport CreateSseTransport(McpServerConfig serverConfig)
    {
        var endpoint = serverConfig.Endpoint ?? serverConfig.Server;
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("Endpoint or Server URL is required for SSE transport");
        }

        var options = new SseClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            Name = serverConfig.Name
        };

        // Apply additional headers from enterprise configuration
        if (serverConfig.Headers?.Any() == true)
        {
            options.AdditionalHeaders = serverConfig.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        // Add auth token as header if present
        if (!string.IsNullOrEmpty(serverConfig.AuthToken))
        {
            options.AdditionalHeaders ??= new Dictionary<string, string>();
            options.AdditionalHeaders["Authorization"] = $"Bearer {serverConfig.AuthToken}";
        }

        // Apply enterprise-specific timeouts if configured
        if (serverConfig.Timeout.HasValue)
        {
            options.ConnectionTimeout = serverConfig.Timeout.Value;
        }

        return new SseClientTransport(options);
    }

    /// <summary>
    /// Extracts environment variables from server configuration
    /// </summary>
    private static IDictionary<string, string?>? ExtractEnvironmentVariables(McpServerConfig serverConfig)
    {
        var envVars = new Dictionary<string, string?>();

        // Extract environment variables from config dictionary
        foreach (var configItem in serverConfig.Config)
        {
            if (configItem.Key.StartsWith("env_") && configItem.Value is string stringValue)
            {
                var envVarName = configItem.Key[4..]; // Remove "env_" prefix
                envVars[envVarName] = ResolveEnvironmentVariable(stringValue);
            }
        }

        return envVars.Count > 0 ? envVars : null;
    }

    /// <summary>
    /// Resolves environment variable references in configuration values
    /// </summary>
    private static string ResolveEnvironmentVariable(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Handle ${VAR_NAME} pattern
        if (value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVarName = value[2..^1];
            return Environment.GetEnvironmentVariable(envVarName) ?? string.Empty;
        }

        // Handle $VAR_NAME pattern
        if (value.StartsWith("$") && !value.Contains(' '))
        {
            var envVarName = value[1..];
            return Environment.GetEnvironmentVariable(envVarName) ?? string.Empty;
        }

        return value;
    }
}
