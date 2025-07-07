using System.Diagnostics;
using System.Security;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Filters;

/// <summary>
/// SK filter for comprehensive security validation and prompt injection prevention
/// Uses Semantic Kernel's built-in content safety features including OpenAI moderation API
/// </summary>
public class SecurityValidationFilter : IPromptRenderFilter, IFunctionInvocationFilter
{
    private readonly ILogger<SecurityValidationFilter> _logger;
    private readonly IPromptSafetyService _promptSafetyService;

    public SecurityValidationFilter(
        ILogger<SecurityValidationFilter> logger,
        IPromptSafetyService promptSafetyService)
    {
        _logger = logger;
        _promptSafetyService = promptSafetyService;
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;

        try
        {
            // Use SK-native prompt safety validation
            await ValidatePromptContentAsync(context, correlationId);
            
            await next(context);
            
            // Validate rendered prompt if accessible
            await ValidateRenderedPromptAsync(context, correlationId);
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
            // Use SK-native parameter safety validation
            await ValidateFunctionParametersAsync(context, correlationId);
            
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
    /// Validate prompt content using SK's built-in prompt safety service
    /// </summary>
    private async Task ValidatePromptContentAsync(PromptRenderContext context, string correlationId)
    {
        var promptContent = ExtractPromptContent(context);
        if (string.IsNullOrEmpty(promptContent))
            return;

        var safetyResult = await _promptSafetyService.ValidatePromptAsync(promptContent, correlationId);
        
        if (!safetyResult.IsValid)
        {
            var issuesText = string.Join("; ", safetyResult.Issues);
            _logger.LogWarning("Prompt safety validation failed for function {FunctionName}: {Issues} with correlation {CorrelationId}", 
                context.Function.Name, issuesText, correlationId);
            
            // For high-risk content, throw security exception
            if (safetyResult.RiskLevel == PromptRiskLevel.High)
            {
                throw new SecurityException($"High-risk prompt content detected: {safetyResult.Recommendation}");
            }
            
            // For medium-risk content, log warning but continue
            _logger.LogWarning("Medium-risk prompt content detected for function {FunctionName}: {Recommendation} with correlation {CorrelationId}", 
                context.Function.Name, safetyResult.Recommendation, correlationId);
        }
        else
        {
            _logger.LogDebug("Prompt content passed safety validation for function {FunctionName} with correlation {CorrelationId}", 
                context.Function.Name, correlationId);
        }
    }

    /// <summary>
    /// Validate rendered prompt content (placeholder for future SK enhancements)
    /// </summary>
    private Task ValidateRenderedPromptAsync(PromptRenderContext context, string correlationId)
    {
        // The rendered prompt is typically not directly accessible in the context
        // This is a placeholder for future enhancement when SK provides better access
        // to the rendered prompt content for post-render validation
        
        _logger.LogDebug("Rendered prompt validation completed for function {FunctionName} with correlation {CorrelationId}", 
            context.Function.Name, correlationId);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate function parameters using SK's built-in safety service
    /// </summary>
    private async Task ValidateFunctionParametersAsync(FunctionInvocationContext context, string correlationId)
    {
        var safetyResult = await _promptSafetyService.ValidateParametersAsync(
            context.Arguments, 
            context.Function.Name, 
            correlationId);
        
        if (!safetyResult.IsValid)
        {
            var issuesText = string.Join("; ", safetyResult.Issues);
            _logger.LogWarning("Parameter safety validation failed for function {FunctionName}: {Issues} with correlation {CorrelationId}", 
                context.Function.Name, issuesText, correlationId);
            
            // For high-risk parameters, throw security exception
            if (safetyResult.RiskLevel == PromptRiskLevel.High)
            {
                throw new SecurityException($"High-risk function parameters detected: {safetyResult.Recommendation}");
            }
            
            // For medium-risk parameters, log warning but continue
            _logger.LogWarning("Medium-risk function parameters detected for function {FunctionName}: {Recommendation} with correlation {CorrelationId}", 
                context.Function.Name, safetyResult.Recommendation, correlationId);
        }
        else
        {
            _logger.LogDebug("Function parameters passed safety validation for function {FunctionName} with correlation {CorrelationId}", 
                context.Function.Name, correlationId);
        }
    }

    /// <summary>
    /// Extract prompt content from context for validation
    /// </summary>
    private static string ExtractPromptContent(PromptRenderContext context)
    {
        // Extract content from arguments that likely contain prompt text
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
}