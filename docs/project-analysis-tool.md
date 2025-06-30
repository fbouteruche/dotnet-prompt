# Project Analysis Tool Specification

## Overview

This document defines the detailed specification for the Project Analysis Tool, which analyzes .NET projects and solutions to extract metadata, dependencies, and structure information for AI workflow decision-making.

## Status
🚧 **DRAFT** - Requires detailed specification

## Purpose

The Project Analysis Tool provides comprehensive analysis of .NET projects and solutions, extracting structured information that can be used by AI workflows to make informed decisions about code generation, testing, documentation, and other development tasks.

## Core Functionality

### Project Metadata Extraction
- Project name, version, and description
- Target frameworks and runtime information
- Assembly information and build properties
- Project type identification (Console, Library, Web, Test, etc.)

### Dependency Analysis
- NuGet package references and versions
- Project-to-project references
- Transitive dependency mapping
- Package vulnerability scanning
- License information extraction

### Source Code Structure Analysis
- File organization and structure
- Namespace and class discovery
- Code metrics and complexity analysis
- Test coverage estimation

### Build Configuration Analysis
- MSBuild properties and targets
- Build configurations (Debug, Release, custom)
- Output paths and deployment settings

## Input Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `project_path` | string | ✅ | - | Path to .csproj or .sln file |
| `include_dependencies` | bool | ❌ | true | Analyze NuGet packages |
| `include_source_files` | bool | ❌ | true | Include source file analysis |
| `include_build_config` | bool | ❌ | true | Include build configuration |
| `analyze_references` | bool | ❌ | false | Recursive project analysis |
| `max_depth` | int | ❌ | 3 | Maximum recursion depth |

## Output Format

```typescript
interface ProjectAnalysisResult {
    project: ProjectMetadata;
    dependencies: DependencyInfo[];
    sourceFiles: SourceFileInfo[];
    buildConfig: BuildConfiguration;
    projectReferences: ProjectReference[];
    metrics: CodeMetrics;
    warnings: string[];
}
```

## Clarifying Questions

### 1. Project Metadata Extraction
- What specific project metadata should be extracted beyond basic name/version?
- How should multi-target projects be handled (analyze all targets vs specific target)?
- Should the tool extract custom MSBuild properties?
- How should project type detection work for complex scenarios?

### 2. Dependency Analysis
- Should NuGet package analysis include vulnerability scanning?
- How deep should transitive dependency analysis go?
- Should the tool check for package update availability?
- How should package license information be extracted and reported?
- Should there be analysis of package usage patterns within the code?

### 3. Source Code Analysis
- What level of source code analysis is needed (syntax trees, symbols, semantics)?
- Should the tool analyze code complexity metrics (cyclomatic complexity, maintainability index)?
- How should test file detection and analysis work?
- Should there be pattern detection for common architectural patterns?
- How should the tool handle large codebases efficiently?

### 4. Multi-Project Solutions
- How should solution-level analysis work?
- Should project dependencies be mapped as a graph?
- How should shared projects and project references be handled?
- What solution-level metrics should be calculated?

### 5. Performance and Scalability
- What are acceptable limits for project size and analysis time?
- Should there be incremental analysis capabilities?
- How should the tool handle very large solutions (hundreds of projects)?
- Should analysis results be cached and how should cache invalidation work?

### 6. Security and Compliance
- Should the tool scan for security vulnerabilities in dependencies?
- How should sensitive information in project files be handled?
- Should there be compliance checking (license compatibility, etc.)?
- What audit logging is needed for analysis operations?

### 7. Integration with AI Workflows
- How should analysis results be formatted for optimal AI consumption?
- What summary information should be provided for large projects?
- Should there be different output formats for different use cases?
- How should the tool highlight important findings for AI attention?

### 8. Error Handling and Edge Cases
- How should the tool handle corrupted or invalid project files?
- What should happen when dependencies cannot be resolved?
- How should missing files or broken references be reported?
- Should there be partial analysis capabilities when some operations fail?

## Example Usage Scenarios

### Basic Project Analysis
```markdown
Analyze the current project to understand its structure:

{{invoke_tool: analyze_project
  project_path: "./MyApp.csproj"
  include_dependencies: true
  include_source_files: true
}}
```

### Solution-wide Analysis
```markdown
Perform comprehensive analysis of the entire solution:

{{invoke_tool: analyze_project
  project_path: "./MySolution.sln"
  analyze_references: true
  max_depth: 5
}}
```

### Dependency-focused Analysis
```markdown
Focus on dependency analysis for security review:

{{invoke_tool: analyze_project
  project_path: "./MyApp.csproj"
  include_dependencies: true
  include_source_files: false
  include_build_config: false
}}
```

## Next Steps

1. Answer the clarifying questions above
2. Define the complete output data structures
3. Specify error handling and edge cases
4. Design the caching and performance optimization strategy
5. Create comprehensive examples and test cases
6. Implement the Semantic Kernel plugin interface
