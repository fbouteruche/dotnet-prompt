# Built-in Tools Reference

Complete reference for all built-in tools available in dotnet-prompt workflows. These tools provide core .NET development capabilities without requiring external MCP servers.

## Overview

Built-in tools are automatically available in all workflows. They provide:
- **Project Analysis**: Comprehensive .NET project and solution analysis
- **File System**: Secure file and directory operations
- **Sub-workflow**: Execute and compose other workflow files

## Using Built-in Tools

### Tool Declaration

Declare tools in your workflow frontmatter:

```yaml
---
name: "example-workflow"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---
```

### Tool Invocation

Tools are invoked through natural language in your workflow content:

```markdown
# Example Workflow

Please analyze the current project structure and generate a report.

Finally, save the results to a new file called `results.md`.
```

The AI automatically maps these requests to appropriate tool functions based on the available tools.

## Project Analysis Tool

**Tool Name**: `project-analysis`

Provides comprehensive analysis of .NET projects, solutions, and codebases.

### Capabilities

#### Project Structure Analysis
- Project file parsing (.csproj, .sln, .fsproj, .vbproj)
- Target framework identification
- Project type detection (Console, Web, Library, etc.)
- Project dependencies and references
- Directory structure mapping

#### Dependency Analysis
- NuGet package inventory
- Package version analysis
- Dependency tree construction
- Vulnerability scanning (when enabled)
- License compliance checking
- Outdated package detection

#### Code Analysis
- Source file discovery and categorization
- Code metrics calculation (lines of code, complexity)
- Namespace and class structure analysis
- Interface and API surface analysis
- Documentation coverage assessment

#### Build Configuration Analysis
- Build configuration review (Debug/Release)
- Compiler settings and flags
- Output configuration analysis
- Framework-specific settings

### Usage Examples

#### Basic Project Analysis
```yaml
---
name: "project-overview"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Project Overview

Analyze the current .NET project and create a comprehensive overview including:
- Project type and target framework
- Main dependencies and their purposes
- Key source files and their organization
- Build configuration summary

Save the overview to `project-overview.md`.
```

#### Dependency Audit
```yaml
---
name: "dependency-audit"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Dependency Audit

Perform a comprehensive dependency audit:
1. List all NuGet packages with versions
2. Identify packages with available updates
3. Check for security vulnerabilities
4. Analyze license compatibility
5. Suggest dependency optimizations

Generate a detailed audit report with actionable recommendations.
```

#### Code Quality Assessment
```yaml
---
name: "code-quality"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Code Quality Assessment

Analyze the codebase for quality metrics:
- Code organization and structure
- Adherence to C# coding conventions
- API design quality
- Documentation coverage
- Potential refactoring opportunities

Provide specific recommendations for improvements.
```

### Configuration Options

Configure the project analysis tool behavior:

```json
{
  "tool_configuration": {
    "project_analysis": {
      "excluded_directories": ["bin", "obj", ".git", "node_modules"],
      "max_file_size_bytes": 1048576,
      "include_test_projects": true,
      "analyze_dependencies": true,
      "check_vulnerabilities": false,
      "code_metrics_enabled": true,
      "documentation_analysis": true
    }
  }
}
```

**Configuration Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `excluded_directories` | array | ["bin", "obj", ".git"] | Directories to skip during analysis |
| `max_file_size_bytes` | integer | 1048576 | Maximum file size to analyze (1MB) |
| `include_test_projects` | boolean | true | Include test projects in analysis |
| `analyze_dependencies` | boolean | true | Perform dependency analysis |
| `check_vulnerabilities` | boolean | false | Check for known vulnerabilities |
| `code_metrics_enabled` | boolean | true | Calculate code metrics |
| `documentation_analysis` | boolean | true | Analyze documentation coverage |

### Output Formats

The project analysis tool provides structured data that can be used by AI workflows:

