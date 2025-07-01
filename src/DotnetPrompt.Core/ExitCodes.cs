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
}