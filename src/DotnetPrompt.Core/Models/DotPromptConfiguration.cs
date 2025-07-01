using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Root configuration model for dotnet-prompt application
/// </summary>
public class DotPromptConfiguration
{
    [YamlMember(Alias = "default_provider")]
    [JsonPropertyName("default_provider")]
    public string? DefaultProvider { get; set; }

    [YamlMember(Alias = "default_model")]
    [JsonPropertyName("default_model")]
    public string? DefaultModel { get; set; }

    [YamlMember(Alias = "providers")]
    [JsonPropertyName("providers")]
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();

    [YamlMember(Alias = "logging")]
    [JsonPropertyName("logging")]
    public LoggingConfiguration? Logging { get; set; }

    [YamlMember(Alias = "timeout")]
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [YamlMember(Alias = "cache_enabled")]
    [JsonPropertyName("cache_enabled")]
    public bool? CacheEnabled { get; set; }

    [YamlMember(Alias = "cache_directory")]
    [JsonPropertyName("cache_directory")]
    public string? CacheDirectory { get; set; }

    [YamlMember(Alias = "telemetry_enabled")]
    [JsonPropertyName("telemetry_enabled")]
    public bool? TelemetryEnabled { get; set; }

    [YamlMember(Alias = "tool_configuration")]
    [JsonPropertyName("tool_configuration")]
    public Dictionary<string, object> ToolConfiguration { get; set; } = new();
}