#### Project Information
```json
{
  "project_type": "WebApplication",
  "target_frameworks": ["net8.0"],
  "language": "C#",
  "project_files": ["MyApp.csproj"],
  "solution_files": ["MyApp.sln"]
}
```

#### Dependency Information
```json
{
  "packages": [
    {
      "name": "Microsoft.AspNetCore.App",
      "version": "8.0.0",
      "type": "FrameworkReference"
    }
  ],
  "total_packages": 15,
  "outdated_packages": 2
}
```

## File System Tool

**Tool Name**: `file-system`

Provides secure file and directory operations with safety controls and audit logging.

### Capabilities

#### File Operations
- Read file contents with encoding detection
- Write files with backup and safety mechanisms
- Copy and move files with conflict resolution
- Delete files with confirmation controls
- File metadata extraction (size, dates, permissions)

#### Directory Operations
- List directory contents with filtering
- Create directory structures
- Directory tree navigation and search
- Bulk file operations within directories

#### Security Features
- Path validation and sandboxing
- Access control based on working directory
- Audit logging for destructive operations
- Backup creation for file modifications

#### Search and Filter
- File pattern matching
- Content-based search
- Metadata filtering
- Recursive directory traversal

### Usage Examples

#### File Content Analysis
```yaml
---
name: "config-analysis"
model: "gpt-4o"
tools: ["file-system"]
---

# Configuration Analysis

Analyze project configuration files:
1. Read appsettings.json and environment-specific variants
2. Examine project files for configuration
3. Check for sensitive data in configuration
4. Generate configuration documentation

Create a configuration analysis report with security recommendations.
```

#### Code Generation
```yaml
---
name: "model-generator"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Model Generator

Generate data models based on project analysis:
1. Analyze existing entity classes
2. Generate new model classes following patterns
3. Create corresponding DTO classes
4. Generate repository interfaces
5. Update project files if needed

Save all generated files in appropriate directories with proper namespaces.
```

#### Documentation Generation
```yaml
---
name: "readme-generator"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# README Generator

Generate comprehensive project documentation:
1. Analyze project structure and purpose
2. Create README.md with project overview
3. Generate API documentation
4. Create setup and installation instructions
5. Add usage examples and best practices

Ensure all documentation is up-to-date and accurate.
```

### Configuration Options

Configure file system tool behavior:

```json
{
  "tool_configuration": {
    "file_system": {
      "working_directory_only": true,
      "max_file_size_bytes": 10485760,
      "backup_enabled": true,
      "audit_logging": true,
      "allowed_extensions": [".cs", ".json", ".md", ".txt", ".xml"],
      "denied_paths": [".git", "bin", "obj", "node_modules"],
      "encoding": "utf-8"
    }
  }
}
```

**Configuration Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `working_directory_only` | boolean | true | Restrict operations to working directory |
| `max_file_size_bytes` | integer | 10485760 | Maximum file size (10MB) |
| `backup_enabled` | boolean | true | Create backups before modifications |
| `audit_logging` | boolean | true | Log file operations |
| `allowed_extensions` | array | [".cs", ".json", ".md"] | Allowed file extensions |
| `denied_paths` | array | [".git", "bin", "obj"] | Denied path patterns |
| `encoding` | string | "utf-8" | Default text encoding |

### Security Model

#### Path Validation
- All paths are validated and normalized
- Access restricted to working directory context
- Symbolic links are resolved safely
- Path traversal attacks are prevented

#### File Safety
- Backups created before destructive operations
- File locks respected during operations
- Atomic write operations when possible
- Rollback capability for failed operations

#### Access Control
```yaml
# Working directory context (CLI --context or current directory)
./src/           # ✅ Allowed
./docs/          # ✅ Allowed
./tests/         # ✅ Allowed
../other-project # ❌ Blocked (outside working directory)
/etc/passwd      # ❌ Blocked (system file)
```

### File Operations

#### Reading Files
- Automatic encoding detection
- Large file streaming support
- Binary file detection and handling
- Content parsing and analysis

