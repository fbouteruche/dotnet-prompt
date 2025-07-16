# Roslyn Analysis Tool Specification

## Overview

This document defines the detailed specification for the **Roslyn Analysis Built-in Tool**, which leverages the .NET Compiler Platform (Roslyn) to perform deep semantic analysis of .NET projects and solutions. This is a **built-in tool for AI workflows** that provides structured analysis data to enable intelligent AI-powered development tasks.

## Status
✅ **COMPLETE** - Comprehensive implementation specification with MSBuild integration details

## Tool Classification

**🤖 AI Workflow Tool - NOT a CLI Command**

This is a **built-in tool** that integrates with the dotnet-prompt AI workflow system. It is:
- ✅ **Built-in tool** for AI workflow consumption via `{{invoke_tool}}` syntax
- ✅ **AI-optimized** with structured JSON output for machine processing
- ✅ **Workflow integration** designed for AI decision-making and code analysis
- ❌ **NOT a CLI command** for human users (no `dotnet-prompt analyze` command)
- ❌ **NOT a standalone utility** for direct human interaction
- ❌ **NOT a terminal command** - exclusively for AI workflow orchestration

## Purpose

The Roslyn Analysis Tool provides comprehensive semantic analysis of .NET projects and solutions using the full power of the .NET Compiler Platform. This tool is specifically designed for **AI workflow consumption**, providing structured JSON output that enables AI workflows to make informed decisions about code generation, refactoring, testing, documentation, and architectural analysis.

4. **Implement Semantic Kernel plugin interface** - Create SK functions that return structured JSON for AI consumption
5. **Create comprehensive test scenarios** - Test hybrid approach against various project types, sizes, and failure scenarios with JSON validation
6. **Integrate security and vulnerability scanning** - Add third-party security analysis tools with JSON-formatted results
7. **Develop AI-optimized JSON output** - Implement clean, structured JSON format with actionable recommendations
8. **Create recommendation system** - Build AI-friendly recommendation engine with actionable insights in JSON formatnology Foundation

