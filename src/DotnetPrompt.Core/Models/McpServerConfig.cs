using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for MCP server integration
/// </summary>
public class McpServerConfig
{
    [YamlMember(Alias = "server")]
    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [YamlMember(Alias = "version")]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [YamlMember(Alias = "config")]
    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }
}