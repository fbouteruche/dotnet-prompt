using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents the metadata section of a dotprompt workflow
/// </summary>
public class DotpromptMetadata
{
    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "author")]
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [YamlMember(Alias = "version")]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [YamlMember(Alias = "created")]
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [YamlMember(Alias = "modified")]
    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }
}