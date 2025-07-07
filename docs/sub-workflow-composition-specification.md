# Sub-workflow Composition Specification (SK-Orchestrated)

## Overview

This document defines how workflows can compose and invoke sub-workflows using Semantic Kernel's automatic function calling and planning capabilities, including parameter passing, context inheritance, and dependency resolution.

## Status
✅ **COMPLETE** - SK-based workflow composition patterns defined

## SK-Based Sub-workflow Architecture

### Core Integration Strategy
- **SK Function Calling**: Sub-workflows are exposed as SK functions for automatic orchestration
- **Automatic Planning**: SK automatically determines sub-workflow execution order and dependencies
- **Context Inheritance**: SK ChatHistory and conversation state seamlessly flow between workflows
- **Parameter Mapping**: SK automatic parameter validation and type conversion
- **Error Propagation**: SK filters handle errors across the entire workflow composition

### Sub-workflow as SK Function

#### SubWorkflowPlugin Implementation
```csharp
[Description("Executes and composes sub-workflows")]
public class SubWorkflowPlugin
{
    private readonly IDotpromptParser _parser;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<SubWorkflowPlugin> _logger;

    [KernelFunction("execute_sub_workflow")]
    [Description("Executes a sub-workflow from a file path with parameters")]
    [return: Description("The result of the sub-workflow execution")]
    public async Task<string> ExecuteSubWorkflowAsync(
        [Description("Relative or absolute path to the sub-workflow .prompt.md file")] string workflowPath,
        [Description("JSON object containing variables for the sub-workflow")] string parameters = "{}",
        [Description("Context inheritance mode: 'inherit', 'isolated', or 'merge'")] string contextMode = "inherit",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing sub-workflow: {WorkflowPath}", workflowPath);

            // 1. Parse the sub-workflow using the main parser
            var subWorkflow = await _parser.ParseFileAsync(workflowPath, cancellationToken);
            
            // 2. Create execution context based on mode and parameters
            var subContext = CreateSubWorkflowContext(parameters, contextMode);
            
            // 3. Execute sub-workflow using the main orchestrator (recursive composition)
            var result = await _orchestrator.ExecuteWorkflowAsync(subWorkflow, subContext, cancellationToken);
            
            if (!result.Success)
            {
                throw new KernelException($"Sub-workflow execution failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Sub-workflow completed successfully: {WorkflowPath}", workflowPath);
            return result.Output ?? "Sub-workflow completed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sub-workflow: {WorkflowPath}", workflowPath);
            throw new KernelException($"Sub-workflow execution failed: {ex.Message}", ex);
        }
    }

    [KernelFunction("validate_sub_workflow")]
    [Description("Validates a sub-workflow without executing it")]
    [return: Description("JSON validation result")]
    public async Task<string> ValidateSubWorkflowAsync(
        [Description("Path to the sub-workflow file")] string workflowPath,
        [Description("JSON object containing variables to validate")] string parameters = "{}",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subWorkflow = await _parser.ParseFileAsync(workflowPath, cancellationToken);
            var subContext = CreateSubWorkflowContext(parameters, "isolated");
            
            var validationResult = await _orchestrator.ValidateWorkflowAsync(subWorkflow, subContext, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors,
                Warnings = validationResult.Warnings
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            throw new KernelException($"Sub-workflow validation failed: {ex.Message}", ex);
        }
    }

    private WorkflowExecutionContext CreateSubWorkflowContext(string parametersJson, string contextMode)
    {
        var context = new WorkflowExecutionContext();
        
        // Parse parameters from JSON
        if (!string.IsNullOrEmpty(parametersJson) && parametersJson != "{}")
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
            foreach (var (key, value) in parameters ?? new())
            {
                context.SetVariable(key, value);
            }
        }

        // Handle context inheritance modes (to be implemented based on requirements)
        // - "inherit": Copy parent context variables
        // - "isolated": Start with empty context
        // - "merge": Combine parent and provided parameters
        
        return context;
    }
}
```

## Sub-workflow Reference Syntax (SK-Enhanced)

