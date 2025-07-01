using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents the input section of a dotprompt workflow
/// </summary>
public class DotpromptInput
{
    [YamlMember(Alias = "default")]
    [JsonPropertyName("default")]
    public Dictionary<string, object>? Default { get; set; }

    [YamlMember(Alias = "schema")]
    [JsonPropertyName("schema")]
    public Dictionary<string, DotpromptInputSchema>? Schema { get; set; }
}

/// <summary>
/// Represents a schema definition for an input parameter
/// </summary>
public class DotpromptInputSchema
{
    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "required")]
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [YamlMember(Alias = "default")]
    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [YamlMember(Alias = "enum")]
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }

    [YamlMember(Alias = "minLength")]
    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [YamlMember(Alias = "maxLength")]
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [YamlMember(Alias = "pattern")]
    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}