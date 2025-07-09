using DotnetPrompt.Core.Models;
using FluentValidation;

namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Validates dotprompt workflows according to the dotprompt specification
/// </summary>
public class DotpromptValidator
{
    private readonly IValidator<DotpromptWorkflow> _workflowValidator;
    private readonly IValidator<DotpromptConfig> _configValidator;
    private readonly IValidator<DotpromptInput> _inputValidator;

    public DotpromptValidator()
    {
        _workflowValidator = new WorkflowValidator();
        _configValidator = new ConfigValidator();
        _inputValidator = new InputValidator();
    }

    /// <summary>
    /// Validates a dotprompt workflow
    /// </summary>
    /// <param name="workflow">Workflow to validate</param>
    /// <returns>Validation result</returns>
    public DotpromptValidationResult Validate(DotpromptWorkflow workflow)
    {
        var result = new DotpromptValidationResult();

        try
        {
            // Validate workflow structure
            var workflowValidation = _workflowValidator.Validate(workflow);
            if (!workflowValidation.IsValid)
            {
                foreach (var error in workflowValidation.Errors)
                {
                    result.Errors.Add(new DotpromptValidationError
                    {
                        Message = error.ErrorMessage,
                        Field = error.PropertyName,
                        ErrorCode = error.ErrorCode
                    });
                }
            }

            // Validate config if present
            if (workflow.Config != null)
            {
                var configValidation = _configValidator.Validate(workflow.Config);
                if (!configValidation.IsValid)
                {
                    foreach (var error in configValidation.Errors)
                    {
                        result.Errors.Add(new DotpromptValidationError
                        {
                            Message = error.ErrorMessage,
                            Field = $"config.{error.PropertyName}",
                            ErrorCode = error.ErrorCode
                        });
                    }
                }
            }

            // Validate input if present
            if (workflow.Input != null)
            {
                var inputValidation = _inputValidator.Validate(workflow.Input);
                if (!inputValidation.IsValid)
                {
                    foreach (var error in inputValidation.Errors)
                    {
                        result.Errors.Add(new DotpromptValidationError
                        {
                            Message = error.ErrorMessage,
                            Field = $"input.{error.PropertyName}",
                            ErrorCode = error.ErrorCode
                        });
                    }
                }
            }

            // Validate parameter references
            ValidateParameterReferences(workflow, result);

            // Validate tool dependencies
            ValidateToolDependencies(workflow, result);

            // Validate extension fields
            ValidateExtensionFields(workflow, result);

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new DotpromptValidationError
            {
                Message = $"Validation failed with exception: {ex.Message}",
                ErrorCode = "VALIDATION_EXCEPTION",
                Severity = ValidationSeverity.Critical
            });
            result.IsValid = false;
        }

