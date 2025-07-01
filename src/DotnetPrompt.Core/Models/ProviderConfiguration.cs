using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for AI providers
/// </summary>
public class ProviderConfiguration
{
    [YamlMember(Alias = "api_key")]
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [YamlMember(Alias = "base_url")]
    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; set; }

    [YamlMember(Alias = "endpoint")]
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [YamlMember(Alias = "deployment")]
    [JsonPropertyName("deployment")]
    public string? Deployment { get; set; }

    [YamlMember(Alias = "token")]
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [YamlMember(Alias = "api_version")]
    [JsonPropertyName("api_version")]
    public string? ApiVersion { get; set; }

    [YamlMember(Alias = "timeout")]
    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [YamlMember(Alias = "max_retries")]
    [JsonPropertyName("max_retries")]
    public int? MaxRetries { get; set; }
}