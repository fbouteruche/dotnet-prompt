using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Interface for parsing dotprompt workflow files
/// </summary>
public interface IDotpromptParser
{
    /// <summary>
    /// Parses a dotprompt workflow from a file path
    /// </summary>
    /// <param name="filePath">Path to the .prompt.md file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed workflow model</returns>
    Task<DotpromptWorkflow> ParseFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a dotprompt workflow from content string
    /// </summary>
    /// <param name="content">File content</param>
    /// <param name="filePath">Optional file path for context</param>
    /// <returns>Parsed workflow model</returns>
    DotpromptWorkflow ParseContent(string content, string? filePath = null);

    /// <summary>
    /// Validates a dotprompt workflow
    /// </summary>
    /// <param name="workflow">Workflow to validate</param>
    /// <returns>Validation result</returns>
    DotpromptValidationResult Validate(DotpromptWorkflow workflow);

    /// <summary>
    /// Validates a dotprompt workflow file
    /// </summary>
    /// <param name="filePath">Path to the .prompt.md file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<DotpromptValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of dotprompt workflow validation
/// </summary>
public class DotpromptValidationResult
{
    /// <summary>
    /// Whether the workflow is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<DotpromptValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<DotpromptValidationWarning> Warnings { get; set; } = new();

    public static DotpromptValidationResult Valid() => new() { IsValid = true };
    
    public static DotpromptValidationResult Invalid(params DotpromptValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Represents a validation error
/// </summary>
public class DotpromptValidationError
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Line number where the error occurred
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Column number where the error occurred
    /// </summary>
    public int? ColumnNumber { get; set; }

    /// <summary>
    /// Field or section where the error occurred
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Severity level of the error
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Represents a validation warning
/// </summary>
public class DotpromptValidationWarning
{
    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Warning code for programmatic handling
    /// </summary>
    public string? WarningCode { get; set; }

    /// <summary>
    /// Line number where the warning occurred
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Field or section where the warning occurred
    /// </summary>
    public string? Field { get; set; }
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Warning,
    Error,
    Critical
}