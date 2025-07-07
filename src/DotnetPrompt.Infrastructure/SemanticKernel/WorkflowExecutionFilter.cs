using System.Diagnostics;
using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Comprehensive SK filter for workflow execution monitoring, error handling, and security validation
/// Implements both IFunctionInvocationFilter and IPromptRenderFilter for complete SK pipeline coverage
/// Uses SK-native content safety features for enhanced security
/// </summary>
public class WorkflowExecutionFilter : IFunctionInvocationFilter, IPromptRenderFilter
{
    private readonly ILogger<WorkflowExecutionFilter> _logger;
    private readonly IPromptSafetyService? _promptSafetyService;
    private static readonly string[] SensitiveKeys = { "apikey", "api_key", "token", "password", "secret", "key" };

    public WorkflowExecutionFilter(
        ILogger<WorkflowExecutionFilter> logger,
        IPromptSafetyService? promptSafetyService = null)
    {
        _logger = logger;
        _promptSafetyService = promptSafetyService;
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
    /// Enhanced prompt safety validation using SK's built-in content safety features
    /// </summary>
    private async Task ValidatePromptSafety(PromptRenderContext context)
    {
        if (_promptSafetyService == null)
        {
            // Fallback to basic validation if safety service is not available
            await ValidatePromptSafetyBasic(context);
            return;
        }

        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        // Extract prompt content for validation
        var promptContent = ExtractPromptContent(context);
        if (string.IsNullOrEmpty(promptContent))
            return;

        try
        {
            var safetyResult = await _promptSafetyService.ValidatePromptAsync(promptContent, correlationId);
            
            if (!safetyResult.IsValid)
            {
                var issuesText = string.Join("; ", safetyResult.Issues);
                _logger.LogWarning("SK prompt safety validation flagged content for function {FunctionName}: {Issues} (Risk: {RiskLevel}) with correlation {CorrelationId}", 
                    context.Function.Name, issuesText, safetyResult.RiskLevel, correlationId);
                
                // For high-risk content, we could throw an exception or sanitize
                // For now, we log and continue with monitoring
                if (safetyResult.RiskLevel == PromptRiskLevel.High)
                {
                    _logger.LogError("High-risk prompt content detected for function {FunctionName}: {Recommendation} with correlation {CorrelationId}", 
                        context.Function.Name, safetyResult.Recommendation, correlationId);
                }
            }
            else
            {
                _logger.LogDebug("Prompt content passed SK safety validation for function {FunctionName} with correlation {CorrelationId}", 
                    context.Function.Name, correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during SK prompt safety validation for function {FunctionName} with correlation {CorrelationId}", 
                context.Function.Name, correlationId);
            
            // Fall back to basic validation
            await ValidatePromptSafetyBasic(context);
        }
    }

    /// <summary>
    /// Basic prompt safety validation fallback when SK service is not available
    /// </summary>
    private Task ValidatePromptSafetyBasic(PromptRenderContext context)
    {
        // Basic validation - check for obvious prompt injection patterns
        foreach (var arg in context.Arguments)
        {
            var value = arg.Value?.ToString()?.ToLowerInvariant();
            if (value != null && (value.Contains("ignore previous") || value.Contains("system prompt")))
            {
                _logger.LogWarning("Potential prompt injection detected in parameter {ParameterName} for function {FunctionName}", 
                    arg.Key, context.Function.Name);
                // In a production system, you might want to sanitize or reject the prompt
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extract prompt content from context for validation
    /// </summary>
    private static string ExtractPromptContent(PromptRenderContext context)
    {
        var contentBuilder = new System.Text.StringBuilder();
        
        foreach (var argument in context.Arguments)
        {
            var value = argument.Value?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                // Include arguments that might contain prompt content
                if (IsPromptArgument(argument.Key))
                {
                    contentBuilder.AppendLine(value);
                }
            }
        }
        
        return contentBuilder.ToString().Trim();
    }

    /// <summary>
    /// Determine if an argument likely contains prompt content
    /// </summary>
    private static bool IsPromptArgument(string argumentName)
    {
        var promptKeywords = new[] { "prompt", "input", "content", "text", "message", "query", "request" };
        var nameLower = argumentName.ToLowerInvariant();
        
        return promptKeywords.Any(keyword => nameLower.Contains(keyword));
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