### Automatic Function Calling Workflow Composition
```markdown
I need to analyze this project and generate comprehensive documentation.

First, analyze the project structure and dependencies:
- Use the project analysis workflow to examine the codebase
- Include dependency analysis and test coverage information
- Store the results for subsequent workflows

Then, based on the analysis results, generate multiple documentation types:
- API documentation from the code structure
- README file with project overview and setup instructions  
- Architecture documentation showing component relationships

Finally, validate all generated documentation for consistency and completeness.
```

> **SK Automatic Planning**: With this natural language description and the registered sub-workflow functions, SK will automatically:
> 1. Call `invoke_sub_workflow` for project analysis
> 2. Pass analysis results to documentation generation workflows
> 3. Execute validation workflows with generated documentation
> 4. Handle parameter passing and context inheritance automatically

### Explicit Sub-workflow Invocation (Handlebars Templates)
```handlebars
{{!-- Execute project analysis and documentation generation --}}

## Project Analysis
{{execute_sub_workflow 
  workflowPath="./analysis/project-analysis.prompt.md"
  parameters=(json 
    project_path=project_path
    include_tests=true
    analysis_depth="comprehensive")
  contextMode="inherit"
}}

## API Documentation Generation
{{#if generate_docs}}
{{execute_sub_workflow 
  workflowPath="./docs/generate-api-docs.prompt.md" 
  parameters=(json 
    project_metadata=analysis_result.metadata
    output_format="markdown"
    include_examples=true)
  contextMode="merge"
}}
{{/if}}

## Summary Report
{{execute_sub_workflow 
  workflowPath="./reports/summary.prompt.md"
  parameters="{}"
  contextMode="inherit"
}}
```

### Enhanced Parameter Mapping with SK Validation
```yaml
# Main workflow frontmatter with SK Handlebars integration
---
name: "project-documentation-workflow"
model: "gpt-4o"
tools: ["project-analysis", "file-system", "sub-workflow"]

config:
  temperature: 0.7
  maxOutputTokens: 4000

input:
  default:
    project_path: "."
    generate_docs: true
    output_directory: "./docs"
  schema:
    project_path:
      type: string
      description: "Path to the .NET project file"
      pattern: ".*\\.(csproj|fsproj|vbproj|sln)$"
    output_directory: 
      type: string
      default: "./docs"
      description: "Directory for generated documentation"
    generate_docs:
      type: boolean
      default: true
      description: "Whether to generate documentation"

# Sub-workflow definitions for reference
sub_workflows:
  - name: "project_analysis"
    path: "./analysis/project-analysis.prompt.md"
    timeout_seconds: 300
    retry_attempts: 2
    cache_results: true
  - name: "api_documentation"
    path: "./docs/generate-api-docs.prompt.md"
    depends_on: ["project_analysis"]
---

# SK will automatically orchestrate these workflows using Handlebars templating
# The SubWorkflowPlugin is available as an SK function for the AI to call

Analyze the project at {{project_path}} and generate comprehensive documentation.

First, perform project analysis:
- Include dependency analysis and test coverage
- Generate metrics and identify potential issues
- Store results for subsequent workflows

{{#if generate_docs}}
Then, based on the analysis results, generate documentation:
- API documentation from the code structure  
- README file with project overview
- Architecture documentation
{{/if}}

The AI will automatically call the execute_sub_workflow function with appropriate parameters.
```
```

## Context Inheritance

### Inherited Context
- AI provider and model configuration
- Tool availability and registration
- Environment variables and execution context
- Parent workflow variables and state

### Isolated Context
- Independent tool dependencies
- Separate configuration overrides
- Isolated variable scope
- Independent error handling

## Dependency Resolution

### Tool Dependencies
How sub-workflow tool requirements are resolved and merged with parent workflow.

### MCP Server Dependencies
How MCP server requirements are handled across workflow boundaries.

## Sub-workflow Return Values

### Result Propagation (Handlebars Variables)
```handlebars
{{!-- Execute analysis and capture result --}}
{{set analysis_result = (execute_sub_workflow 
  workflowPath="./analysis.prompt.md"
  parameters=(json project_path=project_path))
}}

