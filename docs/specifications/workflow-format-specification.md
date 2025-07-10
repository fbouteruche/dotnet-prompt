# Workflow File Format Specification

## Overview

This document defines the exact format and structure for .prompt.md workflow files, following the [dotprompt format specification](https://google.github.io/dotprompt/reference/frontmatter/) with YAML frontmatter schema, markdown content structure, and validation rules.

## Status
✅ **COMPLETE** - Standard dotprompt format specification defined

## Dotprompt YAML Frontmatter Schema

### Core Dotprompt Fields

Following the standard dotprompt format specification:

```yaml
---
# Required/Core Fields
name: "project-analysis-workflow"
model: "github/gpt-4o"                    # Provider/model specification

# Model Configuration
config:
  temperature: 0.7
  maxOutputTokens: 4000
  topP: 1.0
  topK: 40
  stopSequences: ["END", "\n---\n"]

# Tool Integration
tools:
  - "project-analysis"                    # Built-in tools
  - "build-test"
  - "file-system"

# Input Parameters
input:
  default:
    include_dependencies: true
    output_directory: "./docs"
  schema:
    project_path: 
      type: string
      description: "Path to the .NET project file (.csproj/.fsproj/.vbproj)"
    output_directory:
      type: string  
      description: "Directory for generated documentation"
      default: "./generated-docs"  # Schema-level default (takes precedence)
    include_dependencies:
      type: boolean
      description: "Include dependency analysis in the output"
      # Uses workflow-level default since no schema-level default specified

# Output Format
output:
  format: text
  schema:
    analysis_result:
      type: object
      description: "Structured analysis results"
    generated_files:
      type: array
      description: "List of generated documentation files"

# Metadata for workflow management
metadata:
  description: "Comprehensive .NET project analysis with automated documentation generation"
  author: "dotnet-prompt"
  version: "1.0.0"
  tags: ["analysis", "documentation", "dotnet"]
---
```

### Extension Fields for dotnet-prompt

Using dotprompt's extension mechanism for tool-specific configuration:

```yaml
---
# Standard dotprompt fields
name: "advanced-project-workflow"
model: "github/gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

# dotnet-prompt extensions (using namespaced fields)
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      root_path: "./project"
  - server: "git-mcp" 
    version: "2.1.0"
    config:
      repository: "."

dotnet-prompt.sub-workflows:
  - name: "detailed-analysis"
    path: "./analysis/detailed-project-analysis.prompt.md"
    parameters:
      analysis_depth: "comprehensive"
  - name: "api-documentation"
    path: "./docs/generate-api-docs.prompt.md"
    depends_on: ["detailed-analysis"]

dotnet-prompt.progress:
  enabled: true
  checkpoint_frequency: "after_each_tool"
  storage_location: "./.dotnet-prompt/progress"

dotnet-prompt.error-handling:
  retry_attempts: 3
  backoff_strategy: "exponential"
  timeout_seconds: 300
---
```

## Standard Dotprompt Markdown Content

### Natural Language Workflow (Recommended)
```markdown
# Project Analysis and Documentation Generation

I need to perform a comprehensive analysis of this .NET project and generate detailed documentation.

## Analysis Phase

Please analyze the project at `{{project_path}}` with the following requirements:
- Examine the project structure and identify all source files
- Analyze dependencies and detect any security vulnerabilities  
- Generate metrics for code quality and test coverage
- Document the architecture and component relationships

## Documentation Phase

Based on the analysis results, generate comprehensive documentation:
- Create API documentation with examples for all public interfaces
- Generate a detailed README with setup and usage instructions
- Document the project architecture with diagrams
- Create developer onboarding documentation

The analysis will use depth setting: `{{analysis_depth}}` (resolved from schema-level default: "comprehensive")

## Validation Phase

Finally, validate all generated documentation:
- Check for consistency across all documentation files
- Verify that all code examples compile and run correctly
- Ensure documentation follows established style guidelines
- Generate a documentation quality report

Please save all outputs to the `{{output_directory}}` directory and provide a summary of what was generated.
```

### Explicit Tool Calls (When Needed)
```markdown
# Explicit Project Analysis Workflow

## Step 1: Project Structure Analysis

Analyze the project structure:
{{project_path: "{{project_path}}"}}
{{include_dependencies: "{{include_dependencies}}"}}
{{include_tests: true}}

## Step 2: Generate API Documentation

Based on the analysis results, create comprehensive API documentation for all public interfaces with usage examples.

## Step 3: Create Project README

Generate a README file at `{{output_directory}}/README.md` that includes:
- Project overview and description
- Installation instructions
- Quick start guide
- API reference links

Generated on: {{current_date}}
```

### Sub-workflow References
```markdown
# Main Analysis Workflow

First, perform detailed project analysis:

> Execute: ./analysis/detailed-project-analysis.prompt.md
> Parameters: 
> - project_path: "{{project_path}}"
> - analysis_depth: "comprehensive" 
> - include_tests: true

Then generate documentation based on the analysis:

> Execute: ./docs/generate-api-docs.prompt.md
> Parameters:
> - project_metadata: "{{analysis_result.metadata}}"
> - output_format: "markdown"
> - include_examples: true

Finally, validate all generated documentation for consistency and completeness.
```

## Parameter Substitution

### Default Value Precedence

According to the dotprompt specification, parameter defaults can be specified in multiple locations with the following precedence hierarchy (highest to lowest priority):

1. **CLI Parameters** (highest priority) - Values passed via command line flags
2. **Schema-level Defaults** - `input.schema.{parameter}.default`
3. **Workflow-level Defaults** - `input.default.{parameter}`
4. **No Value** - Parameter remains undefined in template context

#### Example with Precedence:
```yaml
input:
  default:
    query: "Default search query"           # Priority 3: Workflow-level
    format: "markdown"                      # Priority 3: Only location
  schema:
    query:
      type: string
      description: "Search query parameter"
      default: "Schema-level query"         # Priority 2: Takes precedence over workflow-level
    format:
      type: string
      description: "Output format"
      # Uses workflow-level default since no schema-level default
    custom_param:
      type: string
      description: "Custom parameter"
      default: "Schema-only default"        # Priority 2: Only location
```

**Resolved Values:**
- `query`: `"Schema-level query"` (schema-level takes precedence)
- `format`: `"markdown"` (workflow-level, no schema conflict)
- `custom_param`: `"Schema-only default"` (schema-level only)

### Standard Parameter References
- `{{parameter_name}}`: Direct parameter substitution from input schema
- `{{analysis_result.property}}`: Access to previous tool results
- `{{current_date}}`: Built-in system variables
- `{{project_metadata.name}}`: Nested object property access

### Complex Parameter Examples
```yaml
input:
  schema:
    project_config:
      type: object
      properties:
        name: {type: string}
        version: {type: string}
        target_framework: {type: string}
    build_options:
      type: array
      items: {type: string}
```

Usage in content:
```markdown
Building project {{project_config.name}} version {{project_config.version}} 
for {{project_config.target_framework}} with options: {{build_options}}
```

## Validation Rules

### Required Elements
1. **Valid YAML frontmatter**: Must parse as valid YAML between `---` delimiters
2. **Model specification**: Either in `model` field or global configuration
3. **Input schema validation**: Parameters must match defined input schema
4. **Tool dependencies**: All referenced tools must be available (built-in or MCP)
5. **Default value consistency**: Schema-level and workflow-level defaults should not conflict

### Optional Elements
- Frontmatter can be completely omitted for simple workflows
- All dotprompt fields are optional and have reasonable defaults
- Extension fields provide additional functionality without breaking compatibility

### Validation Levels

#### Syntax Validation
```bash
dotnet prompt validate workflow.prompt.md
```
- YAML frontmatter syntax
- Parameter substitution syntax
- Sub-workflow reference syntax
- Tool reference validation

#### Advanced Validation
```bash
# Check for default value conflicts
dotnet prompt validate workflow.prompt.md --check-defaults

# Validate with specific warnings
dotnet prompt validate workflow.prompt.md --warnings=all

# Validate parameter usage throughout workflow
dotnet prompt validate workflow.prompt.md --check-parameter-usage
```

#### Semantic Validation
- Parameter type checking
- Tool availability verification
- Sub-workflow dependency validation
- Model/provider compatibility
- **Default value conflict detection**: Warns when same parameter has different defaults in multiple locations
- **Default value redundancy check**: Identifies unnecessary duplication of identical defaults

#### Runtime Validation
- File path existence (for file parameters)
- Network connectivity (for remote models)
- Tool execution capabilities
- Permission validation

## File Organization

### Standard Directory Structure
```
project-root/
├── .dotnet-prompt/
│   ├── config.json          # Project configuration
│   ├── mcp.json             # MCP server definitions
│   └── progress/            # Progress files
├── workflows/
│   ├── analysis/
│   │   ├── project-analysis.prompt.md
│   │   └── dependency-scan.prompt.md
│   ├── docs/
│   │   ├── api-docs.prompt.md
│   │   └── readme-gen.prompt.md
│   └── main-workflow.prompt.md
└── MyProject.csproj
```

### Workflow Discovery
- Automatic discovery of `.prompt.md` files in current directory and subdirectories
- `dotnet prompt list` shows available workflows with descriptions from metadata
- Sub-workflow references use relative paths from the main workflow file

## Complete Example

### project-analysis.prompt.md
```yaml
---
name: "comprehensive-project-analysis"
model: "github/gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 8000
  stopSequences: ["END_ANALYSIS"]

input:
  default:
    include_dependencies: true
    include_tests: true
    output_format: "markdown"
    analysis_depth: "standard"  # Will be overridden by schema-level default
  schema:
    project_path:
      type: string
      description: "Path to the .NET project file"
    output_directory:
      type: string
      description: "Directory for analysis output"
    include_dependencies:
      type: boolean
      description: "Include dependency analysis"
      # Uses workflow-level default: true
    include_tests:
      type: boolean
      description: "Include test analysis"
      # Uses workflow-level default: true
    output_format:
      type: string
      enum: ["markdown", "json", "html"]
      description: "Format for analysis output"
      # Uses workflow-level default: "markdown"
    analysis_depth:
      type: string
      enum: ["basic", "standard", "comprehensive"]
      description: "Depth of analysis to perform"
      default: "comprehensive"  # Schema-level default takes precedence

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
  description: "Comprehensive .NET project analysis with dependency scanning and test coverage"
  author: "dotnet-prompt team"
  version: "1.2.0"
  tags: ["analysis", "dotnet", "dependencies", "testing"]

# Extension fields for dotnet-prompt specific features
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
  - server: "dependency-scanner-mcp"
    version: "2.1.0"

dotnet-prompt.sub-workflows:
  - name: "security-scan"
    path: "./security/vulnerability-scan.prompt.md"
    condition: "{{include_dependencies}}"
  - name: "documentation-gen" 
    path: "./docs/generate-docs.prompt.md"
    depends_on: ["security-scan"]

dotnet-prompt.progress:
  enabled: true
  checkpoint_frequency: "after_each_tool"
  auto_resume: true
---

# Comprehensive .NET Project Analysis

I need to perform a thorough analysis of the .NET project at `{{project_path}}` and generate comprehensive documentation.

## Project Structure Analysis

First, analyze the overall project structure:
- Examine the project file and configuration
- Identify all source code files and their organization
- Document the project architecture and dependencies
- Analyze the build configuration and target frameworks

## Dependency Analysis

{{#if include_dependencies}}
Perform detailed dependency analysis:
- List all NuGet package references with versions
- Identify potential security vulnerabilities in dependencies
- Check for outdated packages and suggest updates
- Document the dependency graph and potential conflicts
{{/if}}

## Test Coverage Analysis

{{#if include_tests}}
Analyze the test suite:
- Identify all test projects and frameworks used
- Calculate test coverage metrics for the codebase
- Document test patterns and identify gaps in coverage
- Suggest improvements to the testing strategy
{{/if}}

## Documentation Generation

Generate comprehensive documentation:
- Create API documentation for all public interfaces
- Generate architectural overview with component diagrams
- Document setup and deployment procedures
- Create developer onboarding guide

## Output Summary

Save all analysis results to `{{output_directory}}` in {{output_format}} format:
- Project analysis report
- Dependency vulnerability report (if applicable)
- Test coverage report (if applicable)
- Generated documentation files

Provide a structured summary of all findings and generated artifacts.

END_ANALYSIS
```

This example demonstrates:
- **Standard dotprompt compliance**: Using official dotprompt schema
- **Tool integration**: Built-in and MCP tools
- **Parameter handling**: Complex input/output schemas
- **Sub-workflow composition**: Conditional and dependent workflows
- **Extension fields**: dotnet-prompt specific features using namespaced fields
- **Natural language workflow**: AI-friendly prompt structure
- **Conditional logic**: Using handlebars-style conditionals for parameter-driven behavior

## References

### Dotprompt Format Specification
This workflow format is based on and fully compliant with the official [Dotprompt format specification](https://google.github.io/dotprompt/reference/frontmatter/) maintained by Google Firebase team.

**Key Documentation:**
- **Frontmatter Reference**: [https://google.github.io/dotprompt/reference/frontmatter/](https://google.github.io/dotprompt/reference/frontmatter/)
- **Schema Documentation**: [https://google.github.io/dotprompt/reference/frontmatter/schema](https://google.github.io/dotprompt/reference/frontmatter/schema)
- **Dotprompt Overview**: [https://google.github.io/dotprompt/](https://google.github.io/dotprompt/)

### Extension Mechanism
The `dotnet-prompt.*` namespaced fields use dotprompt's [frontmatter extensions](https://google.github.io/dotprompt/reference/frontmatter/#frontmatter-extensions) mechanism to provide tool-specific functionality while maintaining full compatibility with the standard format.

### Standards Compliance
All `.prompt.md` files created for dotnet-prompt are valid dotprompt files that can be used with any dotprompt-compatible implementation. The dotprompt format serves as the authoritative specification for workflow file structure and syntax.