#### Writing Files
- Safe write operations with backups
- Directory creation as needed
- Conflict resolution strategies
- Content validation before write

#### File Management
- Copy operations with metadata preservation
- Move operations with conflict handling
- Delete operations with confirmation
- Batch operations for efficiency

## Sub-workflow Tool

**Tool Name**: `sub-workflow`

Enables composition and execution of other workflow files, supporting parameter passing and dependency management.

### Capabilities

#### Workflow Composition
- Execute other `.prompt.md` files as sub-workflows
- Pass parameters between workflows
- Handle workflow dependencies and execution order
- Manage workflow context and state

#### Parameter Management
- Parameter passing from parent to sub-workflows
- Parameter validation and type checking
- Default parameter handling
- Parameter inheritance and overrides

#### Execution Control
- Sequential execution with dependencies
- Parallel execution for independent workflows
- Conditional execution based on parameters
- Error handling and recovery

#### Context Management
- Preserve execution context across workflows
- Share state between related workflows
- Maintain conversation history
- Handle resource cleanup

### Usage Examples

#### Multi-Phase Project Setup
```yaml
---
name: "project-setup"
model: "gpt-4o"
tools: ["project-analysis", "file-system", "sub-workflow"]
---

# Complete Project Setup

Execute a multi-phase project setup:

## Phase 1: Analysis
Execute the project analysis workflow to understand the current state.

## Phase 2: Code Quality
Based on the analysis, run code quality improvements.

## Phase 3: Documentation
Generate comprehensive documentation for the improved project.

## Phase 4: Testing
Set up and run comprehensive testing suite.

Each phase should build on the results of the previous phases.
```

#### Conditional Workflow Execution
```yaml
---
name: "adaptive-ci"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "sub-workflow"]

input:
  schema:
    deployment_target:
      type: string
      enum: ["development", "staging", "production"]
    run_security_scan:
      type: boolean
      default: false
---

# Adaptive CI Pipeline

Execute CI pipeline adapted to deployment target: {{deployment_target}}

## Standard Pipeline
1. Build and test the application
2. Generate build artifacts

{{#eq deployment_target "staging"}}
## Staging-Specific Steps
3. Execute integration test suite
4. Deploy to staging environment
5. Run smoke tests
{{/eq}}

{{#eq deployment_target "production"}}
## Production-Specific Steps
3. Execute full test suite including performance tests
4. Run security analysis
5. Create deployment package
6. Execute blue-green deployment validation
{{/eq}}

{{#if run_security_scan}}
## Security Analysis
Execute comprehensive security scanning workflow
{{/if}}
```

### Configuration Options

Configure sub-workflow execution:

```json
{
  "tool_configuration": {
    "sub_workflow": {
      "max_depth": 5,
      "timeout_seconds": 1800,
      "inherit_context": true,
      "preserve_conversation": true,
      "parallel_execution": true,
      "error_handling": "continue_on_error"
    }
  }
}
```

**Configuration Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `max_depth` | integer | 5 | Maximum sub-workflow nesting depth |
| `timeout_seconds` | integer | 1800 | Sub-workflow execution timeout |
| `inherit_context` | boolean | true | Inherit parent workflow context |
| `preserve_conversation` | boolean | true | Maintain conversation history |
| `parallel_execution` | boolean | true | Enable parallel execution |
| `error_handling` | string | "continue_on_error" | Error handling strategy |

### Sub-workflow Patterns

#### Sequential Execution
```yaml
dotnet-prompt.sub-workflows:
  - name: "phase-1"
    path: "./workflows/analysis.prompt.md"
  - name: "phase-2"
    path: "./workflows/build.prompt.md"
    depends_on: ["phase-1"]
  - name: "phase-3"
    path: "./workflows/test.prompt.md"
    depends_on: ["phase-2"]
```

