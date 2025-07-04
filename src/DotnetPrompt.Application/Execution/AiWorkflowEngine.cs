using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution;

/// <summary>
/// AI-powered workflow engine that implements IWorkflowEngine using an orchestrator
/// This implementation is framework-agnostic - the orchestrator can be SK-powered or use other AI frameworks
/// </summary>
public class AiWorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<AiWorkflowEngine> _logger;

    public AiWorkflowEngine(
        IWorkflowOrchestrator orchestrator,
        ILogger<AiWorkflowEngine> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing workflow via AI orchestrator: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Delegate to the orchestrator (could be SK-powered or another AI framework)
            var result = await _orchestrator.ExecuteWorkflowAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("AI workflow execution completed: {Success}", result.Success);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI workflow execution for {WorkflowName}", workflow.Name);
            
            return new WorkflowExecutionResult(
                Success: false,
                Output: null,
                ErrorMessage: $"AI workflow execution failed: {ex.Message}",
                ExecutionTime: TimeSpan.Zero);
        }
    }

    public async Task<WorkflowValidationResult> ValidateAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating workflow via AI orchestrator: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Delegate to the orchestrator for validation
            var result = await _orchestrator.ValidateWorkflowAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("AI workflow validation completed: {IsValid}", result.IsValid);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI workflow validation for {WorkflowName}", workflow.Name);
            
            return new WorkflowValidationResult(
                IsValid: false,
                Errors: new[] { $"AI workflow validation failed: {ex.Message}" },
                Warnings: null);
        }
    }
}