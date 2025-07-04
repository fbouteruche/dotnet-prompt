using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel filter for workflow execution monitoring and error handling
/// </summary>
public class WorkflowExecutionFilter : IFunctionInvocationFilter
{
    private readonly ILogger<WorkflowExecutionFilter> _logger;

    public WorkflowExecutionFilter(ILogger<WorkflowExecutionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        
        _logger.LogInformation("SK Function invocation started: {PluginName}.{FunctionName}", pluginName, functionName);
        
        // Log input parameters (be careful with sensitive data)
        foreach (var parameter in context.Arguments)
        {
            var value = parameter.Value?.ToString();
            var truncatedValue = value?.Length > 100 ? value.Substring(0, 100) + "..." : value;
            _logger.LogDebug("SK Function parameter {ParameterName}: {Value}", parameter.Key, truncatedValue);
        }

        try
        {
            await next(context);
            
            // Log successful completion
            _logger.LogInformation("SK Function invocation completed: {PluginName}.{FunctionName} in {Duration}ms", 
                pluginName, functionName, stopwatch.ElapsedMilliseconds);
            
            // Log result (truncated for safety)
            if (context.Result.Value != null)
            {
                var resultValue = context.Result.Value.ToString();
                var truncatedResult = resultValue?.Length > 200 ? resultValue.Substring(0, 200) + "..." : resultValue;
                _logger.LogDebug("SK Function result: {Result}", truncatedResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SK Function invocation failed: {PluginName}.{FunctionName} after {Duration}ms", 
                pluginName, functionName, stopwatch.ElapsedMilliseconds);
            
            // Add context to the exception for better error reporting
            var enrichedException = new KernelException(
                $"Function {pluginName}.{functionName} failed: {ex.Message}", ex);
            
            throw enrichedException;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}