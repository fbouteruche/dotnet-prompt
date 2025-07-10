# Roslyn Analysis Tool Specification

## Overview

This document defines the detailed specification for the **Roslyn Analysis Built-in Tool**, which leverages the .NET Compiler Platform (Roslyn) to perform deep semantic analysis of .NET projects and solutions. This is a **built-in tool for AI workflows** that provides structured analysis data to enable intelligent AI-powered development tasks.

## Status
🚧 **DRAFT** - Requires detailed specification

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
    Task<CompilationResult> CreateCompilationAsync(string projectPath, CompilationOptions options);
    bool CanHandle(string projectPath, AnalysisOptions options);
    CompilationStrategy StrategyType { get; }
}

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

### Performance Considerations
- **Semantic Analysis Gating**: No compilation overhead when semantic_depth is "None"
- **Progressive Compilation**: Only create compilation objects when semantic analysis is requested
- **Strategy Selection Optimization**: Intelligent selection based on project characteristics and semantic requirements
- **Basic Resource Management**: Proper disposal of Roslyn objects after analysis

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
