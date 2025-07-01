using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents the configuration section of a dotprompt workflow
/// </summary>
public class DotpromptConfig
{
    [YamlMember(Alias = "temperature")]
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [YamlMember(Alias = "maxOutputTokens")]
    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    [YamlMember(Alias = "topP")]
    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    [YamlMember(Alias = "topK")]
    [JsonPropertyName("topK")]
    public int? TopK { get; set; }

    [YamlMember(Alias = "stopSequences")]
    [JsonPropertyName("stopSequences")]
    public List<string>? StopSequences { get; set; }

    [YamlMember(Alias = "candidateCount")]
    [JsonPropertyName("candidateCount")]
    public int? CandidateCount { get; set; }

    [YamlMember(Alias = "maxRetries")]
    [JsonPropertyName("maxRetries")]
    public int? MaxRetries { get; set; }

    [YamlMember(Alias = "timeout")]
    [JsonPropertyName("timeout")]
    public string? Timeout { get; set; }
}