This tool is built on the [.NET Compiler Platform (Roslyn)](https://github.com/dotnet/roslyn), Microsoft's open-source compiler platform that provides rich APIs for code analysis. Key references:

- **Roslyn SDK Documentation**: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/
- **GitHub Repository**: https://github.com/dotnet/roslyn
- **Semantic Analysis APIs**: Syntax trees, semantic models, symbol information, and workspace APIs

## Required NuGet Packages

The implementation requires these essential packages for MSBuild integration:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
<PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.8.0" />
<PackageReference Include="Microsoft.Build.Locator" Version="1.6.10" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
```

**Critical Setup Requirements:**
- **MSBuild.Locator**: Must be called BEFORE creating any MSBuildWorkspace instances
- **Package Compatibility**: Ensure all CodeAnalysis packages use the same version
- **Build Tools**: Requires .NET SDK or Build Tools installed on target machine

## Core Functionality

### Semantic Code Analysis (Powered by Roslyn)
- **Syntax Tree Analysis**: Complete syntax structure, AST traversal, and code patterns
- **Symbol Resolution**: Types, members, namespaces, and cross-references
- **Semantic Model Analysis**: Type information, method signatures, inheritance hierarchies
- **Compilation Analysis**: Diagnostics, errors, warnings, and compiler insights
- **Cross-Project Symbol Resolution**: Understanding dependencies and API usage across projects

### Advanced Project Metadata Extraction
- **Multi-Target Framework Analysis**: Per-target compilation and feature detection
- **MSBuild Integration**: Deep property analysis, custom targets, and build logic
- **Assembly Metadata**: Attributes, versioning, strong naming, and signing information
- **Runtime Analysis**: Target runtime identification and platform-specific features
- **Project SDK Detection**: Modern SDK-style vs legacy project formats

### Comprehensive Dependency Analysis
- **NuGet Package Deep Analysis**: API usage patterns, version compatibility, and dependency conflicts
- **Project Reference Graph**: Multi-project dependency mapping and circular reference detection
- **Transitive Dependency Resolution**: Complete dependency trees with conflict analysis
- **Package Vulnerability Assessment**: Security scanning and license compliance
- **API Surface Analysis**: Public API exposure and breaking change detection

### Code Quality and Metrics Analysis
- **Cyclomatic Complexity**: Method and class-level complexity measurement
- **Maintainability Index**: Code maintainability scoring
- **Code Coverage Potential**: Test coverage analysis and gap identification
- **Design Pattern Detection**: Common patterns (Repository, Factory, Singleton, etc.)
- **Code Smell Detection**: Anti-patterns and refactoring opportunities
- **Technical Debt Assessment**: Quantifiable code quality metrics

### Architectural Analysis
- **Namespace Architecture**: Logical organization and layer separation
- **Dependency Inversion**: Interface usage and abstraction patterns
- **SOLID Principles Compliance**: Automated principle violation detection
- **Cross-Cutting Concerns**: Logging, caching, validation pattern analysis
- **Domain Model Analysis**: Entity relationships and business logic patterns

## Input Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `project_path` | string | ✅ | - | Path to .csproj, .sln, or source directory |
| `analysis_depth` | enum | ❌ | Standard | Surface, Standard, Deep, Comprehensive |
| `semantic_depth` | enum | ❌ | None | None, Basic, Standard, Deep, Comprehensive |
| `compilation_strategy` | enum | ❌ | Auto | Auto, MSBuild, Custom, Hybrid |
| `include_dependencies` | bool | ❌ | true | Analyze NuGet packages and project references |
| `include_metrics` | bool | ❌ | true | Calculate code quality metrics |
| `include_patterns` | bool | ❌ | false | Detect architectural and design patterns |
| `include_vulnerabilities` | bool | ❌ | false | Scan for security vulnerabilities |
| `target_framework` | string | ❌ | null | Specific target framework to analyze |
| `max_depth` | int | ❌ | 5 | Maximum recursion depth for references |
| `exclude_generated` | bool | ❌ | true | Exclude auto-generated code from analysis |
| `include_tests` | bool | ❌ | true | Include test projects in analysis |
| `cache_compilation` | bool | ❌ | true | Cache Roslyn compilation for performance |
| `msbuild_timeout` | int | ❌ | 30000 | MSBuild workspace timeout in milliseconds |
| `fallback_to_custom` | bool | ❌ | true | Fallback to custom compilation if MSBuild fails |
| `lightweight_mode` | bool | ❌ | false | Use custom compilation for lightweight analysis |

### Analysis Depth Levels

- **Surface**: Basic project metadata and file structure
- **Standard**: Include dependency analysis and basic metrics  
- **Deep**: Full semantic analysis with design patterns
- **Comprehensive**: All features including vulnerability scanning and advanced metrics

### Semantic Analysis Depth Levels

- **None**: No semantic analysis - syntax trees and project structure only (performance-optimized)
- **Basic**: Type information and basic symbol resolution within project scope
- **Standard**: Full semantic model with cross-references and external assembly symbols
- **Deep**: Include inheritance analysis, interface implementations, and design pattern detection
- **Comprehensive**: All semantic features including advanced code analysis and architectural insights

### Compilation Strategy Options

- **Auto**: Intelligent selection between MSBuild and Custom based on project complexity and requirements
- **MSBuild**: Use MSBuildWorkspace for full project system integration and accuracy
- **Custom**: Use custom compilation units for lightweight analysis or specific scenarios
- **Hybrid**: Combine both approaches - MSBuild for comprehensive analysis with Custom fallback

## Output Format

The tool returns a structured JSON response optimized for AI workflow consumption:

```json
{
  "projectPath": "./MyApp.csproj",
  "analysisTimestamp": "2025-07-10T14:30:00Z",
  "analysisDepth": "Standard",
  "semanticDepth": "Basic",
  "compilationStrategy": "Hybrid",
  "success": true,
  "executionTimeMs": 1250,
  
  "metadata": {
    "projectName": "MyApp",
    "targetFrameworks": ["net8.0"],
    "projectType": "Console",
    "sdkStyle": true,
    "outputType": "Exe",
    "fileCount": 45,
    "lineCount": 2340
  },
  
  "compilation": {
    "success": true,
    "strategyUsed": "MSBuild",
    "fallbackUsed": false,
    "diagnosticCount": {
      "errors": 0,
      "warnings": 3,
      "info": 12
    },
    "compilationTimeMs": 850
  },
  
  "structure": {
    "directories": [
      { "path": "Controllers", "fileCount": 8 },
      { "path": "Models", "fileCount": 12 },
      { "path": "Services", "fileCount": 6 }
    ],
    "sourceFiles": [
      { "path": "Program.cs", "lines": 45, "type": "EntryPoint" },
      { "path": "Controllers/HomeController.cs", "lines": 89, "type": "Controller" }
    ]
  },
  
  "dependencies": {
    "packageReferences": [
      { "name": "Microsoft.AspNetCore.App", "version": "8.0.0", "type": "Framework" },
      { "name": "Newtonsoft.Json", "version": "13.0.3", "type": "Package" }
    ],
    "projectReferences": [
      { "name": "MyApp.Core", "path": "../MyApp.Core/MyApp.Core.csproj" }
    ],
    "dependencyCount": {
      "direct": 8,
      "transitive": 47
    }
  },
  
  "semantics": {
    "depthUsed": "Basic",
    "compilationRequired": true,
    "typeCount": {
      "classes": 24,
      "interfaces": 8,
      "enums": 4,
      "structs": 2
    },
    "namespaces": [
      { "name": "MyApp", "typeCount": 12 },
      { "name": "MyApp.Controllers", "typeCount": 8 },
      { "name": "MyApp.Models", "typeCount": 12 }
    ]
  },
  
  "metrics": {
    "complexity": {
      "averageCyclomaticComplexity": 3.2,
      "maxComplexity": 12,
      "complexMethods": ["ProcessPayment", "ValidateUser"]
    },
    "maintainability": {
      "averageIndex": 78.5,
      "lowMaintainabilityFiles": ["LegacyProcessor.cs"]
    },
    "size": {
      "totalLines": 2340,
      "codeLines": 1890,
      "commentLines": 280,
      "blankLines": 170
    }
  },
  
  "patterns": null,
  "vulnerabilities": null,
  
  "recommendations": [
    {
      "type": "Performance",
      "priority": "Medium", 
      "message": "Consider reducing complexity in ProcessPayment method",
      "file": "Services/PaymentService.cs",
      "line": 45
    }
  ],
  
  "performance": {
    "totalAnalysisTimeMs": 1250,
    "compilationTimeMs": 850,
    "semanticAnalysisTimeMs": 200,
    "metricsCalculationTimeMs": 150,
    "memoryUsageMB": 85.2
  }
}
```

### JSON Schema Definition

```csharp
interface RoslynAnalysisResult {
    // Analysis Context
    projectPath: string;
    analysisTimestamp: string;
    analysisDepth: AnalysisDepth;
    semanticDepth: SemanticAnalysisDepth;
    compilationStrategy: CompilationStrategy;
    success: boolean;
    executionTimeMs: number;
    
    // Core Analysis Results
    metadata: ProjectMetadata;
    compilation: CompilationInfo;
    structure: ProjectStructure;
    dependencies: DependencyAnalysis;
    semantics: SemanticAnalysis | null;
    metrics: CodeMetrics | null;
    patterns: ArchitecturalPatterns | null;
    vulnerabilities: SecurityAnalysis | null;
    
    // AI-Friendly Insights
    recommendations: Recommendation[];
    performance: PerformanceMetrics;
}

interface ProjectMetadata {
    projectName: string;
    targetFrameworks: string[];
    projectType: string; // Console, Web, Library, Test
    sdkStyle: boolean;
    outputType: string;
    fileCount: number;
    lineCount: number;
}

interface CompilationInfo {
    success: boolean;
    strategyUsed: CompilationStrategy;
    fallbackUsed: boolean;
    fallbackReason?: string;
    diagnosticCount: {
        errors: number;
        warnings: number;
        info: number;
    };
    compilationTimeMs: number;
}

interface ProjectStructure {
    directories: DirectoryInfo[];
    sourceFiles: SourceFileInfo[];
}

interface DependencyAnalysis {
    packageReferences: PackageReference[];
    projectReferences: ProjectReference[];
    dependencyCount: {
        direct: number;
        transitive: number;
    };
}

interface SemanticAnalysis {
    depthUsed: SemanticAnalysisDepth;
    compilationRequired: boolean;
    typeCount: {
        classes: number;
        interfaces: number;
        enums: number;
        structs: number;
    };
    namespaces: NamespaceInfo[];
    // Additional semantic data based on depth
    types?: TypeInfo[];
    members?: MemberInfo[];
    inheritanceChains?: InheritanceInfo[];
}

interface Recommendation {
    type: string; // Performance, Security, Maintainability, Architecture
    priority: string; // Low, Medium, High, Critical
    message: string;
    file?: string;
    line?: number;
    actionable?: boolean;
}
```

## Clarifying Questions

### 1. Roslyn Integration Strategy ✅ RESOLVED
**Decision: Hybrid Approach with MSBuildWorkspace Primary Strategy**

The tool implements a hybrid compilation strategy that combines the benefits of both approaches:

- **Primary Strategy**: MSBuildWorkspace for comprehensive project analysis with full MSBuild integration
- **Fallback Strategy**: Custom compilation units for lightweight scenarios or when MSBuild fails
- **Multi-target framework projects**: Analyze all targets by default, with option to specify specific target
- **Project format support**: Full support for both SDK-style and legacy project formats via MSBuildWorkspace
- **Roslyn analyzers integration**: Leverage MSBuildWorkspace's built-in analyzer support
- **Compilation caching**: Intelligent caching across both compilation strategies with invalidation

### 2. Semantic Analysis Depth ✅ RESOLVED
**Decision: Progressive Semantic Analysis with Default to None**

The tool implements a performance-first approach with progressive semantic analysis:

- **Default to None**: No semantic analysis by default for optimal performance and fast feedback
- **Opt-in Complexity**: Users explicitly choose semantic analysis depth based on their needs
- **Progressive Enhancement**: Start with structure analysis, incrementally add semantic depth
- **Lazy Loading**: Semantic analysis only performed when explicitly requested
- **Symbol resolution scope**: Basic (project only), Standard (includes dependencies), Deep (full graph)
- **Advanced analysis**: Inheritance, interfaces, and design patterns require Deep or Comprehensive levels
- **Performance optimization**: Clear separation between structural and semantic analysis costs

### 3. Advanced Code Analysis 🔄 PENDING
- Which design patterns should be automatically detected?
- Should the tool identify anti-patterns and code smells?
- How should SOLID principle violations be scored and reported?
- Should there be custom analyzers for domain-specific patterns?
- How should the tool handle reflection-based code and dynamic types?

### 4. Performance and Scalability with Roslyn 🔄 PENDING
- What are acceptable compilation times for large solutions?
- Should incremental compilation be leveraged for repeated analysis?
- How should memory usage be managed for large codebases?
- Should analysis be parallelized across projects?
- How should Roslyn workspace management be optimized?

### 5. Dependency and Package Analysis 🔄 PENDING
- Should the tool analyze actual API usage within dependencies?
- How should package compatibility and breaking changes be detected?
- Should transitive dependency conflicts be automatically resolved?
- How should package license compatibility be validated?
- Should the tool identify unused or under-utilized dependencies?

### 6. Code Quality Metrics 🔄 PENDING
- Which complexity metrics provide the most value for AI decision-making?
- Should custom metrics be definable through configuration?
- How should metrics be weighted and aggregated for overall scores?
- Should the tool provide refactoring suggestions based on metrics?
- How should test coverage potential be calculated without actual tests?

### 7. Architectural Analysis 🔄 PENDING
- How should layered architecture compliance be validated?
- Should the tool detect Domain-Driven Design patterns?
- How should microservice boundary analysis work for solutions?
- Should the tool identify tight coupling and suggest improvements?
- How should cross-cutting concerns be automatically identified?

### 8. AI Workflow Integration ✅ RESOLVED
**Decision: Simple JSON Output Format**

The tool provides structured JSON output optimized for AI consumption:

- **Primary Output Format**: Well-structured JSON with consistent schema
- **Hierarchical Structure**: Logical grouping of analysis results for easy parsing
- **Context Preservation**: Include analysis metadata (depth used, strategy, performance)
- **Null-Safe Design**: Optional fields clearly marked, graceful handling of missing data
- **Flat Arrays**: Avoid deep nesting for easier AI model processing
- **Consistent Naming**: Use clear, descriptive property names following camelCase convention
- **Size Optimization**: Exclude verbose internal details, focus on actionable insights

### 9. Security and Compliance 🔄 PENDING
- Which vulnerability scanners should be integrated with Roslyn analysis?
- How should sensitive code patterns be detected and reported?
- Should the tool check for compliance with coding standards?
- How should license compatibility across the dependency graph be validated?
- Should the tool detect potential security anti-patterns in code?

### 10. Error Handling and Partial Analysis 🔄 PENDING
- How should compilation errors be handled during analysis?
- Should the tool provide partial results when some projects fail to compile?
- How should missing dependencies or broken references be reported?
- Should the tool attempt to repair common project issues automatically?
- How should version conflicts in multi-target scenarios be resolved?

## Example Usage Scenarios

**All examples below show AI workflow usage with `{{invoke_tool}}` syntax - NOT CLI commands**

### Fast Structure Overview (No Semantics)
```markdown
AI workflow task: Get project structure and dependencies without semantic analysis

{{invoke_tool: analyze_with_roslyn
  project_path: "./MyApp.csproj"
  analysis_depth: "Standard"
  semantic_depth: "None"
  include_dependencies: true
  include_metrics: false
}}

Expected JSON response includes: metadata, structure, dependencies, null semantics
```

### Progressive Semantic Analysis  
```markdown
AI workflow task: Start with basic semantic analysis for type information

{{invoke_tool: analyze_with_roslyn
  project_path: "./MyApp.csproj"
  analysis_depth: "Standard"
  semantic_depth: "Basic"
  compilation_strategy: "Auto"
  include_dependencies: true
}}

Expected JSON response includes: basic semantics with type counts and namespaces
```

### Comprehensive Analysis with Recommendations
```markdown
AI workflow task: Deep analysis with AI-friendly recommendations

{{invoke_tool: analyze_with_roslyn
  project_path: "./Domain.csproj"
  analysis_depth: "Deep"
  semantic_depth: "Deep"
  include_patterns: true
  include_metrics: true
  compilation_strategy: "Hybrid"
}}

Expected JSON response includes: full semantics, patterns, metrics, and actionable recommendations
```

### MSBuild-First with Semantic Analysis
```markdown
AI workflow task: Force MSBuild workspace with full semantic analysis

{{invoke_tool: analyze_with_roslyn
  project_path: "./MySolution.sln"
  analysis_depth: "Comprehensive"
  semantic_depth: "Comprehensive"
  compilation_strategy: "MSBuild"
  include_patterns: true
  msbuild_timeout: 60000
  max_depth: 10
}}
```

### Lightweight Structure Analysis
```markdown
AI workflow task: Fast custom compilation for structure overview

{{invoke_tool: analyze_with_roslyn
  project_path: "./SimpleProject.csproj"
  analysis_depth: "Surface"
  semantic_depth: "None"
  compilation_strategy: "Custom"
  lightweight_mode: true
  include_dependencies: false
}}
```

### Code Quality with Basic Semantics
```markdown
AI workflow task: Focus on metrics with minimal semantic overhead

{{invoke_tool: analyze_with_roslyn
  project_path: "./MyProject.csproj"
  analysis_depth: "Standard"
  semantic_depth: "Basic"
  include_metrics: true
  include_vulnerabilities: true
  exclude_generated: true
}}
```

### Design Pattern Detection
```markdown
AI workflow task: Identify architectural patterns with deep semantic analysis

{{invoke_tool: analyze_with_roslyn
  project_path: "./Domain.csproj"
  analysis_depth: "Deep"
  semantic_depth: "Deep"
  include_patterns: true
  compilation_strategy: "Hybrid"
}}
```

### Dependency Security Audit
```markdown
AI workflow task: Security analysis with standard semantic depth

{{invoke_tool: analyze_with_roslyn
  project_path: "./WebApi.csproj"
  analysis_depth: "Standard"
  semantic_depth: "Standard"
  include_dependencies: true
  include_vulnerabilities: true
}}
```

### Multi-Target Framework Analysis
```markdown
AI workflow task: Analyze specific target framework with semantics

{{invoke_tool: analyze_with_roslyn
  project_path: "./CrossPlatform.csproj"
  analysis_depth: "Deep"
  semantic_depth: "Standard"
  target_framework: "net8.0"
  compilation_strategy: "MSBuild"
}}
```

## Implementation Strategy

### Phase 1: Hybrid Compilation Foundation
1. **MSBuildWorkspace Primary Implementation**: Configure robust MSBuildWorkspace with proper error handling and timeout management
2. **Custom Compilation Fallback**: Implement lightweight custom compilation units for scenarios where MSBuild fails
3. **Strategy Selection Logic**: Intelligent automatic selection between compilation strategies based on project characteristics
4. **Progressive Semantic Analysis**: Implement lazy semantic analysis with configurable depth levels

### Phase 2: Advanced Analysis Capabilities  
1. **Semantic Depth Engine**: Build semantic analysis pipeline that respects depth configuration (None → Basic → Standard → Deep → Comprehensive)
2. **Cross-Strategy Metrics**: Implement complexity, maintainability, and quality metrics that work with both compilation strategies
3. **Pattern Detection**: Design pattern and architectural pattern recognition for Deep and Comprehensive semantic levels
4. **Dependency Analysis**: Deep dependency graph analysis with conflict detection for both MSBuild and custom scenarios
5. **Security Integration**: Vulnerability scanning and security pattern detection with strategy-aware implementation

### Phase 3: AI Integration and JSON Optimization
1. **JSON Output Formatting**: Implement clean, structured JSON output optimized for AI consumption
2. **Recommendation Engine**: Build AI-friendly recommendation system with actionable insights
3. **Context Preservation**: Include analysis metadata and performance metrics in JSON output
4. **Response Optimization**: Optimize JSON structure size and complexity for AI model consumption
5. **Workflow Integration**: Seamless integration with AI-powered development workflows using standardized JSON format

## Technical Architecture

### Built-in Tool Integration with Semantic Kernel

This tool integrates as a **Semantic Kernel plugin** within the dotnet-prompt built-in tool system:

```csharp
[KernelFunction("analyze_with_roslyn")]
[Description("Comprehensive .NET project analysis using Roslyn for AI workflows")]
public async Task<string> AnalyzeProjectAsync(
    [Description("Path to .csproj, .sln, or source directory")] string project_path,
    [Description("Analysis depth: Surface, Standard, Deep, Comprehensive")] string analysis_depth = "Standard",
    [Description("Semantic analysis depth: None, Basic, Standard, Deep, Comprehensive")] string semantic_depth = "None",
    [Description("Compilation strategy: Auto, MSBuild, Custom, Hybrid")] string compilation_strategy = "Auto",
    [Description("Include dependency analysis")] bool include_dependencies = true,
    [Description("Include code quality metrics")] bool include_metrics = true,
    [Description("Include architectural pattern detection")] bool include_patterns = false,
    [Description("Include security vulnerability scanning")] bool include_vulnerabilities = false,
    [Description("Specific target framework to analyze")] string? target_framework = null,
    [Description("Maximum recursion depth for references")] int max_depth = 5,
    [Description("Exclude auto-generated code from analysis")] bool exclude_generated = true,
    [Description("Include test projects in analysis")] bool include_tests = true,
    [Description("MSBuild workspace timeout in milliseconds")] int msbuild_timeout = 30000,
    [Description("Fallback to custom compilation if MSBuild fails")] bool fallback_to_custom = true,
    [Description("Use lightweight custom compilation mode")] bool lightweight_mode = false,
    CancellationToken cancellationToken = default)
{
    // Returns structured JSON for AI workflow consumption
    return await _analysisService.AnalyzeAsync(project_path, options, cancellationToken);
}
```

### AI Workflow Integration Architecture

#### Core Service and Strategy Interfaces

```csharp
// Primary interface for analysis
public interface IRoslynAnalysisService
{
    Task<RoslynAnalysisResult> AnalyzeAsync(
        string projectPath, 
        AnalysisOptions options, 
        CancellationToken cancellationToken = default);
}

// Strategy pattern for compilation approaches
public interface ICompilationStrategy
{
    Task<CompilationResult> CreateCompilationAsync(
        string projectPath, 
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default);
    bool CanHandle(string projectPath, AnalysisOptions options);
    CompilationStrategy StrategyType { get; }
}

// Enhanced compilation result with MSBuild metadata
public class CompilationResult
{
    public Compilation? Compilation { get; set; }
    public bool Success { get; set; }
    public CompilationStrategy StrategyUsed { get; set; }
    public bool FallbackUsed { get; set; }
    public string? FallbackReason { get; set; }
    public string? ErrorMessage { get; set; }
    public long CompilationTimeMs { get; set; }
    public ImmutableArray<Diagnostic> Diagnostics { get; set; } = ImmutableArray<Diagnostic>.Empty;
    public WorkspaceDiagnostic[]? WorkspaceDiagnostics { get; set; }
    public Dictionary<string, object>? ProjectMetadata { get; set; }
    public string? TargetFramework { get; set; }
    
    public CompilationResult() { }
    
    public CompilationResult(Compilation compilation, CompilationStrategy strategy)
    {
        Compilation = compilation;
        Success = compilation != null;
        StrategyUsed = strategy;
        Diagnostics = compilation?.GetDiagnostics() ?? ImmutableArray<Diagnostic>.Empty;
    }
}
```

### MSBuild Integration Implementation

#### Critical MSBuild Setup Patterns

```csharp
// REQUIRED: MSBuild Locator must be called before any MSBuildWorkspace creation
public static class MSBuildSetup
{
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new();
    
    public static void EnsureInitialized()
    {
        if (_isInitialized) return;
        
        lock (_lockObject)
        {
            if (_isInitialized) return;
            
            try
            {
                // Register the default MSBuild installation
                MSBuildLocator.RegisterDefaults();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to initialize MSBuild. Ensure .NET SDK is installed.", ex);
            }
        }
    }
}
```

#### MSBuildWorkspace Strategy Implementation

```csharp
/// <summary>
/// Primary compilation strategy using MSBuildWorkspace for full project system integration
/// </summary>
public class MSBuildWorkspaceStrategy : ICompilationStrategy
{
    private readonly ILogger<MSBuildWorkspaceStrategy> _logger;
    
    public MSBuildWorkspaceStrategy(ILogger<MSBuildWorkspaceStrategy> logger)
    {
        _logger = logger;
        MSBuildSetup.EnsureInitialized(); // Critical initialization
    }
    
    public CompilationStrategy StrategyType => CompilationStrategy.MSBuild;
    
    public bool CanHandle(string projectPath, AnalysisOptions options)
    {
        // MSBuild can handle .csproj, .fsproj, .vbproj, and .sln files
        var extension = Path.GetExtension(projectPath).ToLowerInvariant();
        return extension == ".csproj" || extension == ".fsproj" || 
               extension == ".vbproj" || extension == ".sln";
    }
    
    public async Task<CompilationResult> CreateCompilationAsync(
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        MSBuildWorkspace? workspace = null;
        
        try
        {
            _logger.LogInformation("Starting MSBuild compilation for {ProjectPath}", projectPath);
            
            // Create MSBuild workspace with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.MSBuildTimeout));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                // Configure MSBuild properties for better compatibility
                ["DesignTimeBuild"] = "true",
                ["BuildProjectReferences"] = "false",
                ["_ResolveReferenceDependencies"] = "true",
                ["SolutionDir"] = Path.GetDirectoryName(projectPath) + Path.DirectorySeparatorChar
            });
            
            // Handle solution vs project files
            if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleSolutionAnalysis(workspace, projectPath, options, combinedCts.Token);
            }
            else
            {
                return await HandleProjectAnalysis(workspace, projectPath, options, combinedCts.Token);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Re-throw user cancellation
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MSBuild compilation timed out for {ProjectPath} after {Timeout}ms", 
                projectPath, options.MSBuildTimeout);
            
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.MSBuild,
                ErrorMessage = $"MSBuild compilation timed out after {options.MSBuildTimeout}ms",
                CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSBuild compilation failed for {ProjectPath}", projectPath);
            
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.MSBuild,
                ErrorMessage = ex.Message,
                CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
        finally
        {
            workspace?.Dispose();
        }
    }
    
    private async Task<CompilationResult> HandleSolutionAnalysis(
        MSBuildWorkspace workspace,
        string solutionPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken);
        
        // Validate workspace diagnostics
        var criticalDiagnostics = workspace.Diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToList();
            
        if (criticalDiagnostics.Any())
        {
            _logger.LogWarning("MSBuild workspace has {Count} critical diagnostics for solution {Path}",
                criticalDiagnostics.Count, solutionPath);
        }
        
        // For solutions, analyze the first valid project or target specific project
        var targetProject = string.IsNullOrEmpty(options.TargetFramework)
            ? solution.Projects.FirstOrDefault(p => p.Language == LanguageNames.CSharp)
            : solution.Projects.FirstOrDefault(p => 
                p.Language == LanguageNames.CSharp && 
                p.ParseOptions?.DocumentationMode != DocumentationMode.None);
        
        if (targetProject == null)
        {
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.MSBuild,
                ErrorMessage = "No suitable C# project found in solution"
            };
        }
        
        var compilation = await targetProject.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.MSBuild,
                ErrorMessage = "Failed to create compilation from MSBuild project"
            };
        }
        
        return new CompilationResult(compilation, CompilationStrategy.MSBuild)
        {
            ProjectMetadata = ExtractProjectMetadata(targetProject),
            WorkspaceDiagnostics = workspace.Diagnostics.ToArray(),
            TargetFramework = ExtractTargetFramework(targetProject)
        };
    }
    
    private async Task<CompilationResult> HandleProjectAnalysis(
        MSBuildWorkspace workspace,
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        var project = await workspace.OpenProjectAsync(projectPath, cancellationToken);
        
        // Handle multi-target framework projects
        if (!string.IsNullOrEmpty(options.TargetFramework))
        {
            // For multi-target projects, we might need to reload with specific target framework
            _logger.LogDebug("Targeting specific framework {Framework} for project {Project}",
                options.TargetFramework, projectPath);
        }
        
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.MSBuild,
                ErrorMessage = "Failed to create compilation from MSBuild project"
            };
        }
        
        return new CompilationResult(compilation, CompilationStrategy.MSBuild)
        {
            ProjectMetadata = ExtractProjectMetadata(project),
            WorkspaceDiagnostics = workspace.Diagnostics.ToArray(),
            TargetFramework = ExtractTargetFramework(project)
        };
    }
    
    private Dictionary<string, object> ExtractProjectMetadata(Project project)
    {
        return new Dictionary<string, object>
        {
            ["ProjectName"] = project.Name,
            ["Language"] = project.Language,
            ["FilePath"] = project.FilePath ?? string.Empty,
            ["OutputFilePath"] = project.OutputFilePath ?? string.Empty,
            ["CompilationOutputInfo"] = project.CompilationOutputInfo,
            ["HasDocuments"] = project.Documents.Any(),
            ["DocumentCount"] = project.Documents.Count(),
            ["MetadataReferences"] = project.MetadataReferences.Count,
            ["ProjectReferences"] = project.ProjectReferences.Count(),
            ["AnalyzerReferences"] = project.AnalyzerReferences.Count
        };
    }
    
    private string? ExtractTargetFramework(Project project)
    {
        // Extract target framework from project properties or compilation options
        // This is a simplified extraction - full implementation would parse project properties
        return project.CompilationOptions?.Platform?.ToString();
    }
}
```

#### Hybrid Strategy Coordinator

```csharp
/// <summary>
/// Intelligent strategy coordinator that tries MSBuild first with Custom fallback
/// </summary>
public class HybridCompilationStrategy : ICompilationStrategy
{
    private readonly MSBuildWorkspaceStrategy _msbuildStrategy;
    private readonly CustomCompilationStrategy _customStrategy;
    private readonly ILogger<HybridCompilationStrategy> _logger;
    
