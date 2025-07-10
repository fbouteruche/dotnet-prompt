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

    [YamlMember(Alias = "dotnet-prompt.resume")]
    [JsonPropertyName("dotnet-prompt.resume")]
    public ResumeConfig? Resume { get; set; }

    [YamlMember(Alias = "dotnet-prompt.error-handling")]
    [JsonPropertyName("dotnet-prompt.error-handling")]
    public ErrorHandlingConfig? ErrorHandling { get; set; }
}