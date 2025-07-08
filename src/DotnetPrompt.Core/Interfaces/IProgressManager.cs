using DotnetPrompt.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Interface for workflow progress tracking and state management using Semantic Kernel
/// </summary>
public interface IProgressManager
{
    /// <summary>
    /// Save progress for a workflow execution using SK Vector Store
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="context">Current execution context</param>
    /// <param name="chatHistory">SK ChatHistory for conversation state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the save operation</returns>
    Task SaveProgressAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load progress for a workflow execution from SK Vector Store
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution context and chat history if found, null otherwise</returns>
    Task<(WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadProgressAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track completion of a workflow step
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="stepProgress">Step completion information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the tracking operation</returns>
    Task TrackStepCompletionAsync(string workflowId, StepProgress stepProgress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track failure of a workflow step
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="stepName">Name of the failed step</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the tracking operation</returns>
    Task TrackStepFailureAsync(string workflowId, string stepName, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available resumable workflow states
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of workflow identifiers that can be resumed</returns>
    Task<IEnumerable<string>> GetAvailableConversationStatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate workflow compatibility for resume using SK vector similarity
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="currentWorkflowContent">Current workflow content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if workflow is compatible for resume</returns>
    Task<bool> ValidateWorkflowCompatibilityAsync(string workflowId, string currentWorkflowContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old progress records
    /// </summary>
    /// <param name="olderThan">Remove records older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records cleaned up</returns>
    Task<int> CleanupOldProgressAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}