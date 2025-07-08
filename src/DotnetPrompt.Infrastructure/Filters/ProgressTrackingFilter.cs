using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Filters;

/// <summary>
/// SK filter for automatic progress tracking during function execution
/// </summary>
public class ProgressTrackingFilter : IFunctionInvocationFilter
{
    private readonly IProgressManager _progressManager;
    private readonly ILogger<ProgressTrackingFilter> _logger;

    public ProgressTrackingFilter(IProgressManager progressManager, ILogger<ProgressTrackingFilter> logger)
    {
        _progressManager = progressManager;
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var functionName = $"{context.Function.PluginName}.{context.Function.Name}";
        var workflowId = GetWorkflowId(context);

        if (string.IsNullOrEmpty(workflowId))
        {
            // No workflow ID available, proceed without tracking
            await next(context);
            return;
        }

        _logger.LogDebug("Tracking function execution: {FunctionName} for workflow {WorkflowId}", functionName, workflowId);

        try
        {
            // Pre-execution checkpoint (optional - could save state before function execution)
            await SaveCheckpointAsync(workflowId, functionName, context);

            // Execute the function
            await next(context);

            // Post-execution progress update
            stopwatch.Stop();
            var stepProgress = new StepProgress
            {
                StepName = functionName,
                StepType = "function",
                Success = true,
                Duration = stopwatch.Elapsed,
                CompletedAt = DateTimeOffset.UtcNow,
                Result = context.Result?.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "plugin_name", context.Function.PluginName ?? "unknown" },
                    { "function_name", context.Function.Name },
                    { "execution_time_ms", stopwatch.ElapsedMilliseconds }
                }
            };

            await _progressManager.TrackStepCompletionAsync(workflowId, stepProgress);

            _logger.LogDebug("Successfully tracked completion of function {FunctionName} for workflow {WorkflowId} in {ElapsedMs}ms",
                functionName, workflowId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Track failure and preserve state for resume
            stopwatch.Stop();
            
            _logger.LogError(ex, "Function {FunctionName} failed for workflow {WorkflowId}", functionName, workflowId);

            var stepProgress = new StepProgress
            {
                StepName = functionName,
                StepType = "function",
                Success = false,
                Duration = stopwatch.Elapsed,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message,
                Metadata = new Dictionary<string, object>
                {
                    { "plugin_name", context.Function.PluginName ?? "unknown" },
                    { "function_name", context.Function.Name },
                    { "execution_time_ms", stopwatch.ElapsedMilliseconds },
                    { "error_type", ex.GetType().Name }
                }
            };

            await _progressManager.TrackStepCompletionAsync(workflowId, stepProgress);

            // Re-throw the exception to maintain normal error flow
            throw;
        }
    }

    /// <summary>
    /// Save a checkpoint before function execution (optional for fine-grained tracking)
    /// </summary>
    private Task SaveCheckpointAsync(string workflowId, string functionName, FunctionInvocationContext context)
    {
        try
        {
            // For now, we'll just log the checkpoint
            // In a more sophisticated implementation, we might save the full state here
            _logger.LogDebug("Checkpoint before executing function {FunctionName} for workflow {WorkflowId}", functionName, workflowId);
            
            // If we had access to WorkflowExecutionContext and ChatHistory here, we could save a full checkpoint:
            // await _progressManager.SaveProgressAsync(workflowId, executionContext, chatHistory);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save checkpoint for function {FunctionName} in workflow {WorkflowId}", functionName, workflowId);
            // Don't throw - checkpointing is optional
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extract workflow ID from function invocation context
    /// </summary>
    private static string? GetWorkflowId(FunctionInvocationContext context)
    {
        // Try to get workflow ID from kernel arguments
        if (context.Arguments.TryGetValue("workflow_id", out var workflowIdObj) && workflowIdObj != null)
        {
            return workflowIdObj.ToString();
        }

        // Try to get from function metadata
        // Note: KernelFunctionMetadata doesn't have a direct dictionary interface
        // This is a simplified approach - in practice, you might store workflow_id differently
        
        return null;
    }
}