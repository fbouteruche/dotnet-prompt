using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution.Steps;

/// <summary>
/// Abstract base class for step executors
/// </summary>
public abstract class BaseStepExecutor : IStepExecutor
{
    protected readonly ILogger Logger;
    protected readonly IVariableResolver VariableResolver;

    protected BaseStepExecutor(ILogger logger, IVariableResolver variableResolver)
    {
        Logger = logger;
        VariableResolver = variableResolver;
    }

    public abstract string StepType { get; }

    public abstract Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default);

    public virtual Task<StepValidationResult> ValidateAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Basic validation
        if (string.IsNullOrEmpty(step.Name))
            errors.Add("Step name is required");

        if (step.Type != StepType)
            errors.Add($"Invalid step type. Expected: {StepType}, Actual: {step.Type}");

        // Validate required properties
        var requiredProperties = GetRequiredProperties();
        foreach (var property in requiredProperties)
        {
            if (!step.Properties.ContainsKey(property))
                errors.Add($"Required property '{property}' is missing");
        }

        // Validate variable references in properties
        foreach (var (key, value) in step.Properties)
        {
            if (value is string stringValue)
            {
                var validation = VariableResolver.ValidateTemplate(stringValue, context);
                if (!validation.IsValid && validation.Errors != null)
                {
                    errors.AddRange(validation.Errors.Select(e => $"Property '{key}': {e}"));
                }
            }
        }

        return Task.FromResult(new StepValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.Count > 0 ? errors.ToArray() : null
        ));
    }

    /// <summary>
    /// Gets the required properties for this step type
    /// </summary>
    protected abstract string[] GetRequiredProperties();

    /// <summary>
    /// Resolves variables in a property value
    /// </summary>
    protected string ResolveProperty(WorkflowStep step, string propertyName, WorkflowExecutionContext context, string? defaultValue = null)
    {
        var value = step.GetProperty<string>(propertyName, defaultValue);
        return value != null ? VariableResolver.ResolveVariables(value, context) : string.Empty;
    }
}