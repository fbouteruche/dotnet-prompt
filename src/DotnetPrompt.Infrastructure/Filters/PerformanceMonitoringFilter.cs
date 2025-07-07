using System.Diagnostics;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Filters;

/// <summary>
/// SK filter for comprehensive performance monitoring and metrics collection
/// </summary>
public class PerformanceMonitoringFilter : IFunctionInvocationFilter, IPromptRenderFilter
{
    private readonly ILogger<PerformanceMonitoringFilter> _logger;
    
    // Performance thresholds for warnings
    private static readonly Dictionary<string, TimeSpan> PerformanceThresholds = new()
    {
        { "prompt_render", TimeSpan.FromMilliseconds(500) },
        { "function_execution", TimeSpan.FromSeconds(30) },
        { "ai_completion", TimeSpan.FromSeconds(60) }
    };

    public PerformanceMonitoringFilter(ILogger<PerformanceMonitoringFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        
        // Create execution context for metrics
        var executionContext = new WorkflowExecutionContext(correlationId);
        executionContext.Function = context.Function;
        executionContext.SetProperty("plugin_name", pluginName ?? "Unknown");
        executionContext.SetProperty("function_name", functionName);

        try
        {
            // Record start metrics
            RecordStartMetrics(executionContext, context);
            
            await next(context);
            
            // Record completion metrics
            RecordCompletionMetrics(executionContext, context, stopwatch.Elapsed);
            
            // Check performance thresholds
            CheckPerformanceThresholds(executionContext, stopwatch.Elapsed, correlationId);
        }
        catch (Exception ex)
        {
            // Record error metrics
            RecordErrorMetrics(executionContext, ex, stopwatch.Elapsed);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log comprehensive performance metrics
            LogPerformanceMetrics(executionContext, correlationId);
        }
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;

        try
        {
            await next(context);
            
            // Check render performance
            if (stopwatch.Elapsed > PerformanceThresholds["prompt_render"])
            {
                _logger.LogWarning("Slow prompt render detected for function {FunctionName}: {Duration}ms with correlation {CorrelationId}", 
                    functionName, stopwatch.ElapsedMilliseconds, correlationId);
            }
            
            _logger.LogDebug("Prompt render completed for function {FunctionName} in {Duration}ms with correlation {CorrelationId}", 
                functionName, stopwatch.ElapsedMilliseconds, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prompt render failed for function {FunctionName} after {Duration}ms with correlation {CorrelationId}", 
                functionName, stopwatch.ElapsedMilliseconds, correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Record metrics at the start of function execution
    /// </summary>
    private void RecordStartMetrics(WorkflowExecutionContext executionContext, FunctionInvocationContext context)
    {
        // Count of input parameters
        executionContext.RecordMetric("input_parameters_count", context.Arguments.Count);
        
        // Total size of input parameters (approximate)
        var totalInputSize = context.Arguments.Sum(arg => arg.Value?.ToString()?.Length ?? 0);
        executionContext.RecordMetric("input_size_bytes", totalInputSize);
        
        // Record system metrics
        executionContext.RecordMetric("memory_before_mb", GC.GetTotalMemory(false) / 1024.0 / 1024.0);
        executionContext.RecordMetric("gc_gen0_before", GC.CollectionCount(0));
        executionContext.RecordMetric("gc_gen1_before", GC.CollectionCount(1));
        executionContext.RecordMetric("gc_gen2_before", GC.CollectionCount(2));
    }

    /// <summary>
    /// Record metrics at the completion of function execution
    /// </summary>
    private void RecordCompletionMetrics(WorkflowExecutionContext executionContext, FunctionInvocationContext context, TimeSpan duration)
    {
        // Execution time metrics
        executionContext.RecordMetric("execution_time_ms", duration.TotalMilliseconds);
        
        // Output size metrics
        var outputSize = context.Result?.ToString()?.Length ?? 0;
        executionContext.RecordMetric("output_size_bytes", outputSize);
        
        // Memory metrics
        var memoryAfter = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        executionContext.RecordMetric("memory_after_mb", memoryAfter);
        executionContext.RecordMetric("memory_delta_mb", memoryAfter - executionContext.Metrics["memory_before_mb"]);
        
        // GC metrics
        executionContext.RecordMetric("gc_gen0_after", GC.CollectionCount(0));
        executionContext.RecordMetric("gc_gen1_after", GC.CollectionCount(1));
        executionContext.RecordMetric("gc_gen2_after", GC.CollectionCount(2));
        
        // Calculate GC pressure
        var gcPressure = (GC.CollectionCount(0) - executionContext.Metrics["gc_gen0_before"]) +
                        (GC.CollectionCount(1) - executionContext.Metrics["gc_gen1_before"]) +
                        (GC.CollectionCount(2) - executionContext.Metrics["gc_gen2_before"]);
        executionContext.RecordMetric("gc_pressure", gcPressure);
    }

    /// <summary>
    /// Record metrics when an error occurs
    /// </summary>
    private void RecordErrorMetrics(WorkflowExecutionContext executionContext, Exception exception, TimeSpan duration)
    {
        executionContext.RecordMetric("execution_time_ms", duration.TotalMilliseconds);
        executionContext.RecordMetric("error_occurred", 1);
        executionContext.SetProperty("error_type", exception.GetType().Name);
        executionContext.SetProperty("error_message", exception.Message);
    }

    /// <summary>
    /// Check if performance thresholds are exceeded and log warnings
    /// </summary>
    private void CheckPerformanceThresholds(WorkflowExecutionContext executionContext, TimeSpan duration, string correlationId)
    {
        var functionName = executionContext.GetProperty<string>("function_name");
        var pluginName = executionContext.GetProperty<string>("plugin_name");
        
        if (duration > PerformanceThresholds["function_execution"])
        {
            _logger.LogWarning("Slow function execution detected for {PluginName}.{FunctionName}: {Duration}ms with correlation {CorrelationId}", 
                pluginName, functionName, duration.TotalMilliseconds, correlationId);
        }
        
        // Check memory pressure
        if (executionContext.Metrics.TryGetValue("memory_delta_mb", out var memoryDelta) && memoryDelta > 100)
        {
            _logger.LogWarning("High memory usage detected for {PluginName}.{FunctionName}: {MemoryDelta}MB with correlation {CorrelationId}", 
                pluginName, functionName, memoryDelta, correlationId);
        }
        
        // Check GC pressure
        if (executionContext.Metrics.TryGetValue("gc_pressure", out var gcPressure) && gcPressure > 0)
        {
            _logger.LogWarning("Garbage collection pressure detected for {PluginName}.{FunctionName}: {GCPressure} collections with correlation {CorrelationId}", 
                pluginName, functionName, gcPressure, correlationId);
        }
    }

    /// <summary>
    /// Log comprehensive performance metrics
    /// </summary>
    private void LogPerformanceMetrics(WorkflowExecutionContext executionContext, string correlationId)
    {
        var functionName = executionContext.GetProperty<string>("function_name");
        var pluginName = executionContext.GetProperty<string>("plugin_name");
        
        _logger.LogInformation("Performance metrics for {PluginName}.{FunctionName} with correlation {CorrelationId}: " +
            "Duration={Duration}ms, InputSize={InputSize}bytes, OutputSize={OutputSize}bytes, " +
            "MemoryDelta={MemoryDelta}MB, GCPressure={GCPressure}",
            pluginName, functionName, correlationId,
            executionContext.Metrics.GetValueOrDefault("execution_time_ms", 0),
            executionContext.Metrics.GetValueOrDefault("input_size_bytes", 0),
            executionContext.Metrics.GetValueOrDefault("output_size_bytes", 0),
            executionContext.Metrics.GetValueOrDefault("memory_delta_mb", 0),
            executionContext.Metrics.GetValueOrDefault("gc_pressure", 0));
    }
}