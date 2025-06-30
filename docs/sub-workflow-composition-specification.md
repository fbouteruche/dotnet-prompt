# Sub-workflow Composition Specification

## Overview

This document defines how workflows can compose and invoke sub-workflows, including parameter passing, context inheritance, and dependency resolution.

## Status
🚧 **DRAFT** - Requires detailed specification

## Sub-workflow Reference Syntax

### Workflow Invocation
```markdown
Execute the project analysis sub-workflow:

{{invoke: ./analysis/project-analysis.prompt.md
  parameters:
    project_path: "{{project_path}}"
    include_tests: true
  context: inherit
}}

Based on the analysis results, generate documentation:

{{invoke: ./docs/generate-api-docs.prompt.md
  parameters:
    project_metadata: "{{analysis_result.metadata}}"
    output_format: "markdown"
  context: isolated
}}
```

### Parameter Passing
```yaml
# Main workflow frontmatter
---
parameters:
  - name: "project_path"
    type: "string"
    required: true
  - name: "output_directory"
    type: "string"
    default: "./docs"
---

# Sub-workflow invocation with parameter mapping
{{invoke: ./sub-workflow.prompt.md
  parameters:
    input_path: "{{project_path}}"
    output_path: "{{output_directory}}/analysis"
}}
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

### Result Propagation
```markdown
{{set: analysis_result = invoke: ./analysis.prompt.md}}

The analysis found {{analysis_result.issues_count}} issues:
{{analysis_result.summary}}
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
