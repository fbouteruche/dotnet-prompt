using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents the output section of a dotprompt workflow
/// </summary>
public class DotpromptOutput
{
    [YamlMember(Alias = "format")]
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [YamlMember(Alias = "schema")]
    [JsonPropertyName("schema")]
    public Dictionary<string, DotpromptOutputSchema>? Schema { get; set; }
}

/// <summary>
/// Represents a schema definition for an output parameter
/// </summary>
public class DotpromptOutputSchema
{
    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "properties")]
    [JsonPropertyName("properties")]
    public Dictionary<string, DotpromptOutputSchema>? Properties { get; set; }

    [YamlMember(Alias = "items")]
    [JsonPropertyName("items")]
    public DotpromptOutputSchema? Items { get; set; }
}