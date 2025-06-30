namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Represents a workflow execution context
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Executes a workflow from the specified file
    /// </summary>
    /// <param name="workflowFilePath">Path to the workflow file</param>
    /// <param name="options">Execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteAsync(string workflowFilePath, WorkflowExecutionOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a workflow file
    /// </summary>
    /// <param name="workflowFilePath">Path to the workflow file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<WorkflowValidationResult> ValidateAsync(string workflowFilePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for workflow execution
/// </summary>
public record WorkflowExecutionOptions(
    string? Context = null,
    bool DryRun = false,
    TimeSpan? Timeout = null,
    bool Verbose = false
);

/// <summary>
/// Result of workflow execution
/// </summary>
public record WorkflowExecutionResult(
    bool Success,
    string? Output = null,
    string? ErrorMessage = null,
    TimeSpan ExecutionTime = default
);

/// <summary>
/// Result of workflow validation
/// </summary>
public record WorkflowValidationResult(
    bool IsValid,
    string[]? Errors = null,
    string[]? Warnings = null
)
{
    public static readonly WorkflowValidationResult Valid = new(true);
}