    public HybridCompilationStrategy(
        MSBuildWorkspaceStrategy msbuildStrategy,
        CustomCompilationStrategy customStrategy,
        ILogger<HybridCompilationStrategy> logger)
    {
        _msbuildStrategy = msbuildStrategy;
        _customStrategy = customStrategy;
        _logger = logger;
    }
    
    public CompilationStrategy StrategyType => CompilationStrategy.Hybrid;
    
    public bool CanHandle(string projectPath, AnalysisOptions options)
    {
        // Hybrid can handle anything that either strategy can handle
        return _msbuildStrategy.CanHandle(projectPath, options) || 
               _customStrategy.CanHandle(projectPath, options);
    }
    
    public async Task<CompilationResult> CreateCompilationAsync(
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        // Try MSBuild first for comprehensive analysis
        if (_msbuildStrategy.CanHandle(projectPath, options))
        {
            try
            {
                _logger.LogDebug("Attempting MSBuild compilation for {ProjectPath}", projectPath);
                
                var msbuildResult = await _msbuildStrategy.CreateCompilationAsync(projectPath, options, cancellationToken);
                
                if (msbuildResult.Success && HasAcceptableQuality(msbuildResult))
                {
                    _logger.LogInformation("MSBuild compilation successful for {ProjectPath}", projectPath);
                    return msbuildResult;
                }
                
                if (!options.FallbackToCustom)
                {
                    _logger.LogWarning("MSBuild compilation failed and fallback disabled for {ProjectPath}", projectPath);
                    return msbuildResult;
                }
                
                _logger.LogWarning("MSBuild compilation quality insufficient, falling back to custom compilation for {ProjectPath}", projectPath);
            }
            catch (Exception ex) when (options.FallbackToCustom)
            {
                _logger.LogWarning(ex, "MSBuild compilation failed, falling back to custom compilation for {ProjectPath}", projectPath);
            }
        }
        
        // Fallback to custom compilation
        if (_customStrategy.CanHandle(projectPath, options))
        {
            _logger.LogInformation("Using custom compilation strategy for {ProjectPath}", projectPath);
            
            var customResult = await _customStrategy.CreateCompilationAsync(projectPath, options, cancellationToken);
            
            // Mark as fallback result
            customResult.FallbackUsed = true;
            customResult.FallbackReason = "MSBuild compilation failed or produced insufficient quality";
            customResult.StrategyUsed = CompilationStrategy.Hybrid; // Indicate hybrid was used
            
            return customResult;
        }
        
        return new CompilationResult
        {
            Success = false,
            StrategyUsed = CompilationStrategy.Hybrid,
            ErrorMessage = "No suitable compilation strategy available for the given project",
            CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
        };
    }
    
