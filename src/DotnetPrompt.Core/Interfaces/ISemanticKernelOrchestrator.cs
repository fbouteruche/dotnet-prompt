using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Semantic Kernel orchestrator interface (defined in Core but implemented in Infrastructure)
/// </summary>
public interface ISemanticKernelOrchestrator
{
    /// <summary>
    /// Executes a workflow using Semantic Kernel's function calling and planning capabilities
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context with variables and settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow using SK's built-in validation capabilities
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <param name="context">Execution context for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}