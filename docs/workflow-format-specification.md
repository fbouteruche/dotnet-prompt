# Workflow File Format Specification

## Overview

This document defines the exact format and structure for .prompt.md workflow files, including YAML frontmatter schema, markdown content structure, and validation rules.

## Status
🚧 **DRAFT** - Requires detailed specification

## YAML Frontmatter Schema

### Required Fields
- `model`: The AI model to use for this workflow
- Additional required fields need to be defined

### Optional Fields
- `temperature`: Model temperature setting
- `max_tokens`: Maximum tokens for model response
- Additional optional fields need to be specified

### Example Structure
```yaml
---
model: "gpt-4o"
temperature: 0.7
max_tokens: 2000
tools:
  - name: "dotnet-analyzer"
  - name: "file-operations"
mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
  - server: "git-mcp"
    version: "2.1.0"
parameters:
  - name: "project_path"
    type: "string"
    required: true
    description: "Path to the .NET project"
---
```

## Markdown Content Structure

### Sub-workflow References
How sub-workflows are referenced and invoked within the main workflow content.

### Parameter Substitution
How parameters are referenced and substituted in the workflow content.

### Example Workflow File
Complete example showing all features and syntax.

## Clarifying Questions

### 1. YAML Frontmatter Schema
- What are all the required fields in the frontmatter?
- What are all the optional fields and their default values?
- How should tool dependencies be declared?
- How should MCP server dependencies be specified?
- What parameter types are supported (string, number, boolean, array, object)?
- How should parameter validation rules be defined?

### 2. Sub-workflow Integration
- What is the syntax for referencing sub-workflows?
- How are parameters passed to sub-workflows?
- Can sub-workflows return values to the parent workflow?
- How should relative vs absolute paths to sub-workflows be handled?
- Can sub-workflows be nested (sub-workflow calling another sub-workflow)?

### 3. Parameter System
- How are parameters defined in the frontmatter?
- What is the syntax for parameter substitution in markdown content?
- How should parameter validation work?
- Can parameters have default values?
- How should complex parameter types (objects, arrays) be handled?

### 4. Tool Integration Syntax
- How should built-in tools be referenced in workflows?
- How should MCP tools be referenced and used?
- What is the syntax for tool calls within markdown content?
- How should tool parameters be passed?

### 5. Validation Rules
- What constitutes a valid workflow file?
- How should validation errors be reported?
- Should there be different validation levels (syntax, semantic, runtime)?
- How should missing dependencies be handled during validation?

### 6. File Organization
- Should there be a standard directory structure for workflows?
- How should shared sub-workflows be organized?
- Should there be a workflow discovery mechanism?
- How should workflow dependencies be managed?

### 7. Versioning and Compatibility
- How should workflow format versioning work?
- What happens when a workflow uses features not supported by the current tool version?
- How should backward compatibility be maintained?

## Next Steps

1. Define the complete YAML frontmatter schema with all fields
2. Specify the markdown content syntax and structure
3. Create comprehensive workflow examples
4. Define validation rules and error messages
5. Specify file organization and discovery patterns
