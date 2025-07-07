using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// Enhanced execution context for workflow operations with SK integration
/// </summary>
public class WorkflowExecutionContext
{
    /// <summary>
    /// Unique correlation ID for tracking operations across services
    /// </summary>
    public string CorrelationId { get; }
    
    /// <summary>
    /// The SK function being executed
    /// </summary>
    public KernelFunction? Function { get; set; }
    
    /// <summary>
    /// Start time of the operation
    /// </summary>
    public DateTime StartTime { get; }
    
    /// <summary>
    /// Execution duration (updated as operation progresses)
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - StartTime;
    
    /// <summary>
    /// Operation metadata and properties
    /// </summary>
    public Dictionary<string, object> Properties { get; } = new();
    
    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, double> Metrics { get; } = new();

    public WorkflowExecutionContext(string? correlationId = null)
    {
        CorrelationId = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a property to the context
    /// </summary>
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// Get a property from the context
    /// </summary>
    public T? GetProperty<T>(string key)
    {
        return Properties.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
    }

    /// <summary>
    /// Record a performance metric
    /// </summary>
    public void RecordMetric(string name, double value)
    {
        Metrics[name] = value;
    }
}