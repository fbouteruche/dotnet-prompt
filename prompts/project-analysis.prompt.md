---
name: "project-analysis"
model: "gpt-4o"
tools: ["project-analysis", "file-system", "build-test"]

config:
  temperature: 0.3
  maxOutputTokens: 4000
  stopSequences: ["END_ANALYSIS"]

input:
  default:
    include_dependencies: true
    include_tests: true
    output_format: "markdown"
  schema:
    project_path:
      type: string
      description: "Path to the .NET project file (.csproj/.sln/.fsproj/.vbproj)"
      default: "."
    output_directory:
      type: string
      description: "Directory for analysis output"
      default: "./analysis-output"
    include_dependencies:
      type: boolean
      description: "Include dependency analysis"
      default: true
    include_tests:
      type: boolean
      description: "Include test analysis and coverage metrics"
      default: true
    output_format:
      type: string
      enum: ["markdown", "json", "html"]
      description: "Format for analysis output"
      default: "markdown"

output:
  format: json
  schema:
    project_info:
      type: object
      properties:
        name: {type: string}
        version: {type: string}
        target_framework: {type: string}
    analysis_results:
      type: object
      properties:
        file_count: {type: number}
        dependency_count: {type: number}
        test_coverage: {type: number}
    generated_files:
      type: array
      items: {type: string}

metadata:
  description: "Comprehensive .NET project analysis with dependency and test coverage analysis"
  author: "dotnet-prompt team"
  version: "1.2.0"
  tags: ["analysis", "dotnet", "dependencies", "testing", "project"]
---

# Comprehensive .NET Project Analysis

I need to perform a thorough analysis of the .NET project at `{{project_path}}` and generate detailed documentation and insights.

## Project Structure Analysis

First, analyze the overall project structure:
- Examine the project file(s) and solution structure
- Identify all source code files and their organization  
- Document the project architecture and component relationships
- Analyze the build configuration and target frameworks
- List all project references and dependencies

## Dependency Analysis

{{#if include_dependencies}}
Perform detailed dependency analysis:
- List all NuGet package references with current versions
- Identify any known security vulnerabilities in dependencies
- Check for outdated packages and suggest safe updates
- Analyze the dependency tree for potential conflicts
- Document licensing information for key dependencies
{{/if}}

## Code Quality Assessment

Analyze the codebase for quality metrics:
- Identify code organization patterns and architectural decisions
- Look for potential code smells or improvement opportunities
- Document public API surface and interfaces
- Analyze error handling patterns
- Review coding conventions and consistency

## Test Coverage Analysis

{{#if include_tests}}
Analyze the test suite and coverage:
- Identify all test projects and testing frameworks used
- Document test organization and patterns
- Assess test coverage across different project areas
- Identify areas lacking test coverage
- Suggest testing improvements and missing test scenarios
{{/if}}

## Documentation Generation

Generate comprehensive documentation:
- Create a detailed project overview and architecture summary
- Document all public APIs with descriptions and usage examples
- Generate setup and development environment instructions
- Create troubleshooting guides for common issues
- Document deployment and release procedures

## Security Assessment

Review the project for security considerations:
- Analyze authentication and authorization patterns
- Review data handling and validation approaches  
- Check for common security anti-patterns
- Document security-related dependencies and configurations
- Suggest security improvements where applicable

## Output Summary

Save all analysis results to `{{output_directory}}` in {{output_format}} format:
- **project-overview.{{output_format}}** - High-level project summary
- **architecture-analysis.{{output_format}}** - Detailed architecture documentation
- **dependency-report.{{output_format}}** - Complete dependency analysis
- **test-coverage-report.{{output_format}}** - Test coverage assessment  
- **security-review.{{output_format}}** - Security analysis findings
- **recommendations.{{output_format}}** - Improvement suggestions

Provide a structured JSON summary containing:
- Key project metadata (name, version, frameworks)
- Analysis metrics (file counts, dependency counts, coverage percentages)
- List of all generated documentation files
- High-priority recommendations for improvement

END_ANALYSIS