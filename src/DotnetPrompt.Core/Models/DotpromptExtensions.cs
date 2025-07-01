using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents dotnet-prompt specific extension fields using namespaced approach
/// </summary>
public class DotpromptExtensions
{
    [YamlMember(Alias = "dotnet-prompt.mcp")]
    [JsonPropertyName("dotnet-prompt.mcp")]
    public List<McpServerConfig>? Mcp { get; set; }

    [YamlMember(Alias = "dotnet-prompt.sub-workflows")]
    [JsonPropertyName("dotnet-prompt.sub-workflows")]
    public List<SubWorkflowConfig>? SubWorkflows { get; set; }

    [YamlMember(Alias = "dotnet-prompt.progress")]
    [JsonPropertyName("dotnet-prompt.progress")]
    public ProgressConfig? Progress { get; set; }

    [YamlMember(Alias = "dotnet-prompt.error-handling")]
    [JsonPropertyName("dotnet-prompt.error-handling")]
    public ErrorHandlingConfig? ErrorHandling { get; set; }
}

/// <summary>
/// Configuration for MCP server integration
/// </summary>
public class McpServerConfig
{
    [YamlMember(Alias = "server")]
    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [YamlMember(Alias = "version")]
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [YamlMember(Alias = "config")]
    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }
}

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

/// <summary>
/// Configuration for error handling
/// </summary>
public class ErrorHandlingConfig
{
    [YamlMember(Alias = "retry_attempts")]
    [JsonPropertyName("retry_attempts")]
    public int? RetryAttempts { get; set; }

    [YamlMember(Alias = "backoff_strategy")]
    [JsonPropertyName("backoff_strategy")]
    public string? BackoffStrategy { get; set; }

    [YamlMember(Alias = "timeout_seconds")]
    [JsonPropertyName("timeout_seconds")]
    public int? TimeoutSeconds { get; set; }
}