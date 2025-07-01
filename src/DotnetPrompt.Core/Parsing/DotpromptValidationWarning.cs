namespace DotnetPrompt.Core.Parsing;

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