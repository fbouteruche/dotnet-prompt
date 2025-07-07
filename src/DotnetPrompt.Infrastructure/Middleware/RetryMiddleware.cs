using System.Diagnostics;
using System.Net.Sockets;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Middleware;

/// <summary>
/// SK-aware retry middleware for handling transient failures
/// </summary>
public class RetryMiddleware : IFunctionInvocationFilter
{
    private readonly ILogger<RetryMiddleware> _logger;
    private readonly RetryOptions _options;

    public RetryMiddleware(ILogger<RetryMiddleware> logger, RetryOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new RetryOptions();
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        
        Exception? lastException = null;
        
        for (int attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    var delay = CalculateDelay(attempt);
                    _logger.LogWarning("Retrying function {PluginName}.{FunctionName} (attempt {Attempt}/{MaxAttempts}) after {Delay}ms with correlation {CorrelationId}",
                        pluginName, functionName, attempt, _options.MaxAttempts, delay.TotalMilliseconds, correlationId);
                    
                    await Task.Delay(delay);
                }

                await next(context);
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Function {PluginName}.{FunctionName} succeeded on retry attempt {Attempt} with correlation {CorrelationId}",
                        pluginName, functionName, attempt, correlationId);
                }
                
                return; // Success
            }
            catch (Exception ex) when (IsRetryableException(ex) && attempt < _options.MaxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Function {PluginName}.{FunctionName} failed on attempt {Attempt}/{MaxAttempts} with correlation {CorrelationId}",
                    pluginName, functionName, attempt, _options.MaxAttempts, correlationId);
            }
        }

        // All retry attempts failed
        _logger.LogError(lastException, "Function {PluginName}.{FunctionName} failed after {MaxAttempts} attempts with correlation {CorrelationId}",
            pluginName, functionName, _options.MaxAttempts, correlationId);

        if (lastException != null)
        {
            throw new WorkflowExecutionException(
                $"Function {pluginName}.{functionName} failed after {_options.MaxAttempts} retry attempts: {lastException.Message}",
                lastException,
                context.Function,
                context,
                correlationId);
        }
    }

    /// <summary>
    /// Determine if an exception is retryable
    /// </summary>
    private bool IsRetryableException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => IsRetryableHttpException(httpEx),
            TaskCanceledException => true, // Timeout
            TimeoutException => true,
            SocketException => true,
            _ => false
        };
    }

    /// <summary>
    /// Determine if an HTTP exception is retryable
    /// </summary>
    private bool IsRetryableHttpException(HttpRequestException httpException)
    {
        var message = httpException.Message.ToLowerInvariant();
        return message.Contains("timeout") ||
               message.Contains("503") ||
               message.Contains("502") ||
               message.Contains("429") ||
               message.Contains("rate limit");
    }

    /// <summary>
    /// Calculate delay for exponential backoff
    /// </summary>
    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_options.BaseDelayMs * Math.Pow(2, attempt - 2));
        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}

/// <summary>
/// Configuration options for retry behavior
/// </summary>
public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}