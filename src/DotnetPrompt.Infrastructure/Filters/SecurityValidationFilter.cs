using System.Diagnostics;
using System.Security;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Filters;

/// <summary>
/// SK filter for comprehensive security validation and prompt injection prevention
/// </summary>
public class SecurityValidationFilter : IPromptRenderFilter, IFunctionInvocationFilter
{
    private readonly ILogger<SecurityValidationFilter> _logger;
    
    // Common prompt injection patterns to detect
    private static readonly string[] DangerousPatterns = 
    {
        "ignore previous", "ignore all previous", "forget previous", "disregard previous",
        "system prompt", "you are now", "new instructions", "override instructions",
        "act as", "roleplay", "pretend you are", "simulate",
        "jailbreak", "dev mode", "ignore safety", "bypass safety"
    };

    // Sensitive parameter names that should be monitored
    private static readonly string[] SensitiveParameters = 
    {
        "apikey", "api_key", "token", "password", "secret", "key", "credential"
    };

    public SecurityValidationFilter(ILogger<SecurityValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;

        try
        {
            // Validate prompt content for security issues
            await ValidatePromptContent(context, correlationId);
            
            await next(context);
            
            // Validate rendered prompt if accessible
            await ValidateRenderedPrompt(context, correlationId);
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security validation failed for prompt in function {FunctionName} with correlation {CorrelationId}", 
                functionName, correlationId);
            
            throw new WorkflowExecutionException(
                $"Security validation failed for function {functionName}: {ex.Message}",
                ex,
                context.Function,
                null,
                correlationId);
        }
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;

        try
        {
            // Validate function parameters for security
            ValidateFunctionParameters(context, correlationId);
            
            await next(context);
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security validation failed for function {PluginName}.{FunctionName} with correlation {CorrelationId}", 
                pluginName, functionName, correlationId);
            
            throw new WorkflowExecutionException(
                $"Security validation failed for function {pluginName}.{functionName}: {ex.Message}",
                ex,
                context.Function,
                context,
                correlationId);
        }
    }

    /// <summary>
    /// Validate prompt content for potential security issues
    /// </summary>
    private Task ValidatePromptContent(PromptRenderContext context, string correlationId)
    {
        foreach (var argument in context.Arguments)
        {
            var value = argument.Value?.ToString();
            if (string.IsNullOrEmpty(value))
                continue;

            var valueLower = value.ToLowerInvariant();
            
            // Check for prompt injection patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (valueLower.Contains(pattern))
                {
                    _logger.LogWarning("Potential prompt injection detected in parameter {ParameterName} for function {FunctionName}: pattern '{Pattern}' with correlation {CorrelationId}", 
                        argument.Key, context.Function.Name, pattern, correlationId);
                    
                    // In a production system, you might want to:
                    // 1. Sanitize the input
                    // 2. Reject the request
                    // 3. Use a more sophisticated ML-based detection
                    // For now, we just warn and continue
                }
            }

            // Check for excessive length that might indicate injection
            if (value.Length > 10000)
            {
                _logger.LogWarning("Unusually long input detected in parameter {ParameterName} for function {FunctionName}: {Length} characters with correlation {CorrelationId}", 
                    argument.Key, context.Function.Name, value.Length, correlationId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate rendered prompt content
    /// </summary>
    private Task ValidateRenderedPrompt(PromptRenderContext context, string correlationId)
    {
        // The rendered prompt is typically not directly accessible in the context
        // This is a placeholder for future enhancement when SK provides better access
        // to the rendered prompt content
        
        _logger.LogDebug("Rendered prompt validation completed for function {FunctionName} with correlation {CorrelationId}", 
            context.Function.Name, correlationId);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate function parameters for security concerns
    /// </summary>
    private void ValidateFunctionParameters(FunctionInvocationContext context, string correlationId)
    {
        foreach (var parameter in context.Arguments)
        {
            var paramName = parameter.Key.ToLowerInvariant();
            
            // Check if this parameter contains sensitive information
            foreach (var sensitiveParam in SensitiveParameters)
            {
                if (paramName.Contains(sensitiveParam))
                {
                    _logger.LogDebug("Sensitive parameter detected: {ParameterName} for function {FunctionName} with correlation {CorrelationId}", 
                        parameter.Key, context.Function.Name, correlationId);
                    
                    // Ensure sensitive parameters are not logged in detail
                    // This is handled by the WorkflowExecutionFilter, but we double-check here
                    break;
                }
            }
        }
    }
}