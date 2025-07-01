using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace DotnetPrompt.Core.Models;

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