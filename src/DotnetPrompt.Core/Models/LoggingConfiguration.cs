using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for logging
/// </summary>
public class LoggingConfiguration
{
    [YamlMember(Alias = "level")]
    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [YamlMember(Alias = "file")]
    [JsonPropertyName("file")]
    public string? File { get; set; }

    [YamlMember(Alias = "console")]
    [JsonPropertyName("console")]
    public bool? Console { get; set; }

    [YamlMember(Alias = "structured")]
    [JsonPropertyName("structured")]
    public bool? Structured { get; set; }

    [YamlMember(Alias = "include_scopes")]
    [JsonPropertyName("include_scopes")]
    public bool? IncludeScopes { get; set; }
}