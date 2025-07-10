using DotnetPrompt.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Interface for AI workflow resume state management.
/// Focuses exclusively on capturing state needed for intelligent workflow resumption without progress estimation.
/// </summary>
public interface IResumeStateManager
{
    /// <summary>
    /// Save resume state for a workflow execution
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="context">Current execution context</param>
    /// <param name="chatHistory">Semantic Kernel ChatHistory for conversation state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the save operation</returns>
    Task SaveResumeStateAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load resume state for a workflow execution
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution context and chat history if found, null otherwise</returns>
    Task<(WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadResumeStateAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track completion of a tool execution for resume context
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="completedTool">Completed tool information with AI reasoning</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the tracking operation</returns>
    Task TrackCompletedToolAsync(string workflowId, CompletedTool completedTool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available resumable workflow states
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resumable workflow metadata</returns>
    Task<IEnumerable<AIWorkflowResumeState>> ListAvailableWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate workflow compatibility for resume using content comparison
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="currentWorkflowContent">Current workflow content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed compatibility validation result</returns>
    Task<AIWorkflowResumeCompatibility> ValidateResumeCompatibilityAsync(string workflowId, string currentWorkflowContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old resume files based on retention policy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of files cleaned up</returns>
    Task<int> CleanupOldResumeFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Load resume state as AIWorkflowResumeState model
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resume state if found, null otherwise</returns>
    Task<AIWorkflowResumeState?> LoadResumeStateDataAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update context evolution with new insights or changes
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="contextChange">Context change information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the update operation</returns>
    Task UpdateContextEvolutionAsync(string workflowId, ContextChange contextChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add key insight to workflow resume state
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="insight">Key insight discovered</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task AddKeyInsightAsync(string workflowId, string insight, CancellationToken cancellationToken = default);
}