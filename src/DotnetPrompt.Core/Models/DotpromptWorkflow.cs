using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents a complete dotprompt workflow following the dotprompt format specification
/// </summary>
public class DotpromptWorkflow
{
    /// <summary>
    /// Name of the workflow (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Model specification (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "model")]
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Model configuration parameters (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "config")]
    [JsonPropertyName("config")]
    public DotpromptConfig? Config { get; set; }

    /// <summary>
    /// List of tools available to the workflow (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "tools")]
    [JsonPropertyName("tools")]
    public List<string>? Tools { get; set; }

    /// <summary>
    /// Input parameter configuration (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "input")]
    [JsonPropertyName("input")]
    public DotpromptInput? Input { get; set; }

    /// <summary>
    /// Output format specification (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "output")]
    [JsonPropertyName("output")]
    public DotpromptOutput? Output { get; set; }

    /// <summary>
    /// Workflow metadata (dotprompt standard field)
    /// </summary>
    [YamlMember(Alias = "metadata")]
    [JsonPropertyName("metadata")]
    public DotpromptMetadata? Metadata { get; set; }

    /// <summary>
    /// dotnet-prompt specific extension fields
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public DotpromptExtensions Extensions { get; set; } = new();

    /// <summary>
    /// Markdown content of the workflow
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public WorkflowContent Content { get; set; } = new();

    /// <summary>
    /// Path to the original workflow file
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>
    /// Hash of the workflow content for change detection
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Indicates whether this workflow has YAML frontmatter
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public bool HasFrontmatter { get; set; }

    /// <summary>
    /// Raw frontmatter content as string
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public string? RawFrontmatter { get; set; }

    /// <summary>
    /// Extension field storage for custom dotnet-prompt fields
    /// This allows us to handle the namespaced fields dynamically
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public Dictionary<string, object> ExtensionFields { get; set; } = new();
}