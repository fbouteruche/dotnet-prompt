namespace DotnetPrompt.Core.Parsing;

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