        return result;
    }

    private void ValidateParameterReferences(DotpromptWorkflow workflow, DotpromptValidationResult result)
    {
        if (workflow.Content.ParameterReferences.Any() && workflow.Input?.Schema == null)
        {
            result.Warnings.Add(new DotpromptValidationWarning
            {
                Message = "Workflow references parameters but has no input schema defined",
                WarningCode = "MISSING_INPUT_SCHEMA"
            });
        }

        if (workflow.Input?.Schema != null)
        {
            foreach (var paramRef in workflow.Content.ParameterReferences)
            {
                if (!workflow.Input.Schema.ContainsKey(paramRef))
                {
                    result.Warnings.Add(new DotpromptValidationWarning
                    {
                        Message = $"Parameter '{paramRef}' is referenced in content but not defined in input schema",
                        WarningCode = "UNDEFINED_PARAMETER",
                        Field = paramRef
                    });
                }
            }
        }

        // Validate conflicting default values
        ValidateConflictingDefaults(workflow, result);
    }

    /// <summary>
    /// Validates that default values specified in multiple locations are consistent
    /// </summary>
    /// <param name="workflow">Workflow to validate</param>
    /// <param name="result">Validation result to add warnings to</param>
    private void ValidateConflictingDefaults(DotpromptWorkflow workflow, DotpromptValidationResult result)
    {
        if (workflow.Input?.Default == null || workflow.Input?.Schema == null)
            return;

        foreach (var defaultParam in workflow.Input.Default.Keys)
        {
            if (workflow.Input.Schema.TryGetValue(defaultParam, out var schema) 
                && schema.Default != null)
            {
                // Check if values are different
                var workflowDefault = workflow.Input.Default[defaultParam];
                var schemaDefault = schema.Default;
                
                if (!Equals(workflowDefault, schemaDefault))
                {
                    result.Warnings.Add(new DotpromptValidationWarning
                    {
                        Message = $"Parameter '{defaultParam}' has conflicting defaults: " +
                            $"input.default = '{workflowDefault}', input.schema.{defaultParam}.default = '{schemaDefault}'. " +
                            $"Schema-level default will take precedence per dotprompt specification.",
                        WarningCode = "CONFLICTING_DEFAULTS",
                        Field = defaultParam
                    });
                }
                else
                {
                    result.Warnings.Add(new DotpromptValidationWarning
                    {
                        Message = $"Parameter '{defaultParam}' has redundant defaults specified in both " +
                            $"input.default and input.schema.{defaultParam}.default. " +
                            $"Consider using only one location for clarity.",
                        WarningCode = "REDUNDANT_DEFAULTS",
                        Field = defaultParam
                    });
                }
            }
        }
    }

    private void ValidateToolDependencies(DotpromptWorkflow workflow, DotpromptValidationResult result)
    {
        if (workflow.Tools != null)
        {
            var knownTools = new HashSet<string>
            {
                "project-analysis",
                "build-test", 
                "file-system",
                "git-operations",
                "document-generation",
                "sub-workflow"
            };

            foreach (var tool in workflow.Tools)
            {
                if (!knownTools.Contains(tool))
                {
                    result.Warnings.Add(new DotpromptValidationWarning
                    {
                        Message = $"Unknown tool '{tool}' referenced",
                        WarningCode = "UNKNOWN_TOOL",
                        Field = "tools"
                    });
                }
            }
        }
    }

    private void ValidateExtensionFields(DotpromptWorkflow workflow, DotpromptValidationResult result)
    {
        // Validate MCP configuration
        if (workflow.Extensions.Mcp != null)
        {
            foreach (var mcpConfig in workflow.Extensions.Mcp)
            {
                if (string.IsNullOrEmpty(mcpConfig.Server))
                {
                    result.Errors.Add(new DotpromptValidationError
                    {
                        Message = "MCP server configuration must specify a server name",
                        Field = "dotnet-prompt.mcp.server",
                        ErrorCode = "MISSING_MCP_SERVER"
                    });
                }
            }
        }

        // Validate sub-workflow configuration
        if (workflow.Extensions.SubWorkflows != null)
        {
            foreach (var subWorkflow in workflow.Extensions.SubWorkflows)
            {
                if (string.IsNullOrEmpty(subWorkflow.Path))
                {
                    result.Errors.Add(new DotpromptValidationError
                    {
                        Message = "Sub-workflow configuration must specify a path",
                        Field = "dotnet-prompt.sub-workflows.path",
                        ErrorCode = "MISSING_SUBWORKFLOW_PATH"
                    });
                }
            }
        }
    }

    private class WorkflowValidator : AbstractValidator<DotpromptWorkflow>
    {
        public WorkflowValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => x.HasFrontmatter)
                .WithMessage("Workflow name is required when frontmatter is present");

            RuleFor(x => x.Model)
                .NotEmpty()
                .When(x => x.HasFrontmatter)
                .WithMessage("Model specification is required when frontmatter is present");

            RuleFor(x => x.Content.RawMarkdown)
                .NotEmpty()
                .WithMessage("Workflow must contain markdown content");
        }
    }

    private class ConfigValidator : AbstractValidator<DotpromptConfig>
    {
        public ConfigValidator()
        {
            RuleFor(x => x.Temperature)
                .InclusiveBetween(0.0, 2.0)
                .When(x => x.Temperature.HasValue)
                .WithMessage("Temperature must be between 0.0 and 2.0");

            RuleFor(x => x.MaxOutputTokens)
                .GreaterThan(0)
                .When(x => x.MaxOutputTokens.HasValue)
                .WithMessage("MaxOutputTokens must be greater than 0");

            RuleFor(x => x.TopP)
                .InclusiveBetween(0.0, 1.0)
                .When(x => x.TopP.HasValue)
                .WithMessage("TopP must be between 0.0 and 1.0");

            RuleFor(x => x.TopK)
                .GreaterThan(0)
                .When(x => x.TopK.HasValue)
                .WithMessage("TopK must be greater than 0");
        }
    }

    private class InputValidator : AbstractValidator<DotpromptInput>
    {
        public InputValidator()
        {
            RuleFor(x => x.Schema)
                .NotNull()
                .When(x => x.Default != null && x.Default.Any())
                .WithMessage("Input schema should be defined when default values are provided");
        }
    }
}