using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Configuration for progress tracking
/// </summary>
public class ProgressConfig
{
    [YamlMember(Alias = "enabled")]
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [YamlMember(Alias = "checkpoint_frequency")]
    [JsonPropertyName("checkpoint_frequency")]
    public string? CheckpointFrequency { get; set; }

    [YamlMember(Alias = "storage_location")]
    [JsonPropertyName("storage_location")]
    public string? StorageLocation { get; set; }
}