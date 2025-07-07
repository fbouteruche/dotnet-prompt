using System.Collections.Concurrent;
using System.Diagnostics;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Middleware;

/// <summary>
/// SK-aware circuit breaker middleware for protecting against cascading failures
/// </summary>
public class CircuitBreakerMiddleware : IFunctionInvocationFilter
{
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly CircuitBreakerOptions _options;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitStates = new();

    public CircuitBreakerMiddleware(ILogger<CircuitBreakerMiddleware> logger, CircuitBreakerOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new CircuitBreakerOptions();
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        var circuitKey = $"{pluginName}.{functionName}";
        
        var state = _circuitStates.GetOrAdd(circuitKey, _ => new CircuitBreakerState());

        // Check circuit state
        if (state.State == CircuitState.Open)
        {
            // Check if it's time to attempt recovery
            if (DateTime.UtcNow - state.LastFailureTime > _options.OpenTimeout)
            {
                state.State = CircuitState.HalfOpen;
                _logger.LogInformation("Circuit breaker for {PluginName}.{FunctionName} moved to half-open state with correlation {CorrelationId}",
                    pluginName, functionName, correlationId);
            }
            else
            {
                _logger.LogWarning("Circuit breaker for {PluginName}.{FunctionName} is open, rejecting request with correlation {CorrelationId}",
                    pluginName, functionName, correlationId);
                
                throw new WorkflowExecutionException(
                    $"Circuit breaker is open for function {pluginName}.{functionName}",
                    context.Function,
                    context,
                    correlationId);
            }
        }

        try
        {
            await next(context);
            
            // Success - reset circuit if it was half-open
            if (state.State == CircuitState.HalfOpen)
            {
                state.Reset();
                _logger.LogInformation("Circuit breaker for {PluginName}.{FunctionName} reset to closed state with correlation {CorrelationId}",
                    pluginName, functionName, correlationId);
            }
            else if (state.State == CircuitState.Closed)
            {
                // Reset failure count on success
                state.FailureCount = 0;
            }
        }
        catch (Exception)
        {
            // Record failure
            var currentFailureCount = ++state.FailureCount;
            state.LastFailureTime = DateTime.UtcNow;

            if (state.State == CircuitState.HalfOpen)
            {
                // Failed in half-open state, back to open
                state.State = CircuitState.Open;
                _logger.LogWarning("Circuit breaker for {PluginName}.{FunctionName} failed in half-open state, moving to open with correlation {CorrelationId}",
                    pluginName, functionName, correlationId);
            }
            else if (currentFailureCount >= _options.FailureThreshold)
            {
                // Too many failures, open the circuit
                state.State = CircuitState.Open;
                _logger.LogError("Circuit breaker for {PluginName}.{FunctionName} opened due to {FailureCount} failures with correlation {CorrelationId}",
                    pluginName, functionName, currentFailureCount, correlationId);
            }

            throw;
        }
    }
}

/// <summary>
/// Configuration options for circuit breaker behavior
/// </summary>
public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Circuit breaker state for a specific function
/// </summary>
public class CircuitBreakerState
{
    public CircuitState State { get; set; } = CircuitState.Closed;
    public int FailureCount { get; set; } = 0;
    public DateTime LastFailureTime { get; set; } = DateTime.MinValue;

    public void Reset()
    {
        State = CircuitState.Closed;
        FailureCount = 0;
        LastFailureTime = DateTime.MinValue;
    }
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    Closed,   // Normal operation
    Open,     // Failing, reject all requests
    HalfOpen  // Testing if service has recovered
}