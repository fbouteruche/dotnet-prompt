using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for MCP server integration with support for both local and remote connections
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Name of the MCP server (package name for local, URL for remote, or descriptive name)
    /// </summary>
    [YamlMember(Alias = "server")]
    [JsonPropertyName("server")]
    public string? Server { get; set; }

    /// <summary>
    /// Version of the MCP server
    /// </summary>
    [YamlMember(Alias = "version")]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// MCP connection type (Stdio for local, Sse for remote, Auto for detection)
    /// </summary>
    [YamlMember(Alias = "connection_type")]
    [JsonPropertyName("connection_type")]
    public McpConnectionType ConnectionType { get; set; } = McpConnectionType.Auto;

    /// <summary>
    /// Command to execute for local stdio servers
    /// </summary>
    [YamlMember(Alias = "command")]
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Arguments for the local stdio server command
    /// </summary>
    [YamlMember(Alias = "args")]
    [JsonPropertyName("args")]
    public string[]? Args { get; set; }

    /// <summary>
    /// Working directory for local stdio servers
    /// </summary>
    [YamlMember(Alias = "working_directory")]
    [JsonPropertyName("working_directory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Endpoint URL for remote SSE servers
    /// </summary>
    [YamlMember(Alias = "endpoint")]
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// HTTP headers for remote server authentication
    /// </summary>
    [YamlMember(Alias = "headers")]
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Authentication token for remote servers
    /// </summary>
    [YamlMember(Alias = "auth_token")]
    [JsonPropertyName("auth_token")]
    public string? AuthToken { get; set; }

    /// <summary>
    /// Timeout for remote server operations
    /// </summary>
    [YamlMember(Alias = "timeout")]
    [JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Server-specific configuration parameters
    /// </summary>
    [YamlMember(Alias = "config")]
    [JsonPropertyName("config")]
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Descriptive name for the server plugin (auto-generated if not provided)
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public string Name => GetPluginName();

    /// <summary>
    /// Gets the plugin name for Semantic Kernel registration
    /// </summary>
    private string GetPluginName()
    {
        if (!string.IsNullOrEmpty(Server))
        {
            // Clean up server name for use as plugin name
            var name = Server.Split('/').Last().Split('@').Last();
            name = name.Replace("-mcp", "").Replace("_mcp", "");
            name = string.Concat(name.Split('-', '_').Select(part => 
                char.ToUpper(part[0]) + part[1..].ToLower()));
            return name + "Mcp";
        }
        return "UnknownMcp";
    }
}

/// <summary>
/// MCP connection type enumeration
/// </summary>
public enum McpConnectionType
{
    /// <summary>
    /// Auto-detect connection type based on configuration
    /// </summary>
    Auto,
    
    /// <summary>
    /// Local process communication via stdio
    /// </summary>
    Stdio,
    
    /// <summary>
    /// Remote server communication via SSE over HTTPS
    /// </summary>
    Sse
}