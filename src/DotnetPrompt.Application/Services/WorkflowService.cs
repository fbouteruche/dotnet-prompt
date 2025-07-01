using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Parsing;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Services;

/// <summary>
/// Basic implementation of workflow service for MVP
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly IDotpromptParser _parser;

    public WorkflowService(ILogger<WorkflowService> logger, IDotpromptParser parser)
    {
        _logger = logger;
        _parser = parser;
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(string workflowFilePath, WorkflowExecutionOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing workflow: {WorkflowFilePath}", workflowFilePath);
        
        try
        {
            if (options.DryRun)
            {
                _logger.LogInformation("Dry run mode - workflow validation only");
                var validationResult = await ValidateAsync(workflowFilePath, cancellationToken);
                return new WorkflowExecutionResult(validationResult.IsValid, 
                    validationResult.IsValid ? "Dry run completed successfully" : null,
                    validationResult.IsValid ? null : string.Join(", ", validationResult.Errors ?? Array.Empty<string>()));
            }

            // Parse the workflow to validate it can be loaded
            var workflow = await _parser.ParseFileAsync(workflowFilePath, cancellationToken);
            
            // TODO: Implement actual workflow execution in future tasks
            _logger.LogWarning("Workflow execution not yet implemented - this is the CLI foundation MVP");
            
            return new WorkflowExecutionResult(true, "CLI foundation established - workflow execution will be implemented in future tasks");
        }
        catch (DotpromptParseException ex)
        {
            _logger.LogError(ex, "Error parsing workflow file: {WorkflowFilePath}", workflowFilePath);
            return new WorkflowExecutionResult(false, ErrorMessage: $"Error parsing workflow file: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow: {WorkflowFilePath}", workflowFilePath);
            return new WorkflowExecutionResult(false, ErrorMessage: $"Error executing workflow: {ex.Message}");
        }
    }

    public async Task<WorkflowValidationResult> ValidateAsync(string workflowFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating workflow: {WorkflowFilePath}", workflowFilePath);
        
        try
        {
            var dotpromptResult = await _parser.ValidateFileAsync(workflowFilePath, cancellationToken);
            
            if (dotpromptResult.IsValid)
            {
                _logger.LogInformation("Workflow validation passed for: {WorkflowFilePath}", workflowFilePath);
                return WorkflowValidationResult.Valid;
            }

            // Convert dotprompt validation errors to workflow validation errors
            var errors = dotpromptResult.Errors.Select(e => e.Message).ToArray();
            var warnings = dotpromptResult.Warnings.Select(w => w.Message).ToArray();
            
            _logger.LogWarning("Workflow validation failed for: {WorkflowFilePath}. Errors: {Errors}", 
                workflowFilePath, string.Join("; ", errors));
                
            return new WorkflowValidationResult(false, errors, warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow file: {WorkflowFilePath}", workflowFilePath);
            return new WorkflowValidationResult(false, new[] { $"Error validating workflow file: {ex.Message}" });
        }
    }
}
