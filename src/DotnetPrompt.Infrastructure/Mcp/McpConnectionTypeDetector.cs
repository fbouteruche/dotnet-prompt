using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Service for detecting MCP connection types based on configuration
/// </summary>
public class McpConnectionTypeDetector
{
    /// <summary>
    /// Determines the connection type for an MCP server configuration
    /// </summary>
    /// <param name="config">MCP server configuration</param>
    /// <returns>Detected connection type</returns>
    public McpConnectionType DetermineConnectionType(McpServerConfig config)
    {
        if (config.ConnectionType != McpConnectionType.Auto)
        {
            return config.ConnectionType;
        }

        // Auto-detect based on configuration properties
        if (HasRemoteServerIndicators(config))
        {
            return McpConnectionType.Sse;
        }

        // Default to local stdio for package-like servers
        return McpConnectionType.Stdio;
    }

    /// <summary>
    /// Checks if the configuration indicates a remote server
    /// </summary>
    /// <param name="config">MCP server configuration</param>
    /// <returns>True if configuration suggests remote server</returns>
    private static bool HasRemoteServerIndicators(McpServerConfig config)
    {
        // Explicit endpoint configuration
        if (!string.IsNullOrEmpty(config.Endpoint))
        {
            return true;
        }

        // Server name is a URL
        if (!string.IsNullOrEmpty(config.Server) && 
            (config.Server.StartsWith("http://") || config.Server.StartsWith("https://")))
        {
            return true;
        }

        // Has authentication headers or tokens (typically for remote)
        if (!string.IsNullOrEmpty(config.AuthToken) || 
            (config.Headers?.Count > 0))
        {
            return true;
        }

        // Has timeout specified (more common for remote)
        if (config.Timeout.HasValue)
        {
            return true;
        }

        return false;
    }
}