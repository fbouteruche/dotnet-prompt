using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<ConfigurationValidationError> Errors { get; set; } = new();
    public List<ConfigurationValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Configuration validation error
/// </summary>
public class ConfigurationValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>
/// Configuration validation warning
/// </summary>
public class ConfigurationValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}