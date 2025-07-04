using DotnetPrompt.Core.Interfaces;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Execution context for workflow processing
/// </summary>
public class WorkflowExecutionContext
{
    /// <summary>
    /// Variables available during execution
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Execution options
    /// </summary>
    public WorkflowExecutionOptions Options { get; set; } = new();

    /// <summary>
    /// Step execution history
    /// </summary>
    public List<StepExecutionHistory> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// Current step being executed (0-based index)
    /// </summary>
    public int CurrentStep { get; set; } = 0;

    /// <summary>
    /// Start time of execution
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Working directory for file operations
    /// </summary>
    public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;

    /// <summary>
    /// Whether advanced validation requiring AI provider connectivity is needed
    /// </summary>
    public bool RequireAdvancedValidation { get; set; } = false;

    /// <summary>
    /// Gets a variable value with type conversion
    /// </summary>
    /// <typeparam name="T">Type to convert to</typeparam>
    /// <param name="name">Variable name</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Variable value</returns>
    public T? GetVariable<T>(string name, T? defaultValue = default)
    {
        if (!Variables.TryGetValue(name, out var value))
            return defaultValue;

        if (value is T typedValue)
            return typedValue;

        if (value is string stringValue && typeof(T) != typeof(string))
        {
            try
            {
                return (T?)Convert.ChangeType(stringValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Sets a variable value
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    public void SetVariable(string name, object? value)
    {
        Variables[name] = value ?? string.Empty;
    }

    /// <summary>
    /// Creates a copy of the context for step execution
    /// </summary>
    /// <returns>Context copy</returns>
    public WorkflowExecutionContext Clone()
    {
        return new WorkflowExecutionContext
        {
            Variables = new Dictionary<string, object>(Variables),
            Options = Options,
            ExecutionHistory = new List<StepExecutionHistory>(ExecutionHistory),
            CurrentStep = CurrentStep,
            StartTime = StartTime,
            WorkingDirectory = WorkingDirectory,
            RequireAdvancedValidation = RequireAdvancedValidation
        };
    }
}

/// <summary>
/// Represents a workflow step for execution
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Name of the step
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of step (prompt, file_read, file_write, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Step properties/parameters
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Output variable name to store result
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Step execution order
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets a property value with type conversion
    /// </summary>
    /// <typeparam name="T">Type to convert to</typeparam>
    /// <param name="name">Property name</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Property value</returns>
    public T? GetProperty<T>(string name, T? defaultValue = default)
    {
        if (!Properties.TryGetValue(name, out var value))
            return defaultValue;

        if (value is T typedValue)
            return typedValue;

        if (value is string stringValue && typeof(T) != typeof(string))
        {
            try
            {
                return (T?)Convert.ChangeType(stringValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }
}

/// <summary>
/// Result of step execution
/// </summary>
public record StepExecutionResult(
    bool Success,
    object? Result = null,
    string? ErrorMessage = null,
    TimeSpan ExecutionTime = default,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Result of step validation
/// </summary>
public record StepValidationResult(
    bool IsValid,
    string[]? Errors = null,
    string[]? Warnings = null
);

/// <summary>
/// Historical record of step execution
/// </summary>
public class StepExecutionHistory
{
    /// <summary>
    /// Step name
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Step type
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Output variable set by this step
    /// </summary>
    public string? OutputVariable { get; set; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Result of variable validation
/// </summary>
public record VariableValidationResult(
    bool IsValid,
    HashSet<string>? MissingVariables = null,
    string[]? Errors = null
);