    private bool HasAcceptableQuality(CompilationResult result)
    {
        if (!result.Success || result.Compilation == null)
            return false;
        
        // Quality heuristics - customize based on requirements
        var diagnostics = result.Compilation.GetDiagnostics();
        var errorCount = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        
        // Accept if error rate is reasonable (< 50% of total diagnostics)
        var totalDiagnostics = diagnostics.Length;
        if (totalDiagnostics == 0) return true;
        
        var errorRate = (double)errorCount / totalDiagnostics;
        return errorRate < 0.5; // Accept if less than 50% errors
    }
}
```

#### Strategy Factory and Selection Logic

```csharp
public interface ICompilationStrategyFactory
{
    ICompilationStrategy CreateStrategy(CompilationStrategy strategyType);
    ICompilationStrategy SelectOptimalStrategy(string projectPath, AnalysisOptions options);
}

public class CompilationStrategyFactory : ICompilationStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompilationStrategyFactory> _logger;
    
    public CompilationStrategyFactory(IServiceProvider serviceProvider, ILogger<CompilationStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public ICompilationStrategy CreateStrategy(CompilationStrategy strategyType)
    {
        return strategyType switch
        {
            CompilationStrategy.MSBuild => _serviceProvider.GetRequiredService<MSBuildWorkspaceStrategy>(),
            CompilationStrategy.Custom => _serviceProvider.GetRequiredService<CustomCompilationStrategy>(),
            CompilationStrategy.Hybrid => _serviceProvider.GetRequiredService<HybridCompilationStrategy>(),
            CompilationStrategy.Auto => SelectOptimalStrategy(string.Empty, new AnalysisOptions()),
            _ => throw new ArgumentException($"Unknown compilation strategy: {strategyType}")
        };
    }
    
    public ICompilationStrategy SelectOptimalStrategy(string projectPath, AnalysisOptions options)
    {
        // Intelligent strategy selection based on project characteristics
        
        // For solutions, prefer MSBuild or Hybrid
        if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Solution file detected, selecting Hybrid strategy for {ProjectPath}", projectPath);
            return _serviceProvider.GetRequiredService<HybridCompilationStrategy>();
        }
        
        // For semantic analysis, prefer MSBuild for better accuracy
        if (options.SemanticDepth != SemanticAnalysisDepth.None)
        {
            _logger.LogDebug("Semantic analysis requested, selecting Hybrid strategy for {ProjectPath}", projectPath);
            return _serviceProvider.GetRequiredService<HybridCompilationStrategy>();
        }
        
        // For lightweight analysis, Custom might be sufficient
        if (options.AnalysisDepth == AnalysisDepth.Surface && 
            options.SemanticDepth == SemanticAnalysisDepth.None)
        {
            _logger.LogDebug("Lightweight analysis requested, selecting Custom strategy for {ProjectPath}", projectPath);
            return _serviceProvider.GetRequiredService<CustomCompilationStrategy>();
        }
        
        // Default to Hybrid for best results with fallback
        _logger.LogDebug("Default strategy selection: Hybrid for {ProjectPath}", projectPath);
        return _serviceProvider.GetRequiredService<HybridCompilationStrategy>();
    }
}
```

// MSBuild workspace strategy (primary)
public class MSBuildWorkspaceStrategy : ICompilationStrategy
{
    private readonly MSBuildWorkspace _workspace;
    private readonly ILogger<MSBuildWorkspaceStrategy> _logger;
    
    public async Task<CompilationResult> CreateCompilationAsync(string projectPath, CompilationOptions options)
    {
        // Full MSBuild integration with timeout and error handling
        using var cancellation = new CancellationTokenSource(options.MSBuildTimeout);
        var project = await _workspace.OpenProjectAsync(projectPath, cancellation.Token);
        return new CompilationResult(await project.GetCompilationAsync(), CompilationStrategy.MSBuild);
    }
}

// Custom compilation strategy (fallback)
public class CustomCompilationStrategy : ICompilationStrategy  
{
    public async Task<CompilationResult> CreateCompilationAsync(string projectPath, CompilationOptions options)
    {
        // Lightweight custom compilation for specific scenarios
        var syntaxTrees = await LoadSyntaxTreesAsync(projectPath);
        var references = await ResolveReferencesAsync(projectPath, options);
        
        var compilation = CSharpCompilation.Create(
            assemblyName: Path.GetFileNameWithoutExtension(projectPath),
            syntaxTrees: syntaxTrees,
            references: references);
            
        return new CompilationResult(compilation, CompilationStrategy.Custom);
    }
}

// Hybrid strategy coordinator
public class HybridCompilationStrategy : ICompilationStrategy
{
    private readonly MSBuildWorkspaceStrategy _msbuildStrategy;
    private readonly CustomCompilationStrategy _customStrategy;
    private readonly ILogger<HybridCompilationStrategy> _logger;
    
    public async Task<CompilationResult> CreateCompilationAsync(string projectPath, CompilationOptions options)
    {
        try
        {
            // Try MSBuild first for comprehensive analysis
            if (_msbuildStrategy.CanHandle(projectPath, options))
            {
                var result = await _msbuildStrategy.CreateCompilationAsync(projectPath, options);
                if (result.Success)
                {
                    _logger.LogInformation("MSBuild compilation successful for {ProjectPath}", projectPath);
                    return result;
                }
            }
        }
        catch (Exception ex) when (options.FallbackToCustom)
        {
            _logger.LogWarning(ex, "MSBuild compilation failed, falling back to custom compilation");
        }
        
        // Fallback to custom compilation
        if (options.FallbackToCustom)
        {
            var customResult = await _customStrategy.CreateCompilationAsync(projectPath, options);
            customResult.FallbackReason = "MSBuild compilation failed or unavailable";
            return customResult;
        }
        
        throw new InvalidOperationException("No suitable compilation strategy available");
    }
}
```

