using System.Text.Json.Serialization;
using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// Progress file format for file-based storage
/// </summary>
public class WorkflowProgressFile
{
    /// <summary>
    /// Workflow metadata information
    /// </summary>
    [JsonPropertyName("workflow_metadata")]
    public WorkflowMetadata WorkflowMetadata { get; set; } = new();

    /// <summary>
    /// Chat history messages
    /// </summary>
    [JsonPropertyName("chat_history")]
    public ChatMessage[] ChatHistory { get; set; } = Array.Empty<ChatMessage>();

    /// <summary>
    /// Execution context data
    /// </summary>
    [JsonPropertyName("execution_context")]
    public ExecutionContextData ExecutionContext { get; set; } = new();
}

/// <summary>
/// Workflow metadata for progress file
/// </summary>
public class WorkflowMetadata
{
    /// <summary>
    /// Unique workflow execution identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of workflow content for compatibility validation
    /// </summary>
    [JsonPropertyName("workflow_hash")]
    public string WorkflowHash { get; set; } = string.Empty;

    /// <summary>
    /// When workflow execution started
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When workflow state was last updated
    /// </summary>
    [JsonPropertyName("last_checkpoint")]
    public DateTimeOffset LastCheckpoint { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current execution status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "not_started";
}

/// <summary>
/// Chat message for progress file
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Message role (user, assistant, tool)
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Function calls if applicable
    /// </summary>
    [JsonPropertyName("function_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FunctionCall[]? FunctionCalls { get; set; }

    /// <summary>
    /// Tool call ID for tool messages
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

/// <summary>
/// Function call information
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Function name
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Function parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Call identifier
    /// </summary>
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = string.Empty;
}

/// <summary>
/// Execution context data for progress file
/// </summary>
public class ExecutionContextData
{
    /// <summary>
    /// Current step being executed (0-based index)
    /// </summary>
    [JsonPropertyName("current_step")]
    public int CurrentStep { get; set; } = 0;

    /// <summary>
    /// Execution variables
    /// </summary>
    [JsonPropertyName("variables")]
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Execution history
    /// </summary>
    [JsonPropertyName("execution_history")]
    public List<StepExecutionHistory> ExecutionHistory { get; set; } = new();
}