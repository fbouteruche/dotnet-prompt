using System.Text.Json.Serialization;
using DotnetPrompt.Core.Models.Enums;

namespace DotnetPrompt.Core.Models.RoslynAnalysis;

/// <summary>
/// Result of a compilation operation - domain model without external dependencies
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// Whether the compilation was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    /// <summary>
    /// Strategy used for compilation
    /// </summary>
    [JsonPropertyName("strategy_used")]
    public CompilationStrategy StrategyUsed { get; set; }
    
    /// <summary>
    /// Whether fallback strategy was used
    /// </summary>
    [JsonPropertyName("fallback_used")]
    public bool FallbackUsed { get; set; }
    
    /// <summary>
    /// Reason for fallback if used
    /// </summary>
    [JsonPropertyName("fallback_reason")]
    public string? FallbackReason { get; set; }
    
    /// <summary>
    /// Compilation time in milliseconds
    /// </summary>
    [JsonPropertyName("compilation_time_ms")]
    public long CompilationTimeMs { get; set; }
    
    /// <summary>
    /// Error message if compilation failed
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Compilation diagnostics summary
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public DiagnosticsSummary Diagnostics { get; set; } = new();
    
    /// <summary>
    /// Workspace diagnostics if available
    /// </summary>
    [JsonPropertyName("workspace_diagnostics")]
    public List<WorkspaceDiagnosticInfo> WorkspaceDiagnostics { get; set; } = new();
    
    /// <summary>
    /// Project metadata extracted during compilation
    /// </summary>
    [JsonPropertyName("project_metadata")]
    public Dictionary<string, object> ProjectMetadata { get; set; } = new();
    
    /// <summary>
    /// Target framework used for compilation
    /// </summary>
    [JsonPropertyName("target_framework")]
    public string? TargetFramework { get; set; }
    
    /// <summary>
    /// Assembly name of the compiled project
    /// </summary>
    [JsonPropertyName("assembly_name")]
    public string? AssemblyName { get; set; }
    
    /// <summary>
    /// Internal reference to the Roslyn compilation (for compatibility)
    /// </summary>
    [JsonIgnore]
    public Microsoft.CodeAnalysis.Compilation? Compilation { get; set; }
    
    /// <summary>
    /// Default constructor
    /// </summary>
    public CompilationResult()
    {
    }
    
    /// <summary>
    /// Constructor for successful compilation
    /// </summary>
    /// <param name="strategy">Strategy used for compilation</param>
    /// <param name="assemblyName">Name of the compiled assembly</param>
    public CompilationResult(CompilationStrategy strategy, string? assemblyName = null)
    {
        Success = true;
        StrategyUsed = strategy;
        AssemblyName = assemblyName;
    }
}

/// <summary>
/// Summary of compilation diagnostics
/// </summary>
public class DiagnosticsSummary
{
    /// <summary>
    /// Number of error diagnostics
    /// </summary>
    [JsonPropertyName("error_count")]
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Number of warning diagnostics
    /// </summary>
    [JsonPropertyName("warning_count")]
    public int WarningCount { get; set; }
    
    /// <summary>
    /// Number of info diagnostics
    /// </summary>
    [JsonPropertyName("info_count")]
    public int InfoCount { get; set; }
    
    /// <summary>
    /// Sample of critical errors (up to 10)
    /// </summary>
    [JsonPropertyName("critical_errors")]
    public List<DiagnosticInfo> CriticalErrors { get; set; } = new();
    
    /// <summary>
    /// Sample of significant warnings (up to 10)
    /// </summary>
    [JsonPropertyName("significant_warnings")]
    public List<DiagnosticInfo> SignificantWarnings { get; set; } = new();
}

/// <summary>
/// Information about a diagnostic message
/// </summary>
public class DiagnosticInfo
{
    /// <summary>
    /// Diagnostic ID (e.g., CS0001)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Diagnostic message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity level
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// Source file path if available
    /// </summary>
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Line number if available
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; set; }
    
    /// <summary>
    /// Column number if available
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; set; }
}

/// <summary>
/// Information about workspace diagnostics
/// </summary>
public class WorkspaceDiagnosticInfo
{
    /// <summary>
    /// Diagnostic kind
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;
    
    /// <summary>
    /// Diagnostic message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated project if available
    /// </summary>
    [JsonPropertyName("project_path")]
    public string? ProjectPath { get; set; }
}