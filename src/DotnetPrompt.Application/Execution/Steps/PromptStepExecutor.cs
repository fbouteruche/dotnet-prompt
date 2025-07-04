using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution.Steps;

/// <summary>
/// Step executor for AI prompt execution
/// For MVP, this is a placeholder that simulates prompt execution
/// In the full implementation, this would integrate with the AI provider
/// </summary>
public class PromptStepExecutor : BaseStepExecutor
{
    public PromptStepExecutor(ILogger<PromptStepExecutor> logger, IVariableResolver variableResolver)
        : base(logger, variableResolver)
    {
    }

    public override string StepType => "prompt";

    protected override string[] GetRequiredProperties() => new[] { "prompt" };

    public override async Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var prompt = ResolveProperty(step, "prompt", context);
            
            if (string.IsNullOrEmpty(prompt))
            {
                return new StepExecutionResult(false, null, "Prompt is required", stopwatch.Elapsed);
            }

            Logger.LogInformation("Executing prompt step: {StepName}", step.Name);
            Logger.LogDebug("Prompt content: {Prompt}", prompt);

            // For MVP: Simulate prompt execution with a mock response
            // In the full implementation, this would call the AI provider
            await Task.Delay(100, cancellationToken); // Simulate processing time

            var mockResponse = $"[MOCK RESPONSE for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...]";
            
            Logger.LogInformation("Prompt step {StepName} completed successfully", step.Name);

            return new StepExecutionResult(
                true, 
                mockResponse, 
                null, 
                stopwatch.Elapsed,
                new Dictionary<string, object>
                {
                    { "prompt_length", prompt.Length },
                    { "response_length", mockResponse.Length },
                    { "is_mock", true }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing prompt step {StepName}", step.Name);
            return new StepExecutionResult(false, null, ex.Message, stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public override async Task<StepValidationResult> ValidateAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var baseValidation = await base.ValidateAsync(step, context, cancellationToken);
        var errors = baseValidation.Errors?.ToList() ?? new List<string>();

        // Additional validation for prompt steps
        var prompt = ResolveProperty(step, "prompt", context);
        
        if (string.IsNullOrEmpty(prompt))
        {
            errors.Add("Prompt content cannot be empty");
        }
        else if (prompt.Length > 10000) // Reasonable limit for MVP
        {
            errors.Add($"Prompt is too long ({prompt.Length} characters). Maximum allowed: 10000 characters");
        }

        return new StepValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.Count > 0 ? errors.ToArray() : null
        );
    }
}