### Analysis Pipeline Architecture
```csharp
// Core analysis engines that work with any compilation strategy
public class SemanticAnalysisEngine
{
    public async Task<SemanticAnalysis> AnalyzeAsync(Compilation compilation, AnalysisOptions options)
    {
        var result = new SemanticAnalysis 
        { 
            DepthUsed = options.SemanticDepth,
            CompilationRequired = options.SemanticDepth != SemanticAnalysisDepth.None
        };

        // Early return for no semantic analysis
        if (options.SemanticDepth == SemanticAnalysisDepth.None)
        {
            return result;
        }

        // Progressive semantic analysis based on depth
        switch (options.SemanticDepth)
        {
            case SemanticAnalysisDepth.Basic:
                result.Types = await ExtractBasicTypes(compilation);
                result.Namespaces = await ExtractNamespaces(compilation);
                break;
                
            case SemanticAnalysisDepth.Standard:
                result.Types = await ExtractTypes(compilation);
                result.Namespaces = await ExtractNamespaces(compilation);
                result.Members = await ExtractMembers(compilation);
                result.Symbols = await ExtractSymbols(compilation);
                break;
                
            case SemanticAnalysisDepth.Deep:
                result.Types = await ExtractTypes(compilation);
                result.Namespaces = await ExtractNamespaces(compilation);
                result.Members = await ExtractMembers(compilation);
                result.Symbols = await ExtractSymbols(compilation);
                result.InheritanceChains = await AnalyzeInheritance(compilation);
                result.InterfaceImplementations = await AnalyzeInterfaces(compilation);
                break;
                
            case SemanticAnalysisDepth.Comprehensive:
                // Full semantic analysis with all features
                result = await PerformComprehensiveAnalysis(compilation);
                break;
        }
        
        return result;
    }
}

public class MetricsAnalysisEngine  
{
    public async Task<CodeMetrics> CalculateMetricsAsync(Compilation compilation, AnalysisOptions options)
    {
        // Works with both MSBuild and custom compilations
        return new CodeMetrics
        {
            Complexity = await CalculateComplexity(compilation),
            Maintainability = await CalculateMaintainability(compilation),
            TechnicalDebt = await AssessTechnicalDebt(compilation),
            SizeMetrics = await CalculateSizeMetrics(compilation)
        };
    }
}

public class PatternDetectionEngine
{
    public async Task<ArchitecturalPatterns> DetectPatternsAsync(Compilation compilation, AnalysisOptions options)
    {
        // Pattern detection requires at least Deep semantic analysis
        if (options.SemanticDepth < SemanticAnalysisDepth.Deep)
        {
            return new ArchitecturalPatterns 
            { 
                RequiresDeepSemantics = true,
                MinimumDepthRequired = SemanticAnalysisDepth.Deep
            };
        }

        // Full pattern detection with comprehensive semantic information
        return new ArchitecturalPatterns
        {
            DesignPatterns = await DetectDesignPatterns(compilation),
            ArchitecturalLayers = await AnalyzeLayers(compilation),
            DependencyPatterns = await AnalyzeDependencyPatterns(compilation),
            SolidCompliance = await AnalyzeSolidCompliance(compilation),
            CodeSmells = await DetectCodeSmells(compilation)
        };
    }
}

public class RoslynAnalysisService : IRoslynAnalysisService
{
    public async Task<string> AnalyzeAsync(
        string projectPath, 
        AnalysisOptions options, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new RoslynAnalysisResult
        {
            ProjectPath = projectPath,
            AnalysisTimestamp = startTime.ToString("O"),
            AnalysisDepth = options.AnalysisDepth,
            SemanticDepth = options.SemanticDepth,
            CompilationStrategy = options.CompilationStrategy
        };
        
        try
        {
            // Always perform structural analysis (fast)
            result.Metadata = await ExtractProjectMetadata(projectPath);
            result.Structure = await AnalyzeProjectStructure(projectPath);
            result.Dependencies = await AnalyzeDependencies(projectPath, options);
            
            // Conditional compilation and semantic analysis
            if (RequiresCompilation(options))
            {
                var compilation = await CreateCompilation(projectPath, options);
                result.Compilation = MapCompilationInfo(compilation);
                
                // Progressive semantic analysis
                if (options.SemanticDepth != SemanticAnalysisDepth.None)
                {
                    result.Semantics = await _semanticEngine.AnalyzeAsync(compilation.Compilation, options);
                }
                
                // Optional advanced analysis
                if (options.IncludeMetrics)
                {
                    result.Metrics = await _metricsEngine.CalculateMetricsAsync(compilation.Compilation, options);
                }
                
                if (options.IncludePatterns)
                {
                    result.Patterns = await _patternEngine.DetectPatternsAsync(compilation.Compilation, options);
                }
            }
            
            // Generate AI-friendly recommendations
            result.Recommendations = await GenerateRecommendations(result, options);
            
            // Performance metrics
            result.Performance = CalculatePerformanceMetrics(startTime);
            result.Success = true;
            result.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // Return clean JSON
            return JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }
    }
    
    private bool RequiresCompilation(AnalysisOptions options)
    {
        return options.SemanticDepth != SemanticAnalysisDepth.None ||
               options.IncludeMetrics ||
               options.IncludePatterns ||
               options.IncludeVulnerabilities;
    }
}
```

