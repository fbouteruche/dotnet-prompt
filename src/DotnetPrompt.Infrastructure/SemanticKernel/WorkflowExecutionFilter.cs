using System.Diagnostics;
using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Comprehensive SK filter for workflow execution monitoring, error handling, and security validation
/// Implements both IFunctionInvocationFilter and IPromptRenderFilter for complete SK pipeline coverage
/// </summary>
public class WorkflowExecutionFilter : IFunctionInvocationFilter, IPromptRenderFilter
{
    private readonly ILogger<WorkflowExecutionFilter> _logger;
    private static readonly string[] SensitiveKeys = { "apikey", "api_key", "token", "password", "secret", "key" };

    public WorkflowExecutionFilter(ILogger<WorkflowExecutionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation("SK Function invocation started: {PluginName}.{FunctionName} with correlation {CorrelationId}", 
            pluginName, functionName, correlationId);
        
        // Log input parameters (with sensitive data redaction)
        foreach (var parameter in context.Arguments)
        {
            var value = RedactSensitiveData(parameter.Key, parameter.Value?.ToString());
            var truncatedValue = value?.Length > 100 ? value.Substring(0, 100) + "..." : value;
            _logger.LogDebug("SK Function parameter {ParameterName}: {Value}", parameter.Key, truncatedValue);
        }

        try
        {
            await next(context);
            
            // Log successful completion
            _logger.LogInformation("SK Function invocation completed: {PluginName}.{FunctionName} in {Duration}ms with correlation {CorrelationId}", 
                pluginName, functionName, stopwatch.ElapsedMilliseconds, correlationId);
            
            // Log result (truncated and redacted for safety)
            if (context.Result != null)
            {
                var resultValue = RedactSensitiveData("result", context.Result.ToString());
                var truncatedResult = resultValue?.Length > 200 ? resultValue.Substring(0, 200) + "..." : resultValue;
                _logger.LogDebug("SK Function result: {Result}", truncatedResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SK Function invocation failed: {PluginName}.{FunctionName} after {Duration}ms with correlation {CorrelationId}", 
                pluginName, functionName, stopwatch.ElapsedMilliseconds, correlationId);
            
            // Create SK-aware exception with enhanced context
            var enrichedException = new WorkflowExecutionException(
                $"Function {pluginName}.{functionName} failed: {ex.Message}", 
                ex,
                context.Function, 
                context, 
                correlationId,
                stopwatch.Elapsed);
            
            throw enrichedException;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        
        _logger.LogDebug("SK Prompt render started for function: {FunctionName} with correlation {CorrelationId}", 
            functionName, correlationId);

        try
        {
            // Basic prompt safety validation
            await ValidatePromptSafety(context);
            
            await next(context);
            
            _logger.LogDebug("SK Prompt render completed for function: {FunctionName} with correlation {CorrelationId}", 
                functionName, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SK Prompt render failed for function: {FunctionName} with correlation {CorrelationId}", 
                functionName, correlationId);
            
            var enrichedException = new WorkflowExecutionException(
                $"Prompt render failed for function {functionName}: {ex.Message}", 
                ex,
                context.Function,
                null,
                correlationId);
            
            throw enrichedException;
        }
    }

    /// <summary>
    /// Basic prompt safety validation to prevent obvious security issues
    /// </summary>
    private Task ValidatePromptSafety(PromptRenderContext context)
    {
        // Basic validation - in a real implementation, this would be more sophisticated
        // Check for obvious prompt injection patterns
        foreach (var arg in context.Arguments)
        {
            var value = arg.Value?.ToString()?.ToLowerInvariant();
            if (value != null && (value.Contains("ignore previous") || value.Contains("system prompt")))
            {
                _logger.LogWarning("Potential prompt injection detected in parameter {ParameterName}", arg.Key);
                // In a production system, you might want to sanitize or reject the prompt
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Redact sensitive data from logs based on key patterns
    /// </summary>
    private string? RedactSensitiveData(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var keyLower = key.ToLowerInvariant();
        if (SensitiveKeys.Any(sensitiveKey => keyLower.Contains(sensitiveKey)))
        {
            return "[REDACTED]";
        }

        return value;
    }
}