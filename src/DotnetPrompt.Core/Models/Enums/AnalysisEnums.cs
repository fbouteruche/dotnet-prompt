namespace DotnetPrompt.Core.Models.Enums;

/// <summary>
/// Analysis depth levels for structural analysis
/// </summary>
public enum AnalysisDepth
{
    /// <summary>
    /// Basic project metadata and file structure
    /// </summary>
    Surface,
    
    /// <summary>
    /// Include dependency analysis and basic metrics
    /// </summary>
    Standard,
    
    /// <summary>
    /// Full semantic analysis with design patterns
    /// </summary>
    Deep,
    
    /// <summary>
    /// All features including vulnerability scanning and advanced metrics
    /// </summary>
    Comprehensive
}

/// <summary>
/// Semantic analysis depth levels for compilation-based analysis
/// </summary>
public enum SemanticAnalysisDepth
{
    /// <summary>
    /// No semantic analysis - syntax trees and project structure only
    /// </summary>
    None,
    
    /// <summary>
    /// Type information and basic symbol resolution within project scope
    /// </summary>
    Basic,
    
    /// <summary>
    /// Full semantic model with cross-references and external assembly symbols
    /// </summary>
    Standard,
    
    /// <summary>
    /// Include inheritance analysis, interface implementations, and design pattern detection
    /// </summary>
    Deep,
    
    /// <summary>
    /// All semantic features including advanced code analysis and architectural insights
    /// </summary>
    Comprehensive
}

/// <summary>
/// Compilation strategy options
/// </summary>
public enum CompilationStrategy
{
    /// <summary>
    /// Intelligent selection between MSBuild and Custom based on project complexity
    /// </summary>
    Auto,
    
    /// <summary>
    /// Use MSBuildWorkspace for full project system integration
    /// </summary>
    MSBuild,
    
    /// <summary>
    /// Use custom compilation units for lightweight analysis
    /// </summary>
    Custom,
    
    /// <summary>
    /// Combine both approaches - MSBuild for comprehensive analysis with Custom fallback
    /// </summary>
    Hybrid
}