### Service Registration and Dependency Injection

#### Complete DI Container Setup

```csharp
public static class RoslynAnalysisServiceCollectionExtensions
{
    public static IServiceCollection AddRoslynAnalysisServices(this IServiceCollection services)
    {
        // Initialize MSBuild before registering services that depend on it
        MSBuildSetup.EnsureInitialized();
        
        // Core analysis service
        services.AddScoped<IRoslynAnalysisService, RoslynAnalysisService>();
        
        // Compilation strategies
        services.AddScoped<MSBuildWorkspaceStrategy>();
        services.AddScoped<CustomCompilationStrategy>();
        services.AddScoped<HybridCompilationStrategy>();
        
        // Strategy factory
        services.AddScoped<ICompilationStrategyFactory, CompilationStrategyFactory>();
        
        // Analysis engines
        services.AddScoped<ISyntaxAnalysisEngine, SyntaxAnalysisEngine>();
        services.AddScoped<ISemanticAnalysisEngine, SemanticAnalysisEngine>();
        services.AddScoped<IMetricsAnalysisEngine, MetricsAnalysisEngine>();
        services.AddScoped<IDependencyAnalysisEngine, DependencyAnalysisEngine>();
        
        // Configuration options
        services.Configure<RoslynAnalysisOptions>(options =>
        {
            options.DefaultStrategy = CompilationStrategy.Hybrid;
            options.MSBuildTimeout = TimeSpan.FromMinutes(2);
            options.EnableSemanticAnalysis = true;
            options.EnableMetricsCalculation = true;
            options.MaxConcurrentAnalysis = Environment.ProcessorCount;
            options.CacheCompilations = true;
            options.FallbackToCustom = true;
        });
        
        // Caching services
        services.AddMemoryCache();
        services.AddScoped<ICompilationCacheService, CompilationCacheService>();
        
        // Error handling and diagnostics
        services.AddScoped<MSBuildDiagnosticsHandler>();
        
        // Add to Semantic Kernel if available
        services.TryAddSingleton<ProjectAnalysisPlugin>();
        
        return services;
    }
}

// Configuration options model
public class RoslynAnalysisOptions
{
    public CompilationStrategy DefaultStrategy { get; set; } = CompilationStrategy.Hybrid;
    public TimeSpan MSBuildTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool EnableSemanticAnalysis { get; set; } = true;
    public bool EnableMetricsCalculation { get; set; } = true;
    public int MaxConcurrentAnalysis { get; set; } = 4;
    public bool CacheCompilations { get; set; } = true;
    public bool FallbackToCustom { get; set; } = true;
    public bool IncludeGeneratedCode { get; set; } = false;
    public string[] ExcludedDirectories { get; set; } = { "bin", "obj", ".git", "node_modules" };
    public string[] IncludedFilePatterns { get; set; } = { "*.cs", "*.csproj", "*.sln" };
}
```