#### Parallel Execution
```yaml
dotnet-prompt.sub-workflows:
  - name: "code-analysis"
    path: "./workflows/code-quality.prompt.md"
    parallel_group: "analysis"
  - name: "security-scan"
    path: "./workflows/security.prompt.md"
    parallel_group: "analysis"
  - name: "performance-test"
    path: "./workflows/performance.prompt.md"
    parallel_group: "analysis"
```

#### Conditional Execution
```yaml
dotnet-prompt.sub-workflows:
  - name: "web-specific"
    path: "./workflows/web-analysis.prompt.md"
    condition: "{{#eq project_type 'web'}}"
  - name: "library-specific"
    path: "./workflows/library-analysis.prompt.md"
    condition: "{{#eq project_type 'library'}}"
```

## Tool Integration Patterns

### Combining Multiple Tools

#### Comprehensive Project Analysis
```yaml
---
name: "comprehensive-analysis"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Comprehensive Project Analysis

Perform complete project analysis using all available tools:

## Project Structure Analysis
Use project analysis to understand the codebase structure, dependencies, and architecture.

## File System Review
Examine configuration files, documentation, and project organization.

## Consolidated Report
Generate a comprehensive report combining insights from all analysis tools with actionable recommendations.
```

#### Automated Improvement Workflow
```yaml
---
name: "auto-improvement"
model: "gpt-4o"
tools: ["project-analysis", "file-system", "sub-workflow"]
---

# Automated Project Improvement

Execute automated improvements based on analysis:

## Analysis Phase
1. Analyze project structure and identify improvement opportunities
2. Review code quality and documentation

## Improvement Phase
3. Generate improved code files where beneficial
4. Update configuration files for better practices
5. Create or update documentation
6. Add missing unit tests

## Validation Phase
7. Generate improvement summary report

Ensure all improvements are documented for review.
```

### Tool-Specific Best Practices

#### Project Analysis Best Practices
- Include test projects in analysis for complete coverage
- Enable vulnerability checking for security-conscious projects
- Configure appropriate file size limits for large codebases
- Use excluded directories to skip generated code

#### File System Best Practices
- Enable backup creation for safety
- Use audit logging for compliance tracking
- Configure appropriate file size limits
- Restrict operations to working directory for security

#### Sub-workflow Best Practices
- Keep individual workflows focused on specific tasks
- Use clear parameter schemas for better composition
- Implement proper error handling in sub-workflows
- Document workflow dependencies clearly

## Troubleshooting

### Common Issues

#### Tool Not Available
```
Error: Tool 'project-analysis' not available
```

**Solutions:**
1. Check tool name spelling in frontmatter
2. Verify tool is declared in `tools` array
3. Check tool configuration is valid

#### File Access Denied
```
Error: Access denied to file '/restricted/path'
```

**Solutions:**
1. Check working directory context
2. Verify file is within allowed paths
3. Check file permissions
4. Review file system tool configuration

#### Sub-workflow Not Found
```
Error: Sub-workflow file not found: './workflows/missing.prompt.md'
```

**Solutions:**
1. Verify file path is correct and relative to main workflow
2. Check file exists and is accessible
3. Verify file has correct `.prompt.md` extension
4. Check working directory context

### Debugging Tools

#### Verbose Output
Enable verbose output to see detailed tool execution:

```bash
dotnet prompt run workflow.prompt.md --verbose
```

#### Dry Run
Validate tool availability without execution:

```bash
dotnet prompt run workflow.prompt.md --dry-run
```

#### Tool Configuration Check
Verify tool configuration:

```bash
dotnet prompt config show
```

## Next Steps

- **[Installation Guide](../user-guide/installation.md)**: Setup and MCP server installation
- **[MCP Integration](../user-guide/mcp-integration.md)**: Using external tools and services
- **[Configuration Options](./configuration-options.md)**: Customize tool behavior
- **[CLI Commands](./cli-commands.md)**: Tool-related command options
- **[Advanced Workflows](../user-guide/advanced-workflows.md)**: Complex tool usage patterns