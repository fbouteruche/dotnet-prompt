---
name: "multi-plugin-orchestration"
model: "gpt-4o"
tools: ["file-system", "project-analysis", "sub-workflow"]

config:
  temperature: 0.5
  maxOutputTokens: 4000

input:
  default:
    target_directory: "."
    analysis_depth: "comprehensive"
    generate_reports: true
    output_format: "markdown"
  schema:
    target_directory:
      type: string
      description: "Directory to analyze"
      default: "."
    analysis_depth:
      type: string
      enum: ["basic", "detailed", "comprehensive"]
      description: "Depth of analysis to perform"
      default: "detailed"
    generate_reports:
      type: boolean
      description: "Whether to generate analysis reports"
      default: true
    output_format:
      type: string
      enum: ["markdown", "json", "html"]
      description: "Format for output files"
      default: "markdown"
---

# Multi-Plugin Orchestration Test

This workflow demonstrates cross-plugin orchestration using multiple SK plugins working together.

## Phase 1: Project Discovery and Analysis

First, let's analyze the project structure at {{target_directory}} using the project-analysis plugin:

1. Scan the directory structure
2. Identify project files (.csproj, .sln, etc.)
3. Analyze dependencies and references
4. Generate initial project metadata

{{#if (eq analysis_depth "comprehensive")}}
**Comprehensive Analysis Mode**: Will perform deep analysis including:
- Detailed dependency tree analysis
- Code quality metrics
- Security vulnerability scanning  
- Performance analysis patterns
{{else if (eq analysis_depth "detailed")}}
**Detailed Analysis Mode**: Will perform standard analysis including:
- Basic dependency analysis
- Project structure overview
- Key metrics collection
{{else}}
**Basic Analysis Mode**: Will perform quick analysis including:
- Project discovery only
- Basic file count and structure
{{/if}}

## Phase 2: File System Operations

Using the file-system plugin to:

1. Create analysis output directory: `./analysis-{{current_timestamp}}`
2. Read key project files for content analysis
3. {{#if generate_reports}}Generate and save analysis reports{{else}}Prepare analysis data only{{/if}}

## Phase 3: Sub-Workflow Execution

Execute specialized analysis sub-workflows based on project type:

{{#each project_types}}
- {{this}}: Execute {{this}}-specific analysis workflow
{{/each}}

## Phase 4: Report Generation

{{#if generate_reports}}
Generate comprehensive reports in {{output_format}} format:

1. **project-summary.{{output_format}}** - High-level project overview
2. **dependency-analysis.{{output_format}}** - Detailed dependency report  
3. **recommendations.{{output_format}}** - Improvement suggestions
4. **security-analysis.{{output_format}}** - Security assessment results

{{#if (eq output_format "html")}}
Additionally create interactive HTML dashboards with charts and visualizations.
{{/if}}
{{else}}
Skip report generation - provide analysis results directly in response.
{{/if}}

## Expected Plugin Interactions

This workflow tests:
- **FileSystemPlugin**: Directory creation, file reading/writing, path validation
- **ProjectAnalysisPlugin**: .NET project analysis, dependency scanning, metrics collection
- **SubWorkflowPlugin**: Recursive workflow execution, parameter passing, result aggregation

## Success Criteria

The workflow should successfully:
1. ✅ Coordinate multiple plugins without conflicts
2. ✅ Pass data between plugin operations
3. ✅ Handle conditional logic based on input parameters
4. ✅ Generate appropriate outputs based on configuration
5. ✅ Maintain execution state throughout the workflow

Complete this multi-plugin orchestration and provide a summary of the coordination between plugins.