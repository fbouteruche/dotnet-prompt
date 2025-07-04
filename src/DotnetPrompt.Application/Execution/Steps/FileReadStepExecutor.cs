using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution.Steps;

/// <summary>
/// Step executor for reading files
/// </summary>
public class FileReadStepExecutor : BaseStepExecutor
{
    public FileReadStepExecutor(ILogger<FileReadStepExecutor> logger, IVariableResolver variableResolver)
        : base(logger, variableResolver)
    {
    }

    public override string StepType => "file_read";

    protected override string[] GetRequiredProperties() => new[] { "path" };

    public override async Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var filePath = ResolveProperty(step, "path", context);
            
            if (string.IsNullOrEmpty(filePath))
            {
                return new StepExecutionResult(false, null, "File path is required", stopwatch.Elapsed);
            }

            // Resolve relative paths against working directory
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
            }

            Logger.LogInformation("Reading file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return new StepExecutionResult(false, null, $"File not found: {filePath}", stopwatch.Elapsed);
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            Logger.LogInformation("Successfully read file {FilePath} ({Size} characters)", filePath, content.Length);

            return new StepExecutionResult(
                true, 
                content, 
                null, 
                stopwatch.Elapsed,
                new Dictionary<string, object>
                {
                    { "file_path", filePath },
                    { "file_size", content.Length },
                    { "file_exists", true }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reading file in step {StepName}", step.Name);
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

        // Additional validation for file read steps
        var filePath = ResolveProperty(step, "path", context);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            // Resolve relative paths
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
            }

            // Check if file exists (warning only, not error)
            if (!File.Exists(filePath))
            {
                // This is a warning, not an error, since the file might be created by a previous step
                Logger.LogWarning("File does not exist at validation time: {FilePath}", filePath);
            }

            // Validate path format
            try
            {
                Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                errors.Add($"Invalid file path format: {ex.Message}");
            }
        }

        return new StepValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.Count > 0 ? errors.ToArray() : null
        );
    }
}