using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for sub-workflow composition
/// </summary>
public class SubWorkflowConfig
{
    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "path")]
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [YamlMember(Alias = "parameters")]
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    [YamlMember(Alias = "depends_on")]
    [JsonPropertyName("depends_on")]
    public List<string>? DependsOn { get; set; }
}