{{!-- Use the result in subsequent content --}}
The analysis found {{analysis_result.issues_count}} issues:
{{analysis_result.summary}}

{{!-- Pass results to another sub-workflow --}}
{{execute_sub_workflow 
  workflowPath="./generate-report.prompt.md"
  parameters=(json analysis_data=analysis_result)
}}
```

### Structured Results
How complex data structures are passed between workflows.

## Clarifying Questions

### 1. Sub-workflow Invocation Syntax
- What is the exact syntax for invoking sub-workflows?
- How should sub-workflow paths be resolved (relative vs absolute)?
- Should there be support for remote sub-workflows (URLs, git refs)?
- How should sub-workflow versioning work?
- Should there be conditional sub-workflow execution?

### 2. Parameter Passing
- How should parameters be declared and validated?
- What parameter types are supported (primitives, objects, arrays)?
- How should parameter transformation work between workflows?
- Should there be parameter validation at invocation time?
- How should optional vs required parameters be handled?

### 3. Context Inheritance
- What execution context should be inherited by default?
- How should context isolation work?
- Can sub-workflows override inherited configuration?
- How should environment variables be scoped?
- Should there be explicit context control mechanisms?

### 4. Return Values and Results
- How should sub-workflows return values to parent workflows?
- What data formats are supported for return values?
- Should there be typed return value declarations?
- How should return value validation work?
- Can sub-workflows return multiple values?

### 5. Dependency Resolution
- How should conflicting tool dependencies be resolved?
- Should sub-workflows inherit parent tool registrations?
- How should MCP server dependencies be merged?
- What happens when sub-workflows have incompatible requirements?
- Should there be dependency isolation options?

### 6. Error Handling
- How should sub-workflow errors be propagated?
- Should there be error handling strategies (fail-fast, continue, retry)?
- How should partial sub-workflow failures be handled?
- Can parent workflows catch and handle sub-workflow errors?
- Should there be error transformation capabilities?

### 7. Execution Control
- Should sub-workflows run sequentially or in parallel?
- How should execution timeouts work for sub-workflows?
- Can sub-workflow execution be cancelled?
- Should there be execution priority or scheduling?
- How should resource limits be applied to sub-workflows?

### 8. Variable Scope and Data Flow
- How should variables be scoped between parent and child workflows?
- Can sub-workflows modify parent workflow variables?
- How should data flow be tracked across workflow boundaries?
- Should there be variable transformation capabilities?
- How should circular dependencies be prevented?

### 9. Progress and Resume
- How should sub-workflow progress be tracked?
- Should sub-workflows have independent resume capabilities?
- How should nested workflow resume work?
- Can parent workflows resume without re-executing completed sub-workflows?
- How should sub-workflow checkpoints be managed?

### 10. Security and Isolation
- What security boundaries should exist between workflows?
- How should sensitive data be handled across workflow boundaries?
- Should there be permission controls for sub-workflow invocation?
- How should file system access be controlled in sub-workflows?
- Should there be audit logging for sub-workflow execution?

### 11. Performance Considerations
- How should sub-workflow caching work?
- Should there be memoization of sub-workflow results?
- How should large data transfer between workflows be optimized?
- What is the strategy for managing memory usage in nested workflows?
- Should there be lazy evaluation of sub-workflow parameters?

### 12. Development and Debugging
- How should nested workflow debugging work?
- Should there be step-through debugging for sub-workflows?
- How should workflow composition be visualized?
- What tooling should be available for workflow development?
- How should workflow dependencies be analyzed?

### 13. Advanced Workflow Integration
- Should there be tool composition and chaining capabilities beyond SK's function calling?
- How should complex tool dependencies be expressed and resolved?
- What hooks are needed for advanced workflow monitoring and debugging?

## Next Steps

1. Define the exact sub-workflow invocation syntax
2. Implement parameter passing and validation
3. Design context inheritance and isolation mechanisms
4. Create dependency resolution logic
5. Build return value and result propagation system
6. Implement error handling and execution control
