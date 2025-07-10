using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Resume;

/// <summary>
/// Captures resume state from Semantic Kernel's existing telemetry.
/// Does not produce additional telemetry - only consumes what SK already provides.
/// Implements the unified workflow resume system specification.
/// </summary>
public sealed class SKTelemetryResumeStateCapture : IFunctionInvocationFilter
{
    private readonly IResumeStateManager _resumeManager;
    private readonly ILogger<SKTelemetryResumeStateCapture> _logger;

    public SKTelemetryResumeStateCapture(
        IResumeStateManager resumeManager,
        ILogger<SKTelemetryResumeStateCapture> logger)
    {
        _resumeManager = resumeManager;
        _logger = logger;
    }

    /// <summary>
    /// Captures tool execution state for resume purposes when functions are invoked through SK
    /// </summary>
    /// <param name="context">Function invocation context from SK</param>
    /// <param name="next">Next filter in the pipeline</param>
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        var workflowId = ExtractWorkflowId(context);
        
        if (string.IsNullOrEmpty(workflowId)) 
        {
            // No workflow ID found, skip resume state capture but continue execution
            await next(context);
            return;
        }

        var startTime = DateTimeOffset.UtcNow;
        var functionName = context.Function.Name;

        _logger.LogDebug("Capturing tool execution for resume state: {FunctionName} in workflow {WorkflowId}",
            functionName, workflowId);

        try
        {
            await next(context);

            // Capture successful tool execution for resume state
            if (context.Result != null)
            {
                var completedTool = new CompletedTool
                {
                    FunctionName = functionName,
                    Parameters = ExtractParameters(context.Arguments),
                    Result = context.Result.ToString(),
                    ExecutedAt = startTime,
                    Success = true,
                    AIReasoning = ExtractReasoningFromContext(context)
                };

                await _resumeManager.TrackCompletedToolAsync(workflowId, completedTool);

                _logger.LogDebug("Successfully captured tool execution for resume: {FunctionName} in workflow {WorkflowId}",
                    functionName, workflowId);
            }
        }
        catch (Exception ex)
        {
            // Capture failed tool execution for resume context
            var failedTool = new CompletedTool
            {
                FunctionName = functionName,
                Parameters = ExtractParameters(context.Arguments),
                Result = $"ERROR: {ex.Message}",
                ExecutedAt = startTime,
                Success = false,
                AIReasoning = "Tool execution failed"
            };

            await _resumeManager.TrackCompletedToolAsync(workflowId, failedTool);

            _logger.LogWarning("Captured failed tool execution for resume: {FunctionName} in workflow {WorkflowId}, error: {Error}",
                functionName, workflowId, ex.Message);

            throw; // Re-throw to maintain SK pipeline behavior
        }
    }

    /// <summary>
    /// Extracts workflow ID from function invocation context
    /// </summary>
    /// <param name="context">Function invocation context</param>
    /// <returns>Workflow ID if found, null otherwise</returns>
    private static string? ExtractWorkflowId(FunctionInvocationContext context)
    {
        // Try multiple strategies to find workflow ID
        
        // Strategy 1: Direct workflow_id argument
        if (context.Arguments.ContainsName("workflow_id"))
        {
            return context.Arguments["workflow_id"]?.ToString();
        }

        // Strategy 2: Check function metadata for workflow context
        if (context.Function.Metadata.AdditionalProperties?.TryGetValue("workflow_id", out var workflowIdValue) == true)
        {
            return workflowIdValue?.ToString();
        }

        // Strategy 3: Check kernel or execution context for workflow ID
        // This would be set by the orchestrator when executing workflows
        if (context.Arguments.ContainsName("context"))
        {
            var contextValue = context.Arguments["context"];
            if (contextValue is WorkflowExecutionContext executionContext)
            {
                return executionContext.GetVariable<string>("workflow_id");
            }
        }

        // Strategy 4: Check for common workflow identification patterns
        foreach (var arg in context.Arguments)
        {
            if (arg.Key.Contains("workflow", StringComparison.OrdinalIgnoreCase) &&
                arg.Key.Contains("id", StringComparison.OrdinalIgnoreCase))
            {
                return arg.Value?.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts function parameters from kernel arguments for resume state
    /// </summary>
    /// <param name="arguments">Kernel arguments</param>
    /// <returns>Dictionary of parameter names and values</returns>
    private static Dictionary<string, object> ExtractParameters(KernelArguments arguments)
    {
        var parameters = new Dictionary<string, object>();

        foreach (var arg in arguments)
        {
            // Skip internal or sensitive parameters
            if (IsInternalParameter(arg.Key))
                continue;

            try
            {
                parameters[arg.Key] = arg.Value ?? string.Empty;
            }
            catch (Exception)
            {
                // If we can't serialize the parameter, store its type name
                parameters[arg.Key] = arg.Value?.GetType().Name ?? "null";
            }
        }

        return parameters;
    }

    /// <summary>
    /// Extracts AI reasoning from function invocation context
    /// </summary>
    /// <param name="context">Function invocation context</param>
    /// <returns>AI reasoning or default message</returns>
    private static string ExtractReasoningFromContext(FunctionInvocationContext context)
    {
        // Strategy 1: Check if reasoning was passed as an argument
        if (context.Arguments.ContainsName("reasoning"))
        {
            return context.Arguments["reasoning"]?.ToString() ?? "No reasoning provided";
        }

        // Strategy 2: Check function metadata for reasoning
        if (context.Function.Metadata.AdditionalProperties?.TryGetValue("ai_reasoning", out var reasoningValue) == true)
        {
            return reasoningValue?.ToString() ?? "No reasoning provided";
        }

        // Strategy 3: Generate contextual reasoning from function name and description
        var functionName = context.Function.Name;
        var description = context.Function.Metadata.Description;

        if (!string.IsNullOrEmpty(description))
        {
            return $"AI selected {functionName}: {description}";
        }

        // Fallback
        return $"AI tool selection: {functionName}";
    }

    /// <summary>
    /// Determines if a parameter should be excluded from resume state for security or size reasons
    /// </summary>
    /// <param name="parameterName">Parameter name to check</param>
    /// <returns>True if parameter should be excluded</returns>
    private static bool IsInternalParameter(string parameterName)
    {
        var internalPatterns = new[]
        {
            "password", "secret", "token", "key", "credential",
            "internal_", "_internal", "system_", "_system",
            "kernel", "_kernel", "context", "_context"
        };

        var paramLower = parameterName.ToLowerInvariant();
        
        return internalPatterns.Any(pattern => paramLower.Contains(pattern));
    }
}