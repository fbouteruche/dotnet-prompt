using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution;

/// <summary>
/// Main workflow execution engine
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly IVariableResolver _variableResolver;
    private readonly Dictionary<string, IStepExecutor> _stepExecutors;

    public WorkflowEngine(
        ILogger<WorkflowEngine> logger,
        IVariableResolver variableResolver,
        IEnumerable<IStepExecutor> stepExecutors)
    {
        _logger = logger;
        _variableResolver = variableResolver;
        _stepExecutors = stepExecutors.ToDictionary(e => e.StepType, e => e);
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var executionStopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting workflow execution: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Extract and parse steps from the workflow
            var steps = await ExtractStepsFromWorkflow(workflow, context, cancellationToken);
            
            if (steps.Count == 0)
            {
                _logger.LogWarning("No executable steps found in workflow");
                return new WorkflowExecutionResult(true, "No steps to execute", null, executionStopwatch.Elapsed);
            }

            _logger.LogInformation("Found {StepCount} steps to execute", steps.Count);

            // Execute steps sequentially
            for (int i = 0; i < steps.Count; i++)
            {
                context.CurrentStep = i;
                var step = steps[i];
                
                _logger.LogInformation("Executing step {StepIndex}/{TotalSteps}: {StepName} ({StepType})", 
                    i + 1, steps.Count, step.Name, step.Type);

                var stepResult = await ExecuteStep(step, context, cancellationToken);
                
                // Record execution history
                var history = new StepExecutionHistory
                {
                    StepName = step.Name,
                    StepType = step.Type,
                    StartTime = DateTime.UtcNow - stepResult.ExecutionTime,
                    EndTime = DateTime.UtcNow,
                    Success = stepResult.Success,
                    ErrorMessage = stepResult.ErrorMessage,
                    OutputVariable = step.Output
                };
                
                context.ExecutionHistory.Add(history);

                if (!stepResult.Success)
                {
                    var errorMessage = $"Step '{step.Name}' failed: {stepResult.ErrorMessage}";
                    _logger.LogError("Workflow execution failed at step {StepIndex}: {ErrorMessage}", i + 1, errorMessage);
                    return new WorkflowExecutionResult(false, null, errorMessage, executionStopwatch.Elapsed);
                }

                // Store step result in output variable if specified
                if (!string.IsNullOrEmpty(step.Output))
                {
                    context.SetVariable(step.Output, stepResult.Result);
                    _logger.LogDebug("Set output variable {VariableName} = {Result}", step.Output, stepResult.Result);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Workflow execution cancelled at step {StepIndex}", i + 1);
                    return new WorkflowExecutionResult(false, null, "Execution was cancelled", executionStopwatch.Elapsed);
                }
            }

            var totalDuration = executionStopwatch.Elapsed;
            _logger.LogInformation("Workflow execution completed successfully in {Duration}ms", totalDuration.TotalMilliseconds);
            
            return new WorkflowExecutionResult(true, "Workflow executed successfully", null, totalDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during workflow execution");
            return new WorkflowExecutionResult(false, null, ex.Message, executionStopwatch.Elapsed);
        }
        finally
        {
            executionStopwatch.Stop();
        }
    }

    public async Task<WorkflowValidationResult> ValidateAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating workflow: {WorkflowName}", workflow.Name ?? "unnamed");
            
            var errors = new List<string>();
            var warnings = new List<string>();

            // Extract and validate steps
            var steps = await ExtractStepsFromWorkflow(workflow, context, cancellationToken);
            
            if (steps.Count == 0)
            {
                warnings.Add("No executable steps found in workflow");
            }

            // Validate each step
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                
                // Check if we have an executor for this step type
                if (!_stepExecutors.ContainsKey(step.Type))
                {
                    errors.Add($"Step {i + 1} ('{step.Name}'): Unsupported step type '{step.Type}'");
                    continue;
                }

                // Validate the step
                var executor = _stepExecutors[step.Type];
                var stepValidation = await executor.ValidateAsync(step, context, cancellationToken);
                
                if (!stepValidation.IsValid && stepValidation.Errors != null)
                {
                    foreach (var error in stepValidation.Errors)
                    {
                        errors.Add($"Step {i + 1} ('{step.Name}'): {error}");
                    }
                }

                if (stepValidation.Warnings != null)
                {
                    foreach (var warning in stepValidation.Warnings)
                    {
                        warnings.Add($"Step {i + 1} ('{step.Name}'): {warning}");
                    }
                }
            }

            _logger.LogInformation("Workflow validation completed. Errors: {ErrorCount}, Warnings: {WarningCount}", 
                errors.Count, warnings.Count);

            return new WorkflowValidationResult(
                IsValid: errors.Count == 0,
                Errors: errors.Count > 0 ? errors.ToArray() : null,
                Warnings: warnings.Count > 0 ? warnings.ToArray() : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workflow validation");
            return new WorkflowValidationResult(false, new[] { ex.Message });
        }
    }

    private async Task<StepExecutionResult> ExecuteStep(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        if (!_stepExecutors.TryGetValue(step.Type, out var executor))
        {
            return new StepExecutionResult(false, null, $"No executor found for step type: {step.Type}");
        }

        try
        {
            return await executor.ExecuteAsync(step, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step {StepName} of type {StepType}", step.Name, step.Type);
            return new StepExecutionResult(false, null, ex.Message);
        }
    }

    private async Task<List<WorkflowStep>> ExtractStepsFromWorkflow(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        // For MVP, we'll create mock steps based on the workflow content
        // In the full implementation, this would parse the markdown content to extract steps
        
        var steps = new List<WorkflowStep>();
        
        // For now, create a simple prompt step from the workflow content
        if (!string.IsNullOrEmpty(workflow.Content.RawMarkdown))
        {
            steps.Add(new WorkflowStep
            {
                Name = "main_prompt",
                Type = "prompt",
                Properties = new Dictionary<string, object>
                {
                    { "prompt", workflow.Content.RawMarkdown }
                },
                Output = "workflow_result",
                Order = 0
            });
        }

        // TODO: In the full implementation, parse the markdown content to extract:
        // - Explicit step definitions
        // - File operations
        // - Sub-workflow calls
        // - Tool invocations

        return await Task.FromResult(steps);
    }
}