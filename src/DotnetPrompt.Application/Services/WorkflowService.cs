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
    private readonly IWorkflowOrchestrator _workflowOrchestrator;

    public WorkflowService(
        ILogger<WorkflowService> logger, 
        IDotpromptParser parser, 
        IConfigurationService configurationService,
        IWorkflowOrchestrator workflowOrchestrator)
    {
        _logger = logger;
        _parser = parser;
        _configurationService = configurationService;
        _workflowOrchestrator = workflowOrchestrator;
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
            
            // Load configuration with workflow model
            // Use the working directory (context or current directory) for local configuration resolution
            // This ensures consistent behavior regardless of workflow file location
            var workingDirectory = !string.IsNullOrEmpty(options.Context) 
                ? Path.GetFullPath(options.Context) 
                : Environment.CurrentDirectory;
            
            var configuration = await _configurationService.LoadConfigurationAsync(
                workflowModel: workflow.Model,
                projectPath: workingDirectory,
                cancellationToken: cancellationToken);
            
            // Create execution context with configuration
            var context = new WorkflowExecutionContext
            {
                Options = options,
                WorkingDirectory = workingDirectory,
                Configuration = configuration
            };

            // Apply input defaults from workflow specification
            ApplyInputDefaults(workflow, context);

            // Set any context variables from options if needed
            if (!string.IsNullOrEmpty(options.Context))
            {
                context.SetVariable("context", options.Context);
            }

            // Execute the workflow using the orchestrator
            var result = await _workflowOrchestrator.ExecuteWorkflowAsync(workflow, context, cancellationToken);
            
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
            
            // Load configuration with workflow model for validation
            // Use working directory for local configuration resolution
            // This ensures consistent behavior regardless of workflow file location
            var workingDirectory = Environment.CurrentDirectory;
            var configuration = await _configurationService.LoadConfigurationAsync(
                workflowModel: workflow.Model,
                projectPath: workingDirectory,
                cancellationToken: cancellationToken);
            
            // Create execution context for validation with configuration
            var context = new WorkflowExecutionContext
            {
                WorkingDirectory = workingDirectory,
                Configuration = configuration
            };

            // Validate using the workflow orchestrator
            var engineValidation = await _workflowOrchestrator.ValidateWorkflowAsync(workflow, context, cancellationToken);
            
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

    /// <summary>
    /// Applies input default values from workflow specification to execution context
    /// Following dotprompt precedence: CLI parameters > Schema defaults > Workflow defaults
    /// </summary>
    /// <param name="workflow">The parsed workflow containing input defaults</param>
    /// <param name="context">The execution context to populate with default values</param>
    private void ApplyInputDefaults(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        if (workflow.Input == null) return;

        _logger.LogDebug("Applying input defaults from workflow specification");

        // Step 1: Collect all parameter names from both locations
        var allParameters = new HashSet<string>();
        
        // From workflow-level defaults
        if (workflow.Input.Default != null)
        {
            foreach (var param in workflow.Input.Default.Keys)
                allParameters.Add(param);
        }
        
        // From schema definitions
        if (workflow.Input.Schema != null)
        {
            foreach (var param in workflow.Input.Schema.Keys)
                allParameters.Add(param);
        }

        // Step 2: Resolve each parameter using dotprompt precedence hierarchy
        foreach (var paramName in allParameters)
        {
            // Skip if already set in context (CLI parameters take highest precedence)
            if (context.Variables.ContainsKey(paramName))
            {
                _logger.LogDebug("Parameter '{ParameterName}' already set in context, skipping defaults", paramName);
                continue;
            }

            object? resolvedValue = null;

            // Priority 1: Schema-level default (input.schema.{param}.default)
            if (workflow.Input.Schema?.TryGetValue(paramName, out var schema) == true 
                && schema.Default != null)
            {
                resolvedValue = schema.Default;
                _logger.LogDebug("Applied schema-level default for parameter '{ParameterName}': {Value}", 
                    paramName, resolvedValue);
            }
            
            // Priority 2: Workflow-level default (input.default.{param}) - only if not already resolved
            else if (workflow.Input.Default?.TryGetValue(paramName, out var workflowDefault) == true)
            {
                resolvedValue = workflowDefault;
                _logger.LogDebug("Applied workflow-level default for parameter '{ParameterName}': {Value}", 
                    paramName, resolvedValue);
            }

            // Apply resolved value to context
            if (resolvedValue != null)
            {
                context.SetVariable(paramName, resolvedValue);
            }
        }

        _logger.LogInformation("Applied defaults for {Count} parameters from workflow specification", 
            allParameters.Count(p => context.Variables.ContainsKey(p)));
    }
}
