using System.Diagnostics;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Application.Execution.Steps;

/// <summary>
/// Step executor for writing files
/// </summary>
public class FileWriteStepExecutor : BaseStepExecutor
{
    public FileWriteStepExecutor(ILogger<FileWriteStepExecutor> logger, IVariableResolver variableResolver)
        : base(logger, variableResolver)
    {
    }

    public override string StepType => "file_write";

    protected override string[] GetRequiredProperties() => new[] { "path", "content" };

    public override async Task<StepExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var filePath = ResolveProperty(step, "path", context);
            var content = ResolveProperty(step, "content", context);
            
            if (string.IsNullOrEmpty(filePath))
            {
                return new StepExecutionResult(false, null, "File path is required", stopwatch.Elapsed);
            }

            // Resolve relative paths against working directory
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
            }

            Logger.LogInformation("Writing file: {FilePath}", filePath);

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.LogInformation("Created directory: {Directory}", directory);
            }

            await File.WriteAllTextAsync(filePath, content ?? string.Empty, cancellationToken);
            
            Logger.LogInformation("Successfully wrote file {FilePath} ({Size} characters)", filePath, content?.Length ?? 0);

            return new StepExecutionResult(
                true, 
                filePath, // Return the file path as the result
                null, 
                stopwatch.Elapsed,
                new Dictionary<string, object>
                {
                    { "file_path", filePath },
                    { "content_size", content?.Length ?? 0 },
                    { "directory_created", !string.IsNullOrEmpty(directory) && !Directory.Exists(directory) }
                });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing file in step {StepName}", step.Name);
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

        // Additional validation for file write steps
        var filePath = ResolveProperty(step, "path", context);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            // Resolve relative paths
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(context.WorkingDirectory, filePath);
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

            // Check if directory is writable
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                try
                {
                    // Check if we can create the directory (this doesn't actually create it)
                    var directoryInfo = new DirectoryInfo(directory);
                    if (directoryInfo.Exists)
                    {
                        // Directory exists, check if we can write to it
                        var testFile = Path.Combine(directory, Path.GetRandomFileName());
                        try
                        {
                            await File.WriteAllTextAsync(testFile, "test", cancellationToken);
                            File.Delete(testFile);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Cannot write to directory {directory}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Cannot access directory {directory}: {ex.Message}");
                }
            }
        }

        return new StepValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.Count > 0 ? errors.ToArray() : null
        );
    }
}