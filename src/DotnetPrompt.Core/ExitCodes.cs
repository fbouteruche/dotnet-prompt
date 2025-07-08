namespace DotnetPrompt.Core;

/// <summary>
/// Standard exit codes for the dotnet-prompt CLI tool
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success
    /// </summary>
    public const int Success = 0;
    
    /// <summary>
    /// General error
    /// </summary>
    public const int GeneralError = 1;
    
    /// <summary>
    /// Configuration error
    /// </summary>
    public const int ConfigurationError = 2;
    
    /// <summary>
    /// Workflow validation error
    /// </summary>
    public const int WorkflowValidationError = 3;
    
    /// <summary>
    /// Execution timeout
    /// </summary>
    public const int ExecutionTimeout = 4;
    
    /// <summary>
    /// Authentication error
    /// </summary>
    public const int AuthenticationError = 5;
    
    /// <summary>
    /// Network error
    /// </summary>
    public const int NetworkError = 6;
    
    /// <summary>
    /// Permission error
    /// </summary>
    public const int PermissionError = 7;
    
    /// <summary>
    /// Invalid arguments
    /// </summary>
    public const int InvalidArguments = 8;
    
    /// <summary>
    /// Validation error
    /// </summary>
    public const int ValidationError = 9;
    
    /// <summary>
    /// File not found error
    /// </summary>
    public const int FileNotFound = 10;
    
    /// <summary>
    /// Feature not available error
    /// </summary>
    public const int FeatureNotAvailable = 11;
    
    /// <summary>
    /// No progress found for resume
    /// </summary>
    public const int NoProgressFound = 12;
    
    /// <summary>
    /// Workflow execution failed
    /// </summary>
    public const int WorkflowExecutionFailed = 13;
    
    /// <summary>
    /// Invalid operation
    /// </summary>
    public const int InvalidOperation = 14;
    
    /// <summary>
    /// Ambiguous input requiring clarification
    /// </summary>
    public const int AmbiguousInput = 15;
    
    /// <summary>
    /// Unexpected error
    /// </summary>
    public const int UnexpectedError = 99;
}