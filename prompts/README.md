# Sample Prompts for dotnet-prompt

This directory contains sample workflow files (`.prompt.md`) that demonstrate various features and capabilities of the dotnet-prompt tool.

## Quick Start

Test any of these prompts with the CLI tool:

```bash
# Validate a prompt file
dotnet prompt run hello-world.prompt.md --dry-run

# Execute a prompt file  
dotnet prompt run hello-world.prompt.md

# Run with verbose output
dotnet prompt run project-analysis.prompt.md --verbose
```

## Sample Files

### Basic Examples

- **`hello-world.prompt.md`** - Simple greeting workflow, perfect for testing basic CLI functionality
- **`simple-task.prompt.md`** - Minimal workflow without frontmatter, demonstrates the simplest possible format

### .NET Development Examples

- **`project-analysis.prompt.md`** - Comprehensive .NET project analysis using built-in tools
- **`documentation-generator.prompt.md`** - Generate documentation for a .NET project
- **`code-review.prompt.md`** - Automated code review and suggestions workflow

### Advanced Examples

- **`complex-workflow.prompt.md`** - Demonstrates advanced features like sub-workflows, MCP integration, and conditional logic

## File Format

All sample files follow the [dotprompt format specification](https://google.github.io/dotprompt/reference/frontmatter/) with YAML frontmatter and markdown content:

```yaml
---
name: "workflow-name"
model: "gpt-4o"  
tools: ["tool1", "tool2"]
config:
  temperature: 0.7
  maxOutputTokens: 4000
input:
  schema:
    parameter_name:
      type: string
      description: "Parameter description"
---

# Workflow Content

Natural language instructions for the AI to follow...
```

## Testing

These samples are designed to work with the current CLI foundation. As features are implemented, the samples will be updated to demonstrate new capabilities.

**Current Status:**
- ✅ File validation and parsing
- ✅ Dry-run mode testing
- 🚧 Actual workflow execution (requires workflow engine)
- 🚧 Tool integration (requires built-in tools)
- 🚧 AI provider integration (requires configuration system)