using System.Text.Json.Serialization;

namespace DotnetPrompt.Infrastructure.Analysis.Models;

/// <summary>
/// Complete Roslyn analysis result optimized for AI workflow consumption
/// </summary>
public class RoslynAnalysisResult
{
    /// <summary>
    /// Path to the analyzed project
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis timestamp in ISO 8601 format
    /// </summary>
    public string AnalysisTimestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis depth used
    /// </summary>
    public AnalysisDepth AnalysisDepth { get; set; }
    
    /// <summary>
    /// Semantic analysis depth used
    /// </summary>
    public SemanticAnalysisDepth SemanticDepth { get; set; }
    
    /// <summary>
    /// Compilation strategy used
    /// </summary>
    public CompilationStrategy CompilationStrategy { get; set; }
    
    /// <summary>
    /// Whether the analysis was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    // Core Analysis Results
    
    /// <summary>
    /// Project metadata information
    /// </summary>
    public ProjectMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Compilation information
    /// </summary>
    public CompilationInfo? Compilation { get; set; }
    
    /// <summary>
    /// Project structure analysis
    /// </summary>
    public ProjectStructure Structure { get; set; } = new();
    
    /// <summary>
    /// Dependency analysis results
    /// </summary>
    public DependencyAnalysis Dependencies { get; set; } = new();
    
    /// <summary>
    /// Semantic analysis results (null if SemanticDepth is None)
    /// </summary>
    public SemanticAnalysis? Semantics { get; set; }
    
    /// <summary>
    /// Code metrics (null if not requested)
    /// </summary>
    public CodeMetrics? Metrics { get; set; }
    
    /// <summary>
    /// Architectural patterns (null if not requested)
    /// </summary>
    public ArchitecturalPatterns? Patterns { get; set; }
    
    /// <summary>
    /// Security analysis (null if not requested)
    /// </summary>
    public SecurityAnalysis? Vulnerabilities { get; set; }
    
    // AI-Friendly Insights
    
    /// <summary>
    /// AI-friendly recommendations
    /// </summary>
    public List<Recommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Performance metrics for the analysis
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new();
}

/// <summary>
/// Project metadata information
/// </summary>
public class ProjectMetadata
{
    public string ProjectName { get; set; } = string.Empty;
    public List<string> TargetFrameworks { get; set; } = new();
    public string ProjectType { get; set; } = string.Empty;
    public bool SdkStyle { get; set; }
    public string OutputType { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int LineCount { get; set; }
}

/// <summary>
/// Compilation information
/// </summary>
public class CompilationInfo
{
    public bool Success { get; set; }
    public CompilationStrategy StrategyUsed { get; set; }
    public bool FallbackUsed { get; set; }
    public string? FallbackReason { get; set; }
    public DiagnosticCount DiagnosticCount { get; set; } = new();
    public long CompilationTimeMs { get; set; }
}

/// <summary>
/// Diagnostic count information
/// </summary>
public class DiagnosticCount
{
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int Info { get; set; }
}

/// <summary>
/// Project structure information
/// </summary>
public class ProjectStructure
{
    public List<DirectoryInfo> Directories { get; set; } = new();
    public List<SourceFileInfo> SourceFiles { get; set; } = new();
}

/// <summary>
/// Directory information
/// </summary>
public class DirectoryInfo
{
    public string Path { get; set; } = string.Empty;
    public int FileCount { get; set; }
}

/// <summary>
/// Source file information
/// </summary>
public class SourceFileInfo
{
    public string Path { get; set; } = string.Empty;
    public int Lines { get; set; }
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Dependency analysis results
/// </summary>
public class DependencyAnalysis
{
    public List<PackageReference> PackageReferences { get; set; } = new();
    public List<ProjectReference> ProjectReferences { get; set; } = new();
    public DependencyCount DependencyCount { get; set; } = new();
}

/// <summary>
/// Package reference information
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Project reference information
/// </summary>
public class ProjectReference
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Dependency count information
/// </summary>
public class DependencyCount
{
    public int Direct { get; set; }
    public int Transitive { get; set; }
}

/// <summary>
/// Semantic analysis results
/// </summary>
public class SemanticAnalysis
{
    public SemanticAnalysisDepth DepthUsed { get; set; }
    public bool CompilationRequired { get; set; }
    public TypeCount TypeCount { get; set; } = new();
    public List<NamespaceInfo> Namespaces { get; set; } = new();
    public List<TypeInfo>? Types { get; set; }
    public List<MemberInfo>? Members { get; set; }
    public List<InheritanceInfo>? InheritanceChains { get; set; }
}

/// <summary>
/// Type count information
/// </summary>
public class TypeCount
{
    public int Classes { get; set; }
    public int Interfaces { get; set; }
    public int Enums { get; set; }
    public int Structs { get; set; }
}

/// <summary>
/// Namespace information
/// </summary>
public class NamespaceInfo
{
    public string Name { get; set; } = string.Empty;
    public int TypeCount { get; set; }
}

/// <summary>
/// Type information (for Deep/Comprehensive analysis)
/// </summary>
public class TypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
}

/// <summary>
/// Member information (for Deep/Comprehensive analysis)
/// </summary>
public class MemberInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DeclaringType { get; set; } = string.Empty;
}

