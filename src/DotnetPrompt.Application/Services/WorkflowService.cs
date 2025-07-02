using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Services;

/// <summary>
/// Implementation of workflow service using the workflow execution engine
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly IDotpromptParser _parser;
    private readonly IConfigurationService _configurationService;
    private readonly IWorkflowEngine _workflowEngine;

    public WorkflowService(
        ILogger<WorkflowService> logger, 
        IDotpromptParser parser, 
        IConfigurationService configurationService,
        IWorkflowEngine workflowEngine)
    {
        _logger = logger;
        _parser = parser;
        _configurationService = configurationService;
        _workflowEngine = workflowEngine;
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

            // Parse the workflow
            var workflow = await _parser.ParseFileAsync(workflowFilePath, cancellationToken);
            
            // Create execution context
            var context = new WorkflowExecutionContext
            {
                Options = options,
                WorkingDirectory = Path.GetDirectoryName(workflowFilePath) ?? Environment.CurrentDirectory
            };

            // Set any context variables from options if needed
            if (!string.IsNullOrEmpty(options.Context))
            {
                context.SetVariable("context", options.Context);
            }

            // Execute the workflow using the engine
            var result = await _workflowEngine.ExecuteAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("Workflow execution completed. Success: {Success}, Duration: {Duration}ms", 
                result.Success, result.ExecutionTime.TotalMilliseconds);

            return result;
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
            // First validate using the parser
            var parserValidation = await _parser.ValidateFileAsync(workflowFilePath, cancellationToken);
            
            if (!parserValidation.IsValid)
            {
                var errors = parserValidation.Errors.Select(e => e.Message).ToArray();
                var warnings = parserValidation.Warnings.Select(w => w.Message).ToArray();
                return new WorkflowValidationResult(false, errors, warnings);
            }

            // Parse the workflow for execution validation
            var workflow = await _parser.ParseFileAsync(workflowFilePath, cancellationToken);
            
            // Create execution context for validation
            var context = new WorkflowExecutionContext
            {
                WorkingDirectory = Path.GetDirectoryName(workflowFilePath) ?? Environment.CurrentDirectory
            };

            // Validate using the workflow engine
            var engineValidation = await _workflowEngine.ValidateAsync(workflow, context, cancellationToken);
            
            _logger.LogInformation("Workflow validation completed. Valid: {IsValid}", engineValidation.IsValid);
            
            return engineValidation;
        }
        catch (DotpromptParseException ex)
        {
            _logger.LogError(ex, "Error parsing workflow file: {WorkflowFilePath}", workflowFilePath);
            return new WorkflowValidationResult(false, new[] { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating workflow: {WorkflowFilePath}", workflowFilePath);
            return new WorkflowValidationResult(false, new[] { ex.Message });
        }
    }
}
