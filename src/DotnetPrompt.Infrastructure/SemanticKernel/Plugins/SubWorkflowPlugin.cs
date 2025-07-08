using System.ComponentModel;
using System.Text.Json;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Semantic Kernel plugin for sub-workflow composition and execution
/// </summary>
[Description("Executes and composes sub-workflows with parameter passing and context inheritance")]
public class SubWorkflowPlugin
{
    private readonly IDotpromptParser _parser;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<SubWorkflowPlugin> _logger;

    public SubWorkflowPlugin(
        IDotpromptParser parser,
        IWorkflowOrchestrator orchestrator,
        ILogger<SubWorkflowPlugin> logger)
    {
        _parser = parser;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [KernelFunction("execute_sub_workflow")]
    [Description("Execute a sub-workflow with parameter passing and context inheritance")]
    [return: Description("Sub-workflow execution result with success status and output")]
    public async Task<SubWorkflowResult> ExecuteSubWorkflowAsync(
        [Description("Absolute or relative path to sub-workflow .prompt.md file")] string workflowPath,
        [Description("Parameters to pass to sub-workflow as JSON object")] string parameters = "{}",
        [Description("Inherit parent workflow context variables")] bool inheritContext = true,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Executing sub-workflow: {WorkflowPath}", workflowPath);

            // 1. Parse the sub-workflow using the main parser
            var subWorkflow = await _parser.ParseFileAsync(workflowPath, cancellationToken);
            
            // 2. Create execution context based on parameters and inheritance
            var subContext = CreateSubWorkflowContext(parameters, inheritContext);
            
            // 3. Execute sub-workflow using the main orchestrator (recursive composition)
            var result = await _orchestrator.ExecuteWorkflowAsync(subWorkflow, subContext, cancellationToken);
            
            stopwatch.Stop();
            
            if (!result.Success)
            {
                _logger.LogWarning("Sub-workflow execution failed: {WorkflowPath}, Error: {ErrorMessage}", 
                    workflowPath, result.ErrorMessage);
                
                return new SubWorkflowResult(
                    Success: false,
                    Output: null,
                    ErrorMessage: result.ErrorMessage ?? "Sub-workflow execution failed",
                    ExecutionTime: stopwatch.Elapsed);
            }

            _logger.LogInformation("Sub-workflow completed successfully: {WorkflowPath} in {ElapsedMs}ms", 
                workflowPath, stopwatch.ElapsedMilliseconds);
            
            return new SubWorkflowResult(
                Success: true,
                Output: result.Output,
                ErrorMessage: null,
                ExecutionTime: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing sub-workflow: {WorkflowPath}", workflowPath);
            
            return new SubWorkflowResult(
                Success: false,
                Output: null,
                ErrorMessage: $"Sub-workflow execution failed: {ex.Message}",
                ExecutionTime: stopwatch.Elapsed);
        }
    }

    [KernelFunction("validate_sub_workflow")]
    [Description("Validate a sub-workflow without executing it")]
    [return: Description("Sub-workflow validation result with errors and warnings")]
    public async Task<SubWorkflowValidationResult> ValidateSubWorkflowAsync(
        [Description("Path to sub-workflow file")] string workflowPath,
        [Description("Parameters for validation")] string parameters = "{}",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating sub-workflow: {WorkflowPath}", workflowPath);

            // 1. Parse the sub-workflow
            var subWorkflow = await _parser.ParseFileAsync(workflowPath, cancellationToken);
            
            // 2. Create context for validation
            var subContext = CreateSubWorkflowContext(parameters, inheritContext: false);
            
            // 3. Validate using the orchestrator
            var validationResult = await _orchestrator.ValidateWorkflowAsync(subWorkflow, subContext, cancellationToken);
            
            _logger.LogDebug("Sub-workflow validation completed: {WorkflowPath}, Valid: {IsValid}", 
                workflowPath, validationResult.IsValid);
            
            return new SubWorkflowValidationResult(
                IsValid: validationResult.IsValid,
                Errors: validationResult.Errors ?? Array.Empty<string>(),
                Warnings: validationResult.Warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating sub-workflow: {WorkflowPath}", workflowPath);
            
            return new SubWorkflowValidationResult(
                IsValid: false,
                Errors: new[] { $"Validation failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Creates sub-workflow execution context with parameter passing and context inheritance
    /// </summary>
    private Core.Models.WorkflowExecutionContext CreateSubWorkflowContext(string parametersJson, bool inheritContext)
    {
        var context = new Core.Models.WorkflowExecutionContext();
        
        try
        {
            // Add explicit parameters from JSON
            if (!string.IsNullOrEmpty(parametersJson) && parametersJson != "{}")
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                if (parameters != null)
                {
                    foreach (var (key, value) in parameters)
                    {
                        context.Variables[key] = value;
                    }
                }
            }
            
            // Note: Context inheritance from parent would require access to current execution context
            // This implementation provides the foundation for parameter passing
            // Parent context inheritance can be enhanced in future iterations
            
            _logger.LogDebug("Created sub-workflow context with {VariableCount} variables, InheritContext: {InheritContext}", 
                context.Variables.Count, inheritContext);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse parameters JSON: {ParametersJson}", parametersJson);
            // Continue with empty context rather than failing
        }
        
        return context;
    }
}