/// <summary>
/// Inheritance information (for Deep/Comprehensive analysis)
/// </summary>
public class InheritanceInfo
{
    public string BaseType { get; set; } = string.Empty;
    public string DerivedType { get; set; } = string.Empty;
    public List<string> Interfaces { get; set; } = new();
}

/// <summary>
/// Code metrics information
/// </summary>
public class CodeMetrics
{
    public ComplexityMetrics Complexity { get; set; } = new();
    public MaintainabilityMetrics Maintainability { get; set; } = new();
    public SizeMetrics Size { get; set; } = new();
}

/// <summary>
/// Complexity metrics
/// </summary>
public class ComplexityMetrics
{
    public double AverageCyclomaticComplexity { get; set; }
    public int MaxComplexity { get; set; }
    public List<string> ComplexMethods { get; set; } = new();
}

/// <summary>
/// Maintainability metrics
/// </summary>
public class MaintainabilityMetrics
{
    public double AverageIndex { get; set; }
    public List<string> LowMaintainabilityFiles { get; set; } = new();
}

/// <summary>
/// Size metrics
/// </summary>
public class SizeMetrics
{
    public int TotalLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
}

/// <summary>
/// Architectural patterns (for pattern detection)
/// </summary>
public class ArchitecturalPatterns
{
    public bool RequiresDeepSemantics { get; set; }
    public SemanticAnalysisDepth? MinimumDepthRequired { get; set; }
    public List<DesignPattern>? DesignPatterns { get; set; }
    public List<string>? ArchitecturalLayers { get; set; }
    public List<string>? DependencyPatterns { get; set; }
    public List<string>? CodeSmells { get; set; }
}

/// <summary>
/// Design pattern information
/// </summary>
public class DesignPattern
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Security analysis (for vulnerability scanning)
/// </summary>
public class SecurityAnalysis
{
    public List<SecurityIssue> Issues { get; set; } = new();
    public int TotalIssues { get; set; }
    public string ScanEngine { get; set; } = string.Empty;
}

/// <summary>
/// Security issue information
/// </summary>
public class SecurityIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? File { get; set; }
    public int? Line { get; set; }
}

/// <summary>
/// AI-friendly recommendation
/// </summary>
public class Recommendation
{
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? File { get; set; }
    public int? Line { get; set; }
    public bool Actionable { get; set; } = true;
}

/// <summary>
/// Performance metrics for the analysis
/// </summary>
public class PerformanceMetrics
{
    public long TotalAnalysisTimeMs { get; set; }
    public long CompilationTimeMs { get; set; }
    public long SemanticAnalysisTimeMs { get; set; }
    public long MetricsCalculationTimeMs { get; set; }
    public double MemoryUsageMB { get; set; }
}