#### Enhanced Configuration Management

```csharp
// Enhanced analysis options with MSBuild specifics
public class AnalysisOptions
{
    // Core analysis settings
    public AnalysisDepth AnalysisDepth { get; set; } = AnalysisDepth.Comprehensive;
    public SemanticAnalysisDepth SemanticDepth { get; set; } = SemanticAnalysisDepth.Symbols;
    public CompilationStrategy PreferredStrategy { get; set; } = CompilationStrategy.Auto;
    
    // MSBuild-specific options
    public bool FallbackToCustom { get; set; } = true;
    public TimeSpan MSBuildTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public string? TargetFramework { get; set; }
    public string? Configuration { get; set; } = "Debug";
    public Dictionary<string, string> MSBuildProperties { get; set; } = new();
    
    // Output and filtering
    public bool IncludeDependencies { get; set; } = true;
    public bool IncludeGeneratedCode { get; set; } = false;
    public bool IncludeTests { get; set; } = true;
    public string[] ExcludedNamespaces { get; set; } = Array.Empty<string>();
    public string[] IncludedFilePatterns { get; set; } = { "*.cs" };
    
    // Performance settings
    public int MaxConcurrentFiles { get; set; } = Environment.ProcessorCount;
    public bool EnableCaching { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    
    // Analysis features
    public bool CalculateMetrics { get; set; } = true;
    public bool AnalyzeSymbols { get; set; } = true;
    public bool DetectPatterns { get; set; } = false;
    public bool IncludeDocumentation { get; set; } = false;
}

// Compilation-specific options
public class AnalysisCompilationOptions
{
    public TimeSpan MSBuildTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool FallbackToCustom { get; set; } = true;
    public string? TargetFramework { get; set; }
    public string? Configuration { get; set; } = "Debug";
    public Dictionary<string, string> MSBuildProperties { get; set; } = new();
    public bool IncludeGeneratedCode { get; set; } = false;
    public MetadataReferenceResolver? MetadataResolver { get; set; }
}
```

#### Performance and Caching Implementation

