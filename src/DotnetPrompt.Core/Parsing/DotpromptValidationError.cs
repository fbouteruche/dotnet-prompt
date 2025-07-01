namespace DotnetPrompt.Core.Parsing;

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