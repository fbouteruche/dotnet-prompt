using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// SK-aware exception for workflow execution failures with enhanced context
/// </summary>
public class WorkflowExecutionException : Exception
{
    /// <summary>
    /// The SK function that failed
    /// </summary>
    public KernelFunction? Function { get; }
    
    /// <summary>
    /// The SK function invocation context
    /// </summary>
    public FunctionInvocationContext? Context { get; }
    
    /// <summary>
    /// Correlation ID for tracking across distributed systems
    /// </summary>
    public string? CorrelationId { get; }
    
    /// <summary>
    /// Execution duration before failure
    /// </summary>
    public TimeSpan? ExecutionDuration { get; }

    public WorkflowExecutionException(
        string message, 
        KernelFunction? function = null, 
        FunctionInvocationContext? context = null, 
        string? correlationId = null,
        TimeSpan? executionDuration = null) 
        : base(message)
    {
        Function = function;
        Context = context;
        CorrelationId = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        ExecutionDuration = executionDuration;
    }

    public WorkflowExecutionException(
        string message, 
        Exception innerException,
        KernelFunction? function = null, 
        FunctionInvocationContext? context = null, 
        string? correlationId = null,
        TimeSpan? executionDuration = null) 
        : base(message, innerException)
    {
        Function = function;
        Context = context;
        CorrelationId = correlationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        ExecutionDuration = executionDuration;
    }
}