```csharp
public interface ICompilationCacheService
{
    Task<CompilationResult?> GetCachedCompilationAsync(string projectPath, string contentHash);
    Task CacheCompilationAsync(string projectPath, string contentHash, CompilationResult result);
    Task InvalidateCacheAsync(string projectPath);
    Task<string> ComputeContentHashAsync(string projectPath);
}

public class CompilationCacheService : ICompilationCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CompilationCacheService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    
    public CompilationCacheService(IMemoryCache cache, ILogger<CompilationCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public Task<CompilationResult?> GetCachedCompilationAsync(string projectPath, string contentHash)
    {
        var cacheKey = GenerateCacheKey(projectPath, contentHash);
        
        if (_cache.TryGetValue(cacheKey, out CompilationResult? cachedResult))
        {
            _logger.LogDebug("Cache hit for compilation of {ProjectPath}", projectPath);
            return Task.FromResult(cachedResult);
        }
        
        _logger.LogDebug("Cache miss for compilation of {ProjectPath}", projectPath);
        return Task.FromResult<CompilationResult?>(null);
    }
    
    public Task CacheCompilationAsync(string projectPath, string contentHash, CompilationResult result)
    {
        if (!result.Success)
        {
            _logger.LogDebug("Not caching failed compilation for {ProjectPath}", projectPath);
            return Task.CompletedTask;
        }
        
        var cacheKey = GenerateCacheKey(projectPath, contentHash);
        
        // Create a lightweight cached version (without compilation object to save memory)
        var cachedResult = new CompilationResult
        {
            Success = result.Success,
            StrategyUsed = result.StrategyUsed,
            FallbackUsed = result.FallbackUsed,
            FallbackReason = result.FallbackReason,
            ErrorMessage = result.ErrorMessage,
            CompilationTimeMs = result.CompilationTimeMs,
            ProjectMetadata = result.ProjectMetadata,
            TargetFramework = result.TargetFramework,
            WorkspaceDiagnostics = result.WorkspaceDiagnostics,
            Diagnostics = result.Diagnostics,
            // Note: Compilation object not cached to avoid memory issues
            Compilation = null
        };
        
        _cache.Set(cacheKey, cachedResult, _cacheExpiration);
        _logger.LogDebug("Cached compilation result for {ProjectPath}", projectPath);
        
        return Task.CompletedTask;
    }
    
    public Task InvalidateCacheAsync(string projectPath)
    {
        // In a real implementation, we'd track all cache keys for a project
        _logger.LogDebug("Cache invalidation requested for {ProjectPath}", projectPath);
        return Task.CompletedTask;
    }
    
    public async Task<string> ComputeContentHashAsync(string projectPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compute content hash for {ProjectPath}", projectPath);
            return DateTime.UtcNow.Ticks.ToString(); // Fallback to timestamp
        }
    }
    
    private string GenerateCacheKey(string projectPath, string contentHash)
    {
        return $"compilation:{Path.GetFileName(projectPath)}:{contentHash}";
    }
}

// MSBuild diagnostics handler for better error reporting
public class MSBuildDiagnosticsHandler
{
    private readonly ILogger<MSBuildDiagnosticsHandler> _logger;
    
    public MSBuildDiagnosticsHandler(ILogger<MSBuildDiagnosticsHandler> logger)
    {
        _logger = logger;
    }
    
    public void ProcessWorkspaceDiagnostics(IEnumerable<WorkspaceDiagnostic> diagnostics, string projectPath)
    {
        var diagnosticsList = diagnostics.ToList();
        if (!diagnosticsList.Any()) return;
        
        var errors = diagnosticsList.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure).ToList();
        var warnings = diagnosticsList.Where(d => d.Kind == WorkspaceDiagnosticKind.Warning).ToList();
        
        if (errors.Any())
        {
            _logger.LogWarning("MSBuild workspace has {ErrorCount} errors for {ProjectPath}:", 
                errors.Count, projectPath);
            
            foreach (var error in errors.Take(5)) // Log first 5 errors
            {
                _logger.LogWarning("  Error: {Message}", error.Message);
            }
            
            if (errors.Count > 5)
            {
                _logger.LogWarning("  ... and {AdditionalCount} more errors", errors.Count - 5);
            }
        }
        
        if (warnings.Any())
        {
            _logger.LogDebug("MSBuild workspace has {WarningCount} warnings for {ProjectPath}", 
                warnings.Count, projectPath);
        }
        
        CheckForCommonIssues(diagnosticsList, projectPath);
    }
    
    private void CheckForCommonIssues(List<WorkspaceDiagnostic> diagnostics, string projectPath)
    {
        var messages = diagnostics.Select(d => d.Message).ToList();
        
        // Check for SDK not found
        if (messages.Any(m => m.Contains("SDK") && m.Contains("not found")))
        {
            _logger.LogError("MSBuild SDK issues detected for {ProjectPath}. " +
                           "Ensure proper .NET SDK is installed and accessible.", projectPath);
        }
        
        // Check for target framework issues
        if (messages.Any(m => m.Contains("TargetFramework") || m.Contains("target framework")))
        {
            _logger.LogWarning("Target framework issues detected for {ProjectPath}. " +
                             "Project may use unsupported or missing target framework.", projectPath);
        }
        
        // Check for package reference issues
        if (messages.Any(m => m.Contains("PackageReference") || m.Contains("package")))
        {
            _logger.LogWarning("Package reference issues detected for {ProjectPath}. " +
                             "Some NuGet packages may be missing or incompatible.", projectPath);
        }
    }
    
    public bool ShouldFallbackToCustom(IEnumerable<WorkspaceDiagnostic> diagnostics, CompilationResult? result)
    {
        var diagnosticsList = diagnostics.ToList();
        var criticalErrors = diagnosticsList.Count(d => d.Kind == WorkspaceDiagnosticKind.Failure);
        
        // Fallback if too many critical errors
        if (criticalErrors > 10)
        {
            _logger.LogWarning("Too many MSBuild errors ({Count}), recommending fallback to custom compilation", criticalErrors);
            return true;
        }
        
        // Fallback if compilation completely failed
        if (result?.Compilation == null)
        {
            _logger.LogWarning("MSBuild compilation produced no result, recommending fallback to custom compilation");
            return true;
        }
        
        // Fallback if too many compilation errors
        var compilationErrors = result.Compilation.GetDiagnostics()
            .Count(d => d.Severity == DiagnosticSeverity.Error);
            
        if (compilationErrors > 50)
        {
            _logger.LogWarning("Too many compilation errors ({Count}), recommending fallback to custom compilation", compilationErrors);
            return true;
        }
        
        return false;
    }
}
```
```

### Performance Considerations
- **Semantic Analysis Gating**: No compilation overhead when semantic_depth is "None"
- **Progressive Compilation**: Only create compilation objects when semantic analysis is requested
- **Strategy Selection Optimization**: Intelligent selection based on project characteristics and semantic requirements
- **MSBuild Workspace Management**: Proper disposal and timeout handling for MSBuild resources
- **Compilation Caching**: Intelligent caching system that works across compilation strategies
- **Memory Management**: Lightweight caching and resource cleanup for large projects

## Implementation Status

✅ **COMPLETE SPECIFICATION** - This document now provides comprehensive implementation guidance for the Roslyn Analysis Built-in Tool including:

- **MSBuild Integration Patterns**: Complete MSBuildWorkspace setup with MSBuildLocator initialization
- **Strategy Pattern Architecture**: MSBuildWorkspaceStrategy (primary), CustomCompilationStrategy (fallback), HybridCompilationStrategy (coordinator)
- **Service Registration**: Full dependency injection configuration with options patterns
- **Performance Optimization**: Caching, resource management, and intelligent strategy selection
- **Error Handling**: Comprehensive diagnostics processing and fallback mechanisms
- **Configuration Management**: Complete options models with MSBuild-specific settings

## Next Steps

1. **Implement hybrid compilation strategy** - Create the strategy pattern architecture with MSBuildWorkspace primary and custom compilation fallback
2. **Design intelligent strategy selection** - Implement logic to automatically choose optimal compilation approach based on project characteristics
3. **Create unified analysis pipeline** - Build analysis engines that work seamlessly with both compilation strategies
4. **Implement Semantic Kernel plugin interface** - Create SK functions that return structured JSON for AI consumption
5. **Design cross-strategy caching system** - Implement intelligent caching that works with both MSBuild and custom compilations
6. **Create comprehensive test scenarios** - Test hybrid approach against various project types, sizes, and failure scenarios with JSON validation
7. **Integrate security and vulnerability scanning** - Add third-party security analysis tools with JSON-formatted results
8. **Develop AI-optimized JSON output** - Implement clean, structured JSON format with actionable recommendations
9. **Implement performance monitoring and telemetry** - Track performance differences between compilation strategies in JSON metadata
10. **Create recommendation system** - Build AI-friendly recommendation engine with actionable insights in JSON format

## References

- **Roslyn SDK**: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/
- **Roslyn GitHub**: https://github.com/dotnet/roslyn
- **Semantic Kernel Integration**: Follow project's SK-first architecture patterns
- **Performance Guidelines**: Roslyn best practices for tooling scenarios
