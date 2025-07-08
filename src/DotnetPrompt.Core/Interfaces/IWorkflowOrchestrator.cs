using DotnetPrompt.Core.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// AI-powered workflow orchestrator interface (framework-agnostic)
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes a workflow using AI orchestration capabilities
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context with variables and settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow using AI-powered validation capabilities
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <param name="context">Execution context for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume a workflow from a saved progress state
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="workflow">The workflow definition</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ResumeWorkflowAsync(string workflowId, DotpromptWorkflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the ChatHistory for a specific workflow execution
    /// </summary>
    /// <param name="workflowId">Workflow execution identifier</param>
    /// <returns>ChatHistory if available</returns>
    Task<ChatHistory> GetChatHistoryAsync(string workflowId);

    /// <summary>
    /// Save the ChatHistory for a specific workflow execution
    /// </summary>
    /// <param name="workflowId">Workflow execution identifier</param>
    /// <param name="chatHistory">ChatHistory to save</param>
    /// <returns>Task</returns>
    Task SaveChatHistoryAsync(string workflowId, ChatHistory chatHistory);
}