# Dotprompt Format Specification

This guide explains the dotprompt format used by dotnet-prompt workflows. The format is based on the [official dotprompt specification](https://google.github.io/dotprompt/reference/frontmatter/) with extensions for .NET development scenarios.

## File Structure

Dotprompt files combine YAML frontmatter with Markdown content:

```
---
# YAML frontmatter with configuration
name: "workflow-name"
model: "model-identifier"
tools: ["tool1", "tool2"]
---

# Markdown content with natural language instructions
Your workflow description and instructions here...
```

## File Naming Convention

Workflow files should end with `.prompt.md`:
- `analyze-project.prompt.md`
- `generate-docs.prompt.md`
- `code-review.prompt.md`

## Frontmatter Fields

### Required Fields

#### `name` (string)
Unique identifier for the workflow. Used for progress tracking and logging.

```yaml
name: "project-analysis-workflow"
```

#### `model` (string)
AI model to use for this workflow. Supported formats:
- `"gpt-4o"` - GitHub Models (recommended)
- `"openai/gpt-4"` - OpenAI 
- `"azure/gpt-4"` - Azure OpenAI
- `"anthropic/claude-3-sonnet"` - Anthropic
- `"local/model-name"` - Local models

```yaml
model: "gpt-4o"
```

### Optional Core Fields

#### `tools` (array)
List of tools available to this workflow:

```yaml
tools: 
  - "project-analysis"    # Analyze .NET projects
  - "build-test"          # Build and test operations  
  - "file-system"         # File and directory operations
```

**Available Built-in Tools:**
- `project-analysis`: .NET project and solution analysis
- `build-test`: Build, test, and publish operations
- `file-system`: File and directory operations
- `sub-workflow`: Execute other workflow files

#### `config` (object)
Model-specific configuration:

```yaml
config:
  temperature: 0.7          # Creativity level (0.0-1.0)
  maxOutputTokens: 4000     # Maximum response length
  topP: 0.9                 # Nucleus sampling parameter
  frequencyPenalty: 0.0     # Repetition penalty
  presencePenalty: 0.0      # Topic penalty
```

#### `input` (object)
Define expected input parameters:

```yaml
input:
  schema:
    project_path:
      type: string
      description: "Path to the .NET project file"
      default: "."
    include_tests:
      type: boolean
      description: "Include test analysis"
      default: true
    output_format:
      type: string
      description: "Output format"
      enum: ["markdown", "json", "text"]
      default: "markdown"
```

### dotnet-prompt Extension Fields

#### `dotnet-prompt.mcp` (array)
Model Context Protocol server configuration:

```yaml
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src", "./docs"]
      max_file_size: "10MB"
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
      default_repo: "owner/repository"
```

#### `dotnet-prompt.sub-workflows` (array)
Sub-workflow composition:

```yaml
dotnet-prompt.sub-workflows:
  - name: "detailed-analysis"
    path: "./analysis/detailed-analysis.prompt.md"
    parameters:
      depth: "comprehensive"
    depends_on: []
  - name: "documentation"
    path: "./docs/generate-docs.prompt.md" 
    parameters:
      format: "markdown"
    depends_on: ["detailed-analysis"]
```

#### `dotnet-prompt.progress` (object)
Progress tracking configuration:

```yaml
dotnet-prompt.progress:
  enabled: true
  checkpoint_frequency: "tool_execution"  # or "step", "manual"
  auto_resume: false
  save_conversation: true
```

## Markdown Content

The content section uses natural language to describe what you want the workflow to accomplish:

### Basic Structure
```markdown
# Workflow Title

## Overview
Brief description of what this workflow does.

## Requirements
What you want the AI to accomplish:
1. First requirement
2. Second requirement  
3. Third requirement

## Output
Describe the expected output format and location.
```

### Parameter Substitution
Use handlebars-style syntax for parameter substitution:

```markdown
# Project Analysis

Analyze the project at `{{project_path}}` with the following settings:
- Include tests: {{include_tests}}
- Output format: {{output_format}}
- Analysis depth: {{analysis_depth}}

Generate a report and save it to `{{output_directory}}/analysis.{{output_format}}`.
```

### Conditional Content
Use conditional blocks for parameter-driven behavior:

```markdown
{{#if include_security_scan}}
## Security Analysis
Perform a comprehensive security analysis including:
- Dependency vulnerability scanning
- Code security pattern analysis
- Configuration security review
{{/if}}

{{#unless skip_documentation}}
## Documentation Generation
Generate comprehensive documentation for the project.
{{/unless}}
```

### Tool Invocation Hints
While not required, you can hint at tool usage:

```markdown
# Code Quality Analysis

Please analyze the code quality using project analysis capabilities:
1. Examine the project structure and dependencies
2. Check for code smells and anti-patterns
3. Evaluate test coverage and quality
4. Generate recommendations for improvements

Use the file system tools to save the analysis results to `./reports/quality-analysis.md`.
```

## Complete Examples

### Simple Workflow
```yaml
---
name: "hello-world"
model: "gpt-4o"
tools: ["file-system"]
---

# Hello World

Create a simple greeting file at `./hello.txt` with the message "Hello from dotnet-prompt!".
```

### Project Analysis Workflow
```yaml
---
name: "comprehensive-analysis"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 4000

input:
  schema:
    project_path:
      type: string
      description: "Path to project file"
      default: "."
    include_security:
      type: boolean
      default: true
---

# Comprehensive Project Analysis

Analyze the .NET project at `{{project_path}}` and provide:

## Core Analysis
1. Project structure and architecture
2. Dependencies and package analysis
3. Code quality metrics
4. Build and test status

{{#if include_security}}
## Security Review
5. Dependency vulnerability assessment
6. Code security analysis
7. Configuration security review
{{/if}}

## Output
Save all analysis results to `./analysis/` directory:
- `project-overview.md` - High-level summary
- `dependencies.json` - Detailed dependency information
- `quality-metrics.json` - Code quality data
- `recommendations.md` - Improvement suggestions

{{#if include_security}}
- `security-report.md` - Security analysis results
{{/if}}
```

### MCP Integration Workflow
```yaml
---
name: "git-workflow-analysis"
model: "gpt-4o"
tools: ["project-analysis"]

dotnet-prompt.mcp:
  - server: "git-mcp"
    version: "2.0.0"
    config:
      repository: "."
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
---

# Git Workflow Analysis

Analyze the Git repository and GitHub project to understand:

1. **Commit History**: Recent changes and patterns
2. **Branch Strategy**: Current branching model
3. **Pull Request Patterns**: Review and merge practices
4. **Issue Management**: Open issues and their categorization
5. **CI/CD Integration**: GitHub Actions and automation

Generate a comprehensive report on the development workflow and suggest improvements.
```

### Sub-workflow Composition
```yaml
---
name: "full-project-lifecycle"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "analysis"
    path: "./workflows/detailed-analysis.prompt.md"
    parameters:
      depth: "comprehensive"
  - name: "testing"
    path: "./workflows/test-generation.prompt.md"
    depends_on: ["analysis"]
  - name: "documentation"
    path: "./workflows/doc-generation.prompt.md"
    depends_on: ["analysis", "testing"]
---

# Complete Project Lifecycle Workflow

Execute a comprehensive project workflow including:

1. **Analysis Phase**: Detailed project analysis
2. **Testing Phase**: Generate and run tests
3. **Documentation Phase**: Create comprehensive documentation

Each phase will use specialized sub-workflows for maximum reusability and maintainability.
```

## Validation and Best Practices

### File Validation
```bash
# Validate workflow syntax
dotnet prompt validate my-workflow.prompt.md

# Check tool availability
dotnet prompt validate my-workflow.prompt.md --check-tools

# Validate with dependency check
dotnet prompt validate my-workflow.prompt.md --check-dependencies
```

### Best Practices

1. **Clear Naming**: Use descriptive, kebab-case names
2. **Focused Scope**: Keep workflows focused on specific tasks
3. **Parameter Defaults**: Provide sensible defaults for all parameters
4. **Error Handling**: Consider edge cases in your instructions
5. **Output Specification**: Clearly describe expected outputs
6. **Reusability**: Design for composition with sub-workflows

### Common Pitfalls

1. **Missing Required Fields**: Always include `name` and `model`
2. **Invalid Tool Names**: Use exact tool names from the available list
3. **Circular Dependencies**: Avoid circular references in sub-workflows
4. **Overly Complex Instructions**: Break complex workflows into sub-workflows
5. **Missing Parameter Descriptions**: Always describe input parameters

## Migration from Other Formats

If you're migrating from other AI workflow formats:

- **Replace custom function calls** with natural language tool requests
- **Convert structured commands** to natural language instructions
- **Move configuration** to YAML frontmatter
- **Add input schemas** for better parameter handling

## Next Steps

- **[Basic Workflows](./basic-workflows.md)**: Learn with simple examples
- **[Advanced Workflows](./advanced-workflows.md)**: Complex scenarios and patterns
- **[Built-in Tools Reference](../reference/built-in-tools.md)**: Available tools and capabilities
- **[MCP Integration Guide](./mcp-integration.md)**: Using external tools
- **[Troubleshooting](./troubleshooting.md)**: Common issues and solutions