using System.ComponentModel;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// Result of sub-workflow execution
/// </summary>
public record SubWorkflowResult(
    [property: Description("Whether the sub-workflow executed successfully")]
    bool Success,
    
    [property: Description("Output content from the sub-workflow")]
    string? Output,
    
    [property: Description("Error message if execution failed")]
    string? ErrorMessage,
    
    [property: Description("Time taken to execute the sub-workflow")]
    TimeSpan ExecutionTime,
    
    [property: Description("Additional metadata from sub-workflow execution")]
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Result of sub-workflow validation without execution
/// </summary>
public record SubWorkflowValidationResult(
    [property: Description("Whether the sub-workflow is valid")]
    bool IsValid,
    
    [property: Description("Validation errors if any")]
    string[] Errors,
    
    [property: Description("Validation warnings if any")]
    string[]? Warnings = null);