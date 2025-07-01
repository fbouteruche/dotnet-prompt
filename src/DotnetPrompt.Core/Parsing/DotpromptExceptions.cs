namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Exception thrown when parsing a dotprompt file fails
/// </summary>
public class DotpromptParseException : Exception
{
    /// <summary>
    /// Line number where the error occurred
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// Column number where the error occurred
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// File path where the error occurred
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; }

    public DotpromptParseException(string message) : base(message)
    {
    }

    public DotpromptParseException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DotpromptParseException(
        string message, 
        string? filePath = null, 
        int? lineNumber = null, 
        int? columnNumber = null,
        string? errorCode = null) : base(message)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        ErrorCode = errorCode;
    }

    public DotpromptParseException(
        string message, 
        Exception innerException,
        string? filePath = null, 
        int? lineNumber = null, 
        int? columnNumber = null,
        string? errorCode = null) : base(message, innerException)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when validating a dotprompt workflow fails
/// </summary>
public class DotpromptValidationException : Exception
{
    /// <summary>
    /// Validation errors that caused the exception
    /// </summary>
    public List<DotpromptValidationError> ValidationErrors { get; }

    public DotpromptValidationException(string message, List<DotpromptValidationError> validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors;
    }

    public DotpromptValidationException(string message, params DotpromptValidationError[] validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors.ToList();
    }
}