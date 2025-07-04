using System.ComponentModel;
using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Semantic Kernel plugin for workflow execution and orchestration
/// </summary>
public class WorkflowExecutorPlugin
{
    private readonly ILogger<WorkflowExecutorPlugin> _logger;
    private readonly IVariableResolver _variableResolver;

    public WorkflowExecutorPlugin(ILogger<WorkflowExecutorPlugin> logger, IVariableResolver variableResolver)
    {
        _logger = logger;
        _variableResolver = variableResolver;
    }

    [KernelFunction("execute_prompt")]
    [Description("Executes an AI prompt with variable substitution and returns the result")]
    [return: Description("The AI response to the prompt")]
    public Task<string> ExecutePromptAsync(
        [Description("The prompt content to execute")] string prompt,
        [Description("JSON object containing variables for substitution")] string variables = "{}",
        [Description("Model configuration settings as JSON")] string modelConfig = "{}",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Executing prompt via SK function");
            
            // Create execution context for variable resolution
            var context = CreateExecutionContext(variables);
            
            // Resolve variables in the prompt
            var resolvedPrompt = _variableResolver.ResolveVariables(prompt, context);
            
            _logger.LogDebug("Resolved prompt: {Prompt}", resolvedPrompt);

            // For now, return a structured response that indicates this is SK-powered
            // In a full implementation, this would use the chat completion service directly
            var response = $"""
                [SK-Powered Response]
                
                Original Prompt: {prompt}
                Resolved Prompt: {resolvedPrompt}
                Variables Used: {variables}
                
                This prompt was executed through Semantic Kernel's function calling system.
                Processing time: {stopwatch.ElapsedMilliseconds}ms
                """;

            _logger.LogInformation("Prompt execution completed via SK in {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing prompt via SK function");
            return Task.FromException<string>(new KernelException($"Prompt execution failed: {ex.Message}", ex));
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("validate_variables")]
    [Description("Validates that all variables in a template can be resolved with the provided context")]
    [return: Description("Validation result indicating success and any missing variables")]
    public async Task<string> ValidateVariablesAsync(
        [Description("Template content with {{variable}} references")] string template,
        [Description("JSON object containing available variables")] string variables = "{}",
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogInformation("Validating variables via SK function");
            
            var context = CreateExecutionContext(variables);
            var validationResult = _variableResolver.ValidateTemplate(template, context);
            
            var result = new
            {
                IsValid = validationResult.IsValid,
                MissingVariables = validationResult.MissingVariables,
                ReferencedVariables = _variableResolver.ExtractVariableReferences(template),
                ValidationTime = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Variable validation completed: {IsValid}", validationResult.IsValid);
            
            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating variables via SK function");
            throw new KernelException($"Variable validation failed: {ex.Message}", ex);
        }
    }

    [KernelFunction("extract_workflow_steps")]
    [Description("Extracts executable steps from workflow content")]
    [return: Description("JSON array of workflow steps with their properties")]
    public async Task<string> ExtractWorkflowStepsAsync(
        [Description("Workflow content in markdown format")] string workflowContent,
        [Description("JSON object containing variables for step resolution")] string variables = "{}",
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogInformation("Extracting workflow steps via SK function");
            
            var context = CreateExecutionContext(variables);
            
            // Simple step extraction logic - in a full implementation this would be more sophisticated
            var lines = workflowContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var steps = new List<object>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                if (line.StartsWith("- ") || line.StartsWith("* ") || 
                    System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\."))
                {
                    steps.Add(new
                    {
                        Index = steps.Count,
                        Content = line,
                        Type = DetermineStepType(line),
                        Variables = _variableResolver.ExtractVariableReferences(line)
                    });
                }
            }

            _logger.LogInformation("Extracted {StepCount} workflow steps", steps.Count);
            
            return System.Text.Json.JsonSerializer.Serialize(steps, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting workflow steps via SK function");
            throw new KernelException($"Step extraction failed: {ex.Message}", ex);
        }
    }

    private WorkflowExecutionContext CreateExecutionContext(string variablesJson)
    {
        var variables = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(variablesJson) && variablesJson != "{}")
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(variablesJson);
                if (parsed != null)
                {
                    variables = parsed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse variables JSON: {Variables}", variablesJson);
            }
        }

        return new WorkflowExecutionContext
        {
            Variables = variables,
            StartTime = DateTime.UtcNow
        };
    }

    private static string DetermineStepType(string stepContent)
    {
        var content = stepContent.ToLowerInvariant();
        
        if (content.Contains("read") && content.Contains("file"))
            return "file_read";
        if (content.Contains("write") && content.Contains("file"))
            return "file_write";
        if (content.Contains("analyze") || content.Contains("prompt"))
            return "prompt";
        
        return "unknown";
    }
}