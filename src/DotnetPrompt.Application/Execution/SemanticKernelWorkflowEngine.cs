using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution;

/// <summary>
/// Semantic Kernel-powered workflow engine that implements IWorkflowEngine
/// This replaces the custom execution engine with SK orchestration
/// </summary>
public class SemanticKernelWorkflowEngine : IWorkflowEngine
{
    private readonly ISemanticKernelOrchestrator _skOrchestrator;
    private readonly ILogger<SemanticKernelWorkflowEngine> _logger;

    public SemanticKernelWorkflowEngine(
        ISemanticKernelOrchestrator skOrchestrator,
        ILogger<SemanticKernelWorkflowEngine> logger)
    {
        _skOrchestrator = skOrchestrator;
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Executing workflow via Semantic Kernel: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Delegate to SK orchestrator
            var result = await _skOrchestrator.ExecuteWorkflowAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("SK workflow execution completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SK workflow execution for {WorkflowName}", workflow.Name);
            
            return new WorkflowExecutionResult(
                Success: false,
                Output: null,
                ErrorMessage: $"SK workflow execution failed: {ex.Message}",
                ExecutionTime: stopwatch.Elapsed);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<WorkflowValidationResult> ValidateAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating workflow via Semantic Kernel: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Delegate to SK orchestrator
            var result = await _skOrchestrator.ValidateWorkflowAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("SK workflow validation completed");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SK workflow validation for {WorkflowName}", workflow.Name);
            
            return new WorkflowValidationResult(
                IsValid: false,
                Errors: new[] { $"SK validation failed: {ex.Message}" },
                Warnings: null);
        }
    }
}