using System.Text.Json.Serialization;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// Vector Store record for conversation state persistence
/// </summary>
public class ConversationRecord
{
    /// <summary>
    /// Unique workflow execution identifier (Vector Store key)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Serialized ChatHistory JSON
    /// </summary>
    public string ChatHistoryJson { get; set; } = string.Empty;

    /// <summary>
    /// Workflow execution context JSON
    /// </summary>
    public string ExecutionContextJson { get; set; } = string.Empty;

    /// <summary>
    /// Workflow execution state JSON
    /// </summary>
    public string ExecutionStateJson { get; set; } = string.Empty;

    /// <summary>
    /// When this conversation was last modified
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    public string WorkflowFile { get; set; } = string.Empty;

    /// <summary>
    /// Workflow status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Summary of conversation for search purposes
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Vector Store record for workflow snapshot and compatibility validation
/// </summary>
public class WorkflowSnapshot
{
    /// <summary>
    /// Unique workflow identifier (Vector Store key)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    public string WorkflowFile { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of workflow content
    /// </summary>
    public string WorkflowHash { get; set; } = string.Empty;

    /// <summary>
    /// Full workflow content
    /// </summary>
    public string WorkflowContent { get; set; } = string.Empty;

    /// <summary>
    /// Semantic embedding of workflow for similarity comparison
    /// </summary>
    public ReadOnlyMemory<float>? WorkflowEmbedding { get; set; }

    /// <summary>
    /// When this snapshot was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Workflow metadata JSON
    /// </summary>
    public string WorkflowMetadata { get; set; } = string.Empty;
}

/// <summary>
/// Combined progress record for comprehensive workflow state
/// </summary>
public class WorkflowProgress
{
    /// <summary>
    /// Unique workflow execution identifier (Vector Store key)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Serialized ChatHistory JSON
    /// </summary>
    public string ChatHistoryJson { get; set; } = string.Empty;

    /// <summary>
    /// Workflow execution context JSON
    /// </summary>
    public string ExecutionContextJson { get; set; } = string.Empty;

    /// <summary>
    /// Workflow execution state JSON
    /// </summary>
    public string ExecutionStateJson { get; set; } = string.Empty;

    /// <summary>
    /// Last checkpoint timestamp
    /// </summary>
    public DateTimeOffset LastCheckpoint { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Workflow status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Original workflow file path
    /// </summary>
    public string WorkflowFile { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of workflow content
    /// </summary>
    public string WorkflowHash { get; set; } = string.Empty;

    /// <summary>
    /// Error information if workflow failed
    /// </summary>
    public string? ErrorInfo { get; set; }
}