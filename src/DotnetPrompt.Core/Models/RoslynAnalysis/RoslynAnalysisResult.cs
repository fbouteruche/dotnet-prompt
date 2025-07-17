using System.Text.Json.Serialization;
using DotnetPrompt.Core.Models.Enums;

namespace DotnetPrompt.Core.Models.RoslynAnalysis;

/// <summary>
/// Complete Roslyn analysis result optimized for AI workflow consumption - domain model
/// </summary>
public class RoslynAnalysisResult
{
    /// <summary>
    /// Path to the analyzed project
    /// </summary>
    [JsonPropertyName("project_path")]
    public string ProjectPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis timestamp in ISO 8601 format
    /// </summary>
    [JsonPropertyName("analysis_timestamp")]
    public string AnalysisTimestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// Analysis depth used
    /// </summary>
    [JsonPropertyName("analysis_depth")]
    public AnalysisDepth AnalysisDepth { get; set; }
    
    /// <summary>
    /// Semantic analysis depth used
    /// </summary>
    [JsonPropertyName("semantic_depth")]
    public SemanticAnalysisDepth SemanticDepth { get; set; }
    
    /// <summary>
    /// Compilation strategy used
    /// </summary>
    [JsonPropertyName("compilation_strategy")]
    public CompilationStrategy CompilationStrategy { get; set; }
    
    /// <summary>
    /// Whether the analysis was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
    
    // Core Analysis Results
    
    /// <summary>
    /// Project metadata information
    /// </summary>
    [JsonPropertyName("project_metadata")]
    public ProjectMetadata ProjectMetadata { get; set; } = new();
    
    /// <summary>
    /// Compilation information (null if compilation was not required)
    /// </summary>
    [JsonPropertyName("compilation")]
    public CompilationInfo? Compilation { get; set; }
    
    /// <summary>
    /// Project structure analysis
    /// </summary>
    [JsonPropertyName("structure")]
    public ProjectStructure Structure { get; set; } = new();
    
    /// <summary>
    /// Dependency analysis results
    /// </summary>
    [JsonPropertyName("dependencies")]
    public DependencyAnalysis Dependencies { get; set; } = new();
    
    /// <summary>
    /// Semantic analysis results (null if SemanticDepth is None)
    /// </summary>
    [JsonPropertyName("semantics")]
    public SemanticAnalysis? Semantics { get; set; }
    
    /// <summary>
    /// Code metrics (null if not requested)
    /// </summary>
    [JsonPropertyName("metrics")]
    public CodeMetrics? Metrics { get; set; }
    
    /// <summary>
    /// Architectural patterns (null if not requested)
    /// </summary>
    [JsonPropertyName("patterns")]
    public ArchitecturalPatterns? Patterns { get; set; }
    
    /// <summary>
    /// Security analysis (null if not requested)
    /// </summary>
    [JsonPropertyName("vulnerabilities")]
    public SecurityAnalysis? Vulnerabilities { get; set; }
    
    // AI-Friendly Insights
    
    /// <summary>
    /// AI-friendly recommendations
    /// </summary>
    [JsonPropertyName("recommendations")]
    public List<Recommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Performance metrics for the analysis operation
    /// </summary>
    [JsonPropertyName("performance")]
    public PerformanceMetrics Performance { get; set; } = new();
    
    /// <summary>
    /// Execution time in milliseconds (compatibility property)
    /// </summary>
    [JsonPropertyName("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Metadata dictionary for compatibility with existing code
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Project metadata information
/// </summary>
public class ProjectMetadata
{
    /// <summary>
    /// Project name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Project type (e.g., "Console Application", "Class Library")
    /// </summary>
    [JsonPropertyName("project_type")]
    public string ProjectType { get; set; } = string.Empty;
    
    /// <summary>
    /// Target frameworks
    /// </summary>
    [JsonPropertyName("target_frameworks")]
    public List<string> TargetFrameworks { get; set; } = new();
    
    /// <summary>
    /// Language version
    /// </summary>
    [JsonPropertyName("language_version")]
    public string? LanguageVersion { get; set; }
    
    /// <summary>
    /// SDK version
    /// </summary>
    [JsonPropertyName("sdk_version")]
    public string? SdkVersion { get; set; }
    
    /// <summary>
    /// Whether nullable reference types are enabled
    /// </summary>
    [JsonPropertyName("nullable_enabled")]
    public bool NullableEnabled { get; set; }
    
    /// <summary>
    /// Assembly version
    /// </summary>
    [JsonPropertyName("assembly_version")]
    public string? AssemblyVersion { get; set; }
    
    /// <summary>
    /// File version
    /// </summary>
    [JsonPropertyName("file_version")]
    public string? FileVersion { get; set; }
}

/// <summary>
/// Compilation information
/// </summary>
public class CompilationInfo
{
    /// <summary>
    /// Assembly name
    /// </summary>
    [JsonPropertyName("assembly_name")]
    public string AssemblyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether compilation was successful
    /// </summary>
    [JsonPropertyName("compilation_successful")]
    public bool CompilationSuccessful { get; set; }
    
    /// <summary>
    /// Diagnostic counts
    /// </summary>
    [JsonPropertyName("diagnostic_counts")]
    public DiagnosticCount DiagnosticCounts { get; set; } = new();
    
    /// <summary>
    /// Total diagnostic count (compatibility property)
    /// </summary>
    [JsonPropertyName("diagnostic_count")]
    public int DiagnosticCount { get; set; }
    
    /// <summary>
    /// Number of syntax trees
    /// </summary>
    [JsonPropertyName("syntax_tree_count")]
    public int SyntaxTreeCount { get; set; }
    
    /// <summary>
    /// Number of referenced assemblies
    /// </summary>
    [JsonPropertyName("referenced_assembly_count")]
    public int ReferencedAssemblyCount { get; set; }
    
    /// <summary>
    /// Strategy used for compilation (compatibility property)
    /// </summary>
    [JsonPropertyName("strategy_used")]
    public CompilationStrategy StrategyUsed { get; set; }
    
    /// <summary>
    /// Whether fallback strategy was used (compatibility property)
    /// </summary>
    [JsonPropertyName("fallback_used")]
    public bool FallbackUsed { get; set; }
    
    /// <summary>
    /// Reason for fallback if used (compatibility property)
    /// </summary>
    [JsonPropertyName("fallback_reason")]
    public string? FallbackReason { get; set; }
    
    /// <summary>
    /// Compilation time in milliseconds (compatibility property)
    /// </summary>
    [JsonPropertyName("compilation_time_ms")]
    public long CompilationTimeMs { get; set; }
}

/// <summary>
/// Diagnostic count information
/// </summary>
public class DiagnosticCount
{
    /// <summary>
    /// Number of errors
    /// </summary>
    [JsonPropertyName("errors")]
    public int Errors { get; set; }
    
    /// <summary>
    /// Number of warnings
    /// </summary>
    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
    
    /// <summary>
    /// Number of info messages
    /// </summary>
    [JsonPropertyName("info")]
    public int Info { get; set; }
    
    /// <summary>
    /// Number of hidden diagnostics
    /// </summary>
    [JsonPropertyName("hidden")]
    public int Hidden { get; set; }
}

/// <summary>
/// Project structure information
/// </summary>
public class ProjectStructure
{
    /// <summary>
    /// Total number of source files
    /// </summary>
    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }
    
    /// <summary>
    /// Total lines of code
    /// </summary>
    [JsonPropertyName("total_lines")]
    public int TotalLines { get; set; }
    
    /// <summary>
    /// Directory structure
    /// </summary>
    [JsonPropertyName("directories")]
    public List<DirectoryStructureInfo> Directories { get; set; } = new();
    
    /// <summary>
    /// Source files information
    /// </summary>
    [JsonPropertyName("source_files")]
    public List<SourceFileInfo> SourceFiles { get; set; } = new();
}

/// <summary>
/// Directory structure information
/// </summary>
public class DirectoryStructureInfo
{
    /// <summary>
    /// Directory name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Relative path from project root
    /// </summary>
    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of files in directory
    /// </summary>
    [JsonPropertyName("file_count")]
    public int FileCount { get; set; }
    
    /// <summary>
    /// Total lines of code in directory
    /// </summary>
    [JsonPropertyName("line_count")]
    public int LineCount { get; set; }
}

/// <summary>
/// Source file information
/// </summary>
public class SourceFileInfo
{
    /// <summary>
    /// File name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Relative path from project root
    /// </summary>
    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of lines in file
    /// </summary>
    [JsonPropertyName("line_count")]
    public int LineCount { get; set; }
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Whether file is auto-generated
    /// </summary>
    [JsonPropertyName("is_generated")]
    public bool IsGenerated { get; set; }
}

/// <summary>
/// Dependency analysis results
/// </summary>
public class DependencyAnalysis
{
    /// <summary>
    /// Package references
    /// </summary>
    [JsonPropertyName("package_references")]
    public List<PackageReference> PackageReferences { get; set; } = new();
    
    /// <summary>
    /// Project references
    /// </summary>
    [JsonPropertyName("project_references")]
    public List<ProjectReference> ProjectReferences { get; set; } = new();
    
    /// <summary>
    /// Assembly references
    /// </summary>
    [JsonPropertyName("assembly_references")]
    public List<string> AssemblyReferences { get; set; } = new();
    
    /// <summary>
    /// Dependency counts
    /// </summary>
    [JsonPropertyName("dependency_counts")]
    public DependencyCount DependencyCounts { get; set; } = new();
}

/// <summary>
/// Package reference information
/// </summary>
public class PackageReference
{
    /// <summary>
    /// Package name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Package type (compatibility property)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "package";
    
    /// <summary>
    /// Package version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this is a transitive dependency
    /// </summary>
    [JsonPropertyName("is_transitive")]
    public bool IsTransitive { get; set; }
    
    /// <summary>
    /// Target framework for this reference
    /// </summary>
    [JsonPropertyName("target_framework")]
    public string? TargetFramework { get; set; }
}

/// <summary>
/// Project reference information
/// </summary>
public class ProjectReference
{
    /// <summary>
    /// Referenced project name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to referenced project
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether reference is conditional
    /// </summary>
    [JsonPropertyName("is_conditional")]
    public bool IsConditional { get; set; }
}

/// <summary>
/// Dependency count information
/// </summary>
public class DependencyCount
{
    /// <summary>
    /// Number of direct package dependencies
    /// </summary>
    [JsonPropertyName("direct_packages")]
    public int DirectPackages { get; set; }
    
    /// <summary>
    /// Number of transitive package dependencies
    /// </summary>
    [JsonPropertyName("transitive_packages")]
    public int TransitivePackages { get; set; }
    
    /// <summary>
    /// Number of project references
    /// </summary>
    [JsonPropertyName("project_references")]
    public int ProjectReferences { get; set; }
    
    /// <summary>
    /// Number of assembly references
    /// </summary>
    [JsonPropertyName("assembly_references")]
    public int AssemblyReferences { get; set; }
}

/// <summary>
/// Semantic analysis results
/// </summary>
public class SemanticAnalysis
{
    /// <summary>
    /// Type counts by category
    /// </summary>
    [JsonPropertyName("type_counts")]
    public TypeCount TypeCounts { get; set; } = new();
    
    /// <summary>
    /// Namespace information
    /// </summary>
    [JsonPropertyName("namespaces")]
    public List<NamespaceInfo> Namespaces { get; set; } = new();
    
    /// <summary>
    /// Type information (for Deep/Comprehensive analysis)
    /// </summary>
    [JsonPropertyName("types")]
    public List<TypeInfo> Types { get; set; } = new();
    
    /// <summary>
    /// Member information (for Deep/Comprehensive analysis)
    /// </summary>
    [JsonPropertyName("members")]
    public List<MemberInfo> Members { get; set; } = new();
    
    /// <summary>
    /// Inheritance relationships (for Deep/Comprehensive analysis)
    /// </summary>
    [JsonPropertyName("inheritance")]
    public List<InheritanceInfo> Inheritance { get; set; } = new();
}

/// <summary>
/// Type count information
/// </summary>
public class TypeCount
{
    /// <summary>
    /// Number of classes
    /// </summary>
    [JsonPropertyName("classes")]
    public int Classes { get; set; }
    
    /// <summary>
    /// Number of interfaces
    /// </summary>
    [JsonPropertyName("interfaces")]
    public int Interfaces { get; set; }
    
    /// <summary>
    /// Number of structs
    /// </summary>
    [JsonPropertyName("structs")]
    public int Structs { get; set; }
    
    /// <summary>
    /// Number of enums
    /// </summary>
    [JsonPropertyName("enums")]
    public int Enums { get; set; }
    
    /// <summary>
    /// Number of delegates
    /// </summary>
    [JsonPropertyName("delegates")]
    public int Delegates { get; set; }
    
    /// <summary>
    /// Number of records
    /// </summary>
    [JsonPropertyName("records")]
    public int Records { get; set; }
}

/// <summary>
/// Namespace information
/// </summary>
public class NamespaceInfo
{
    /// <summary>
    /// Namespace name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of types in namespace
    /// </summary>
    [JsonPropertyName("type_count")]
    public int TypeCount { get; set; }
    
    /// <summary>
    /// Whether namespace is external
    /// </summary>
    [JsonPropertyName("is_external")]
    public bool IsExternal { get; set; }
}

/// <summary>
/// Type information (for Deep/Comprehensive analysis)
/// </summary>
public class TypeInfo
{
    /// <summary>
    /// Type name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Full type name including namespace
    /// </summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type kind (class, interface, struct, enum, etc.)
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;
    
    /// <summary>
    /// Access modifier
    /// </summary>
    [JsonPropertyName("accessibility")]
    public string Accessibility { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether type is abstract
    /// </summary>
    [JsonPropertyName("is_abstract")]
    public bool IsAbstract { get; set; }
    
    /// <summary>
    /// Whether type is sealed
    /// </summary>
    [JsonPropertyName("is_sealed")]
    public bool IsSealed { get; set; }
    
    /// <summary>
    /// Whether type is static
    /// </summary>
    [JsonPropertyName("is_static")]
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Base type name
    /// </summary>
    [JsonPropertyName("base_type")]
    public string? BaseType { get; set; }
    
    /// <summary>
    /// Implemented interfaces
    /// </summary>
    [JsonPropertyName("interfaces")]
    public List<string> Interfaces { get; set; } = new();
    
    /// <summary>
    /// Number of members in type
    /// </summary>
    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }
}

/// <summary>
/// Member information (for Deep/Comprehensive analysis)
/// </summary>
public class MemberInfo
{
    /// <summary>
    /// Member name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Containing type name
    /// </summary>
    [JsonPropertyName("containing_type")]
    public string ContainingType { get; set; } = string.Empty;
    
    /// <summary>
    /// Member kind (method, property, field, event)
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;
    
    /// <summary>
    /// Access modifier
    /// </summary>
    [JsonPropertyName("accessibility")]
    public string Accessibility { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether member is static
    /// </summary>
    [JsonPropertyName("is_static")]
    public bool IsStatic { get; set; }
    
    /// <summary>
    /// Whether member is virtual
    /// </summary>
    [JsonPropertyName("is_virtual")]
    public bool IsVirtual { get; set; }
    
    /// <summary>
    /// Whether member is override
    /// </summary>
    [JsonPropertyName("is_override")]
    public bool IsOverride { get; set; }
    
    /// <summary>
    /// Return type for methods and properties
    /// </summary>
    [JsonPropertyName("return_type")]
    public string? ReturnType { get; set; }
}

/// <summary>
/// Inheritance information (for Deep/Comprehensive analysis)
/// </summary>
public class InheritanceInfo
{
    /// <summary>
    /// Base type name
    /// </summary>
    [JsonPropertyName("base_type")]
    public string BaseType { get; set; } = string.Empty;
    
    /// <summary>
    /// Derived type name
    /// </summary>
    [JsonPropertyName("derived_type")]
    public string DerivedType { get; set; } = string.Empty;
    
    /// <summary>
    /// Implemented interfaces
    /// </summary>
    [JsonPropertyName("interfaces")]
    public List<string> Interfaces { get; set; } = new();
}

/// <summary>
/// Code metrics information
/// </summary>
public class CodeMetrics
{
    /// <summary>
    /// Complexity metrics
    /// </summary>
    [JsonPropertyName("complexity")]
    public ComplexityMetrics Complexity { get; set; } = new();
    
    /// <summary>
    /// Maintainability metrics
    /// </summary>
    [JsonPropertyName("maintainability")]
    public MaintainabilityMetrics Maintainability { get; set; } = new();
    
    /// <summary>
    /// Size metrics
    /// </summary>
    [JsonPropertyName("size")]
    public SizeMetrics Size { get; set; } = new();
}

/// <summary>
/// Complexity metrics
/// </summary>
public class ComplexityMetrics
{
    /// <summary>
    /// Average cyclomatic complexity
    /// </summary>
    [JsonPropertyName("average_cyclomatic_complexity")]
    public double AverageCyclomaticComplexity { get; set; }
    
    /// <summary>
    /// Maximum complexity found
    /// </summary>
    [JsonPropertyName("max_complexity")]
    public int MaxComplexity { get; set; }
    
    /// <summary>
    /// Methods with high complexity
    /// </summary>
    [JsonPropertyName("complex_methods")]
    public List<string> ComplexMethods { get; set; } = new();
}

/// <summary>
/// Maintainability metrics
/// </summary>
public class MaintainabilityMetrics
{
    /// <summary>
    /// Overall maintainability index
    /// </summary>
    [JsonPropertyName("maintainability_index")]
    public double MaintainabilityIndex { get; set; }
    
    /// <summary>
    /// Technical debt ratio
    /// </summary>
    [JsonPropertyName("technical_debt_ratio")]
    public double TechnicalDebtRatio { get; set; }
    
    /// <summary>
    /// Code duplication percentage
    /// </summary>
    [JsonPropertyName("duplication_percentage")]
    public double DuplicationPercentage { get; set; }
}

/// <summary>
/// Size metrics
/// </summary>
public class SizeMetrics
{
    /// <summary>
    /// Total lines of code
    /// </summary>
    [JsonPropertyName("lines_of_code")]
    public int LinesOfCode { get; set; }
    
    /// <summary>
    /// Number of statements
    /// </summary>
    [JsonPropertyName("statement_count")]
    public int StatementCount { get; set; }
    
    /// <summary>
    /// Number of functions/methods
    /// </summary>
    [JsonPropertyName("function_count")]
    public int FunctionCount { get; set; }
    
    /// <summary>
    /// Number of classes
    /// </summary>
    [JsonPropertyName("class_count")]
    public int ClassCount { get; set; }
}

/// <summary>
/// Architectural patterns (for pattern detection)
/// </summary>
public class ArchitecturalPatterns
{
    /// <summary>
    /// Whether deep semantics are required for pattern detection
    /// </summary>
    [JsonPropertyName("requires_deep_semantics")]
    public bool RequiresDeepSemantics { get; set; }
    
    /// <summary>
    /// Minimum depth required for pattern detection
    /// </summary>
    [JsonPropertyName("minimum_depth_required")]
    public SemanticAnalysisDepth? MinimumDepthRequired { get; set; }
    
    /// <summary>
    /// Detected design patterns
    /// </summary>
    [JsonPropertyName("design_patterns")]
    public List<DesignPattern> DesignPatterns { get; set; } = new();
    
    /// <summary>
    /// Architectural layers detected
    /// </summary>
    [JsonPropertyName("architectural_layers")]
    public List<string> ArchitecturalLayers { get; set; } = new();
    
    /// <summary>
    /// Dependency patterns
    /// </summary>
    [JsonPropertyName("dependency_patterns")]
    public List<string> DependencyPatterns { get; set; } = new();
}

/// <summary>
/// Design pattern information
/// </summary>
public class DesignPattern
{
    /// <summary>
    /// Pattern name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Pattern description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence level (0.0 to 1.0)
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
    
    /// <summary>
    /// Types involved in the pattern
    /// </summary>
    [JsonPropertyName("involved_types")]
    public List<string> InvolvedTypes { get; set; } = new();
}

/// <summary>
/// Security analysis (for vulnerability scanning)
/// </summary>
public class SecurityAnalysis
{
    /// <summary>
    /// Number of security issues found
    /// </summary>
    [JsonPropertyName("issue_count")]
    public int IssueCount { get; set; }
    
    /// <summary>
    /// Security issues by severity
    /// </summary>
    [JsonPropertyName("issues")]
    public List<SecurityIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// Security patterns detected
    /// </summary>
    [JsonPropertyName("security_patterns")]
    public List<string> SecurityPatterns { get; set; } = new();
}

/// <summary>
/// Security issue information
/// </summary>
public class SecurityIssue
{
    /// <summary>
    /// Issue ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Issue title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Issue description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity level
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// File path where issue was found
    /// </summary>
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Line number where issue was found
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; set; }
}

/// <summary>
/// AI-friendly recommendation
/// </summary>
public class Recommendation
{
    /// <summary>
    /// Recommendation category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Recommendation type (compatibility property)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "recommendation";
    
    /// <summary>
    /// Priority level
    /// </summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;
    
    /// <summary>
    /// Recommendation message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// File path if applicable
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }
    
    /// <summary>
    /// Line number if applicable
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; set; }
    
    /// <summary>
    /// Whether recommendation is actionable
    /// </summary>
    [JsonPropertyName("actionable")]
    public bool Actionable { get; set; }
}

/// <summary>
/// Performance metrics for the analysis operation
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Total analysis time in milliseconds
    /// </summary>
    [JsonPropertyName("total_time_ms")]
    public long TotalTimeMs { get; set; }
    
    /// <summary>
    /// Compilation time in milliseconds
    /// </summary>
    [JsonPropertyName("compilation_time_ms")]
    public long CompilationTimeMs { get; set; }
    
    /// <summary>
    /// Analysis time in milliseconds
    /// </summary>
    [JsonPropertyName("analysis_time_ms")]
    public long AnalysisTimeMs { get; set; }
    
    /// <summary>
    /// Peak memory usage in bytes
    /// </summary>
    [JsonPropertyName("peak_memory_bytes")]
    public long PeakMemoryBytes { get; set; }
    
    /// <summary>
    /// Number of files processed
    /// </summary>
    [JsonPropertyName("files_processed")]
    public int FilesProcessed { get; set; }
    
    /// <summary>
    /// Total analysis time in milliseconds (compatibility property)
    /// </summary>
    [JsonPropertyName("total_analysis_time_ms")]
    public long TotalAnalysisTimeMs { get; set; }
    
    /// <summary>
    /// Semantic analysis time in milliseconds (compatibility property)
    /// </summary>
    [JsonPropertyName("semantic_analysis_time_ms")]
    public long SemanticAnalysisTimeMs { get; set; }
    
    /// <summary>
    /// Metrics calculation time in milliseconds (compatibility property)
    /// </summary>
    [JsonPropertyName("metrics_calculation_time_ms")]
    public long MetricsCalculationTimeMs { get; set; }
    
    /// <summary>
    /// Memory usage in MB (compatibility property)
    /// </summary>
    [JsonPropertyName("memory_usage_mb")]
    public double MemoryUsageMB { get; set; }
}