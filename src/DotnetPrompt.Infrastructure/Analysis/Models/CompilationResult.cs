using Microsoft.CodeAnalysis;

namespace DotnetPrompt.Infrastructure.Analysis.Models;

/// <summary>
/// Result of a compilation operation
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// The Roslyn compilation object
    /// </summary>
    public Microsoft.CodeAnalysis.Compilation? Compilation { get; set; }
    
    /// <summary>
    /// Whether the compilation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Strategy used for compilation
    /// </summary>
    public CompilationStrategy StrategyUsed { get; set; }
    
    /// <summary>
    /// Whether fallback strategy was used
    /// </summary>
    public bool FallbackUsed { get; set; }
    
    /// <summary>
    /// Reason for fallback if used
    /// </summary>
    public string? FallbackReason { get; set; }
    
    /// <summary>
    /// Compilation time in milliseconds
    /// </summary>
    public long CompilationTimeMs { get; set; }
    
    /// <summary>
    /// Error message if compilation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Compilation diagnostics
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics { get; set; } = Enumerable.Empty<Diagnostic>();

    public CompilationResult()
    {
    }
    
    public CompilationResult(Microsoft.CodeAnalysis.Compilation? compilation, CompilationStrategy strategy)
    {
        Compilation = compilation;
        StrategyUsed = strategy;
        Success = compilation != null;
    }
}

/// <summary>
/// Options for compilation operations
/// </summary>
public class AnalysisCompilationOptions
{
    /// <summary>
    /// MSBuild workspace timeout in milliseconds
    /// </summary>
    public int MSBuildTimeout { get; set; } = 30000;
    
    /// <summary>
    /// Whether to fallback to custom compilation if MSBuild fails
    /// </summary>
    public bool FallbackToCustom { get; set; } = true;
    
    /// <summary>
    /// Use lightweight mode for custom compilation
    /// </summary>
    public bool LightweightMode { get; set; } = false;
    
    /// <summary>
    /// Specific target framework to compile for
    /// </summary>
    public string? TargetFramework { get; set; }
    
    /// <summary>
    /// Whether to include test projects
    /// </summary>
    public bool IncludeTests { get; set; } = true;
    
    /// <summary>
    /// Whether to exclude generated code
    /// </summary>
    public bool ExcludeGenerated { get; set; } = true;
}