using System.Text.Json.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Core resume state for AI workflow resumption. 
/// Captures essential state information solely for intelligent workflow resumption without progress estimation.
/// </summary>
public class AIWorkflowResumeState
{
    /// <summary>
    /// Unique workflow execution identifier
    /// </summary>
    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    [JsonPropertyName("workflow_file_path")]
    public string WorkflowFilePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of original workflow content for compatibility validation
    /// </summary>
    [JsonPropertyName("original_workflow_hash")]
    public string OriginalWorkflowHash { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow content for change detection
    /// </summary>
    [JsonPropertyName("original_workflow_content")]
    public string OriginalWorkflowContent { get; set; } = string.Empty;

    /// <summary>
    /// When workflow execution started
    /// </summary>
    [JsonPropertyName("start_time")]
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When resume state was last updated
    /// </summary>
    [JsonPropertyName("last_activity")]
    public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current workflow phase for context (not progress)
    /// </summary>
    [JsonPropertyName("current_phase")]
    public string CurrentPhase { get; set; } = "understanding";

    /// <summary>
    /// Current AI strategy for context
    /// </summary>
    [JsonPropertyName("current_strategy")]
    public string CurrentStrategy { get; set; } = string.Empty;

    /// <summary>
    /// Completed tool executions for resume context
    /// </summary>
    [JsonPropertyName("completed_tools")]
    public List<CompletedTool> CompletedTools { get; set; } = new();

    /// <summary>
    /// Conversation history for AI context
    /// </summary>
    [JsonPropertyName("chat_history")]
    public List<ChatMessage> ChatHistory { get; set; } = new();

    /// <summary>
    /// Context evolution tracking
    /// </summary>
    [JsonPropertyName("context_evolution")]
    public ContextEvolution ContextEvolution { get; set; } = new();

    /// <summary>
    /// Available tools for validation
    /// </summary>
    [JsonPropertyName("available_tools")]
    public List<string> AvailableTools { get; set; } = new();
}

/// <summary>
/// Represents a completed tool execution with AI reasoning
/// </summary>
public class CompletedTool
{
    /// <summary>
    /// Name of the executed function
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Parameters passed to the function
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Function execution result
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// When the tool was executed
    /// </summary>
    [JsonPropertyName("executed_at")]
    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the tool execution was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// AI reasoning for why this tool was chosen
    /// </summary>
    [JsonPropertyName("ai_reasoning")]
    public string? AIReasoning { get; set; }
}

/// <summary>
/// Chat message for resume context
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Message role (user, assistant, tool, system)
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Tool call ID for tool response messages
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Function calls for assistant messages with tool calls
    /// </summary>
    [JsonPropertyName("function_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FunctionCall>? FunctionCalls { get; set; }
}

/// <summary>
/// Function call information for chat messages
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Name of the function being called
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Parameters for the function call
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Unique call identifier
    /// </summary>
    [JsonPropertyName("call_id")]
    public string CallId { get; set; } = string.Empty;
}

/// <summary>
/// Tracks context evolution and key insights over time
/// </summary>
public class ContextEvolution
{
    /// <summary>
    /// Current accumulated context variables
    /// </summary>
    [JsonPropertyName("current_context")]
    public Dictionary<string, object> CurrentContext { get; set; } = new();

    /// <summary>
    /// Key insights discovered during workflow execution
    /// </summary>
    [JsonPropertyName("key_insights")]
    public List<string> KeyInsights { get; set; } = new();

    /// <summary>
    /// Chronological context changes
    /// </summary>
    [JsonPropertyName("changes")]
    public List<ContextChange> Changes { get; set; } = new();
}

/// <summary>
/// Represents a single context change
/// </summary>
public class ContextChange
{
    /// <summary>
    /// When the change occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Context key that changed
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Previous value
    /// </summary>
    [JsonPropertyName("old_value")]
    public object? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    [JsonPropertyName("new_value")]
    public object? NewValue { get; set; }

    /// <summary>
    /// Source of the change (tool name, process, etc.)
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Reasoning or explanation for the change
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for resume system
/// </summary>
public class ResumeConfig
{
    /// <summary>
    /// Directory for storing resume files
    /// </summary>
    public string StorageLocation { get; set; } = "./.dotnet-prompt/resume";

    /// <summary>
    /// Number of days to retain completed workflow resume files
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Enable compression for large resume files
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Maximum resume file size in bytes before compression
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Frequency of resume checkpoints (1 = after each tool execution)
    /// </summary>
    public int CheckpointFrequency { get; set; } = 1;

    /// <summary>
    /// Enable atomic file writes with backup
    /// </summary>
    public bool EnableAtomicWrites { get; set; } = true;

    /// <summary>
    /// Enable backup creation before overwrites
    /// </summary>
    public bool EnableBackup { get; set; } = true;
}

/// <summary>
/// Compatibility validation result for workflow resume
/// </summary>
public class AIWorkflowResumeCompatibility
{
    /// <summary>
    /// Whether the workflow can be safely resumed
    /// </summary>
    public bool CanResume { get; set; }

    /// <summary>
    /// Whether the resume requires adaptation
    /// </summary>
    public bool RequiresAdaptation { get; set; }

    /// <summary>
    /// Warning messages about compatibility issues
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Suggested adaptations for better compatibility
    /// </summary>
    public List<string> Adaptations { get; set; } = new();

    /// <summary>
    /// Compatibility score from 0.0 to 1.0 (higher is more compatible)
    /// </summary>
    public float CompatibilityScore { get; set; }

    /// <summary>
    /// Migration strategies for handling incompatible changes
    /// </summary>
    public Dictionary<string, string> MigrationStrategies { get; set; } = new();
}

/// <summary>
/// Workflow metadata for resume/progress files
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
/// Resume file format for JSON storage
/// </summary>
public class WorkflowResumeFile
{
    /// <summary>
    /// Workflow metadata information
    /// </summary>
    [JsonPropertyName("workflow_metadata")]
    public WorkflowMetadata WorkflowMetadata { get; set; } = new();

    /// <summary>
    /// Completed tool executions
    /// </summary>
    [JsonPropertyName("completed_tools")]
    public CompletedTool[] CompletedTools { get; set; } = Array.Empty<CompletedTool>();

    /// <summary>
    /// Chat history messages
    /// </summary>
    [JsonPropertyName("chat_history")]
    public ChatMessage[] ChatHistory { get; set; } = Array.Empty<ChatMessage>();

    /// <summary>
    /// Context evolution data
    /// </summary>
    [JsonPropertyName("context_evolution")]
    public ContextEvolution ContextEvolution { get; set; } = new();

    /// <summary>
    /// Workflow variables at time of save
    /// </summary>
    [JsonPropertyName("workflow_variables")]
    public Dictionary<string, object> WorkflowVariables { get; set; } = new();
}