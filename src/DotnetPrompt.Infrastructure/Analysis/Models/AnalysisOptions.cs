namespace DotnetPrompt.Infrastructure.Analysis.Models;

/// <summary>
/// Configuration options for Roslyn analysis
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Analysis depth level
    /// </summary>
    public AnalysisDepth AnalysisDepth { get; set; } = AnalysisDepth.Standard;
    
    /// <summary>
    /// Semantic analysis depth level
    /// </summary>
    public SemanticAnalysisDepth SemanticDepth { get; set; } = SemanticAnalysisDepth.None;
    
    /// <summary>
    /// Compilation strategy to use
    /// </summary>
    public CompilationStrategy CompilationStrategy { get; set; } = CompilationStrategy.Auto;
    
    /// <summary>
    /// Include dependency analysis
    /// </summary>
    public bool IncludeDependencies { get; set; } = true;
    
    /// <summary>
    /// Include code quality metrics
    /// </summary>
    public bool IncludeMetrics { get; set; } = true;
    
    /// <summary>
    /// Include architectural pattern detection
    /// </summary>
    public bool IncludePatterns { get; set; } = false;
    
    /// <summary>
    /// Include security vulnerability scanning
    /// </summary>
    public bool IncludeVulnerabilities { get; set; } = false;
    
    /// <summary>
    /// Specific target framework to analyze
    /// </summary>
    public string? TargetFramework { get; set; }
    
    /// <summary>
    /// Maximum recursion depth for references
    /// </summary>
    public int MaxDepth { get; set; } = 5;
    
    /// <summary>
    /// Exclude auto-generated code from analysis
    /// </summary>
    public bool ExcludeGenerated { get; set; } = true;
    
    /// <summary>
    /// Include test projects in analysis
    /// </summary>
    public bool IncludeTests { get; set; } = true;
    
    /// <summary>
    /// MSBuild workspace timeout in milliseconds
    /// </summary>
    public int MSBuildTimeout { get; set; } = 30000;
    
    /// <summary>
    /// Fallback to custom compilation if MSBuild fails
    /// </summary>
    public bool FallbackToCustom { get; set; } = true;
    
    /// <summary>
    /// Use lightweight custom compilation mode
    /// </summary>
    public bool LightweightMode { get; set; } = false;
}