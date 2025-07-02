using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Core workflow execution engine interface
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow with the provided execution context
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="context">Execution context with variables and settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow for execution without running it
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <param name="context">Execution context for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<WorkflowValidationResult> ValidateAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for step execution
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// Type of step this executor handles
    /// </summary>
    string StepType { get; }

    /// <summary>
    /// Executes a workflow step
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <param name="context">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Step execution result</returns>
    Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a step without executing it
    /// </summary>
    /// <param name="step">The step to validate</param>
    /// <param name="context">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<StepValidationResult> ValidateAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for variable resolution and template processing
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    /// Resolves variables in a template string
    /// </summary>
    /// <param name="template">Template with {{variable}} references</param>
    /// <param name="context">Execution context with variables</param>
    /// <returns>Resolved string with variables substituted</returns>
    string ResolveVariables(string template, WorkflowExecutionContext context);

    /// <summary>
    /// Validates that all variables in a template can be resolved
    /// </summary>
    /// <param name="template">Template to validate</param>
    /// <param name="context">Execution context with variables</param>
    /// <returns>Validation result with any missing variables</returns>
    VariableValidationResult ValidateTemplate(string template, WorkflowExecutionContext context);

    /// <summary>
    /// Extracts variable references from a template
    /// </summary>
    /// <param name="template">Template to analyze</param>
    /// <returns>Set of variable names referenced in the template</returns>
    HashSet<string> ExtractVariableReferences(string template);
}