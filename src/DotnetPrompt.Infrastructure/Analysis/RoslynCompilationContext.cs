using Microsoft.CodeAnalysis;

namespace DotnetPrompt.Infrastructure.Analysis;

/// <summary>
/// Context for holding Roslyn compilation artifacts during analysis
/// Bridges the gap between Core domain models and Infrastructure Roslyn implementations
/// </summary>
public class RoslynCompilationContext
{
    /// <summary>
    /// The actual Roslyn compilation for semantic analysis
    /// </summary>
    public Microsoft.CodeAnalysis.Compilation? Compilation { get; set; }
    
    /// <summary>
    /// Whether the compilation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if compilation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}