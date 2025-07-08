using System.Text.Json.Serialization;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Progress information for a workflow step
/// </summary>
public class StepProgress
{
    /// <summary>
    /// Name of the step
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the step
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>
    /// When the step was completed
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Step execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether the step completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Step result data
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    /// <summary>
    /// Additional metadata about the step execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Workflow execution status enumeration
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is not started
    /// </summary>
    NotStarted,

    /// <summary>
    /// Workflow is currently in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Workflow completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow failed with errors
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Workflow execution state for progress tracking
/// </summary>
public class WorkflowExecutionState
{
    /// <summary>
    /// Unique workflow execution identifier
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    public string WorkflowFile { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of workflow content for compatibility validation
    /// </summary>
    public string WorkflowHash { get; set; } = string.Empty;

    /// <summary>
    /// Current execution status
    /// </summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.NotStarted;

    /// <summary>
    /// When workflow execution started
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When workflow state was last updated
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current step being executed (0-based index)
    /// </summary>
    public int CurrentStep { get; set; } = 0;

    /// <summary>
    /// List of completed function calls
    /// </summary>
    public List<string> CompletedFunctions { get; set; } = new();

    /// <summary>
    /// List of pending function calls
    /// </summary>
    public List<string> PendingFunctions { get; set; } = new();

    /// <summary>
    /// Function execution results cache
    /// </summary>
    public Dictionary<string, object> FunctionResults { get; set; } = new();

    /// <summary>
    /// Conversation variables state
    /// </summary>
    public Dictionary<string, object> ConversationVariables { get; set; } = new();

    /// <summary>
    /// SK configuration snapshot
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? SkConfiguration { get; set; }
}