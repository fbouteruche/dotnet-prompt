using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DotnetPrompt.Core.Models.Enums;

namespace DotnetPrompt.Core.Models.RoslynAnalysis;

/// <summary>
/// Configuration options for Roslyn analysis
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Analysis depth level
    /// </summary>
    [JsonPropertyName("analysis_depth")]
    public AnalysisDepth AnalysisDepth { get; set; } = AnalysisDepth.Standard;
    
    /// <summary>
    /// Semantic analysis depth level
    /// </summary>
    [JsonPropertyName("semantic_depth")]
    public SemanticAnalysisDepth SemanticDepth { get; set; } = SemanticAnalysisDepth.None;
    
    /// <summary>
    /// Compilation strategy to use
    /// </summary>
    [JsonPropertyName("compilation_strategy")]
    public CompilationStrategy CompilationStrategy { get; set; } = CompilationStrategy.Auto;
    
    /// <summary>
    /// Include dependency analysis
    /// </summary>
    [JsonPropertyName("include_dependencies")]
    public bool IncludeDependencies { get; set; } = true;
    
    /// <summary>
    /// Include code quality metrics
    /// </summary>
    [JsonPropertyName("include_metrics")]
    public bool IncludeMetrics { get; set; } = true;
    
    /// <summary>
    /// Include architectural pattern detection
    /// </summary>
    [JsonPropertyName("include_patterns")]
    public bool IncludePatterns { get; set; } = false;
    
    /// <summary>
    /// Include security vulnerability scanning
    /// </summary>
    [JsonPropertyName("include_vulnerabilities")]
    public bool IncludeVulnerabilities { get; set; } = false;
    
    /// <summary>
    /// Specific target framework to analyze
    /// </summary>
    [JsonPropertyName("target_framework")]
    public string? TargetFramework { get; set; }
    
    /// <summary>
    /// Maximum recursion depth for references
    /// </summary>
    [JsonPropertyName("max_depth")]
    [Range(1, 50, ErrorMessage = "Max depth must be between 1 and 50")]
    public int MaxDepth { get; set; } = 5;
    
    /// <summary>
    /// Exclude auto-generated code from analysis
    /// </summary>
    [JsonPropertyName("exclude_generated")]
    public bool ExcludeGenerated { get; set; } = true;
    
    /// <summary>
    /// Include test projects in analysis
    /// </summary>
    [JsonPropertyName("include_tests")]
    public bool IncludeTests { get; set; } = true;
    
    /// <summary>
    /// MSBuild workspace timeout in milliseconds
    /// </summary>
    [JsonPropertyName("msbuild_timeout")]
    [Range(1000, 300000, ErrorMessage = "MSBuild timeout must be between 1 second and 5 minutes")]
    public int MSBuildTimeout { get; set; } = 30000;
    
    /// <summary>
    /// Fallback to custom compilation if MSBuild fails
    /// </summary>
    [JsonPropertyName("fallback_to_custom")]
    public bool FallbackToCustom { get; set; } = true;
    
    /// <summary>
    /// Use lightweight custom compilation mode
    /// </summary>
    [JsonPropertyName("lightweight_mode")]
    public bool LightweightMode { get; set; } = false;
}

/// <summary>
/// Configuration options specific to compilation operations
/// </summary>
public class AnalysisCompilationOptions
{
    /// <summary>
    /// Target framework for compilation
    /// </summary>
    [JsonPropertyName("target_framework")]
    public string? TargetFramework { get; set; }
    
    /// <summary>
    /// MSBuild properties to set during compilation
    /// </summary>
    [JsonPropertyName("msbuild_properties")]
    public Dictionary<string, string> MSBuildProperties { get; set; } = new();
    
    /// <summary>
    /// Additional reference assemblies
    /// </summary>
    [JsonPropertyName("reference_assemblies")]
    public List<string> ReferenceAssemblies { get; set; } = new();
    
    /// <summary>
    /// Preprocessor symbols
    /// </summary>
    [JsonPropertyName("preprocessor_symbols")]
    public List<string> PreprocessorSymbols { get; set; } = new();
    
    /// <summary>
    /// Compilation timeout in milliseconds
    /// </summary>
    [JsonPropertyName("compilation_timeout")]
    [Range(5000, 600000, ErrorMessage = "Compilation timeout must be between 5 seconds and 10 minutes")]
    public int CompilationTimeout { get; set; } = 60000;
    
    /// <summary>
    /// Exclude auto-generated code from compilation
    /// </summary>
    [JsonPropertyName("exclude_generated")]
    public bool ExcludeGenerated { get; set; } = true;
    
    /// <summary>
    /// Enable nullable reference types context
    /// </summary>
    [JsonPropertyName("nullable_context")]
    public bool NullableContext { get; set; } = true;
    
    /// <summary>
    /// Language version to use for compilation
    /// </summary>
    [JsonPropertyName("language_version")]
    public string? LanguageVersion { get; set; }
    
    /// <summary>
    /// MSBuild workspace timeout in milliseconds (for compatibility with MSBuild operations)
    /// </summary>
    [JsonPropertyName("msbuild_timeout")]
    [Range(1000, 300000, ErrorMessage = "MSBuild timeout must be between 1 second and 5 minutes")]
    public int MSBuildTimeout { get; set; } = 30000;
    
    /// <summary>
    /// Include test projects in compilation
    /// </summary>
    [JsonPropertyName("include_tests")]
    public bool IncludeTests { get; set; } = true;
    
    /// <summary>
    /// Fallback to custom compilation if MSBuild fails (compatibility property)
    /// </summary>
    [JsonPropertyName("fallback_to_custom")]
    public bool FallbackToCustom { get; set; } = true;
    
    /// <summary>
    /// Use lightweight custom compilation mode (compatibility property)
    /// </summary>
    [JsonPropertyName("lightweight_mode")]
    public bool LightweightMode { get; set; } = false;
}