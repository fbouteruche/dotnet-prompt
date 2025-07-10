# Getting Started with dotnet-prompt

Welcome to dotnet-prompt! This guide will help you install the tool and create your first AI-powered workflow in just a few minutes.

## What is dotnet-prompt?

dotnet-prompt is a CLI tool that lets you create and execute AI-powered workflows using simple markdown files. These workflows can analyze your .NET projects, generate code, run tests, create documentation, and automate many development tasks.

## Quick Installation

```bash
# Install as a global .NET tool (once published to NuGet)
dotnet tool install -g dotnet-prompt

# Verify installation
dotnet prompt --version
```

> **Note**: Currently in development. See our detailed [Installation Guide](./installation.md) for platform-specific instructions, or see [Development Installation](#development-installation) below for building from source.

## Your First Workflow

Let's start with a simple "Hello World" workflow that creates a personalized greeting:

### Step 1: Create a Workflow File

Create a file named `my-first-workflow.prompt.md`:

```yaml
---
name: "my-first-workflow"
model: "gpt-4o"
tools: ["file_write"]

config:
  temperature: 0.7
  maxOutputTokens: 500

metadata:
  description: "Simple hello world greeting workflow"
  author: "dotnet-prompt team"
  version: "1.0.0"
  tags: ["hello", "example", "basic"]
---

# Hello World Workflow

Write me a hello world poem and use the `file_write` tool to save it to `./hello-world.txt`.
The poem should be creative and engaging, showcasing the beauty of a simple greeting.
```

### Step 2: Run the Workflow

```bash
# Execute the workflow
dotnet prompt run my-first-workflow.prompt.md

# Run with verbose output to see what's happening
dotnet prompt run my-first-workflow.prompt.md --verbose

# Validate the workflow without running it
dotnet prompt run my-first-workflow.prompt.md --dry-run
```

### Step 3: Check the Results

After running successfully, you should see:
- Console output with the generated poem
- A new file `hello-world.txt` containing your personalized poem
- Progress file `my-first-workflow.progress.md` for resume capability

## Understanding the Workflow Format

Every dotnet-prompt workflow has two parts:

### 1. YAML Frontmatter (Configuration)
```yaml
---
name: "workflow-identifier"        # Unique name for this workflow
model: "gpt-4o"                   # AI model to use
tools: ["tool1", "tool2"]         # Available tools for this workflow

config:                           # Model-specific settings
  temperature: 0.7                # Creativity level (0.0-1.0)
  maxOutputTokens: 2000           # Response length limit
---
```

### 2. Markdown Content (Instructions)
```markdown
# Workflow Title

Clear instructions for what you want the AI to accomplish.

Use natural language to describe your goals and requirements.
The AI will use the available tools to complete the tasks.
```
## Next Steps

Now that you've created your first workflow, explore these guides:

- **[Installation Guide](./installation.md)**: Complete installation instructions for all platforms
- **[Dotprompt Format](./dotprompt-format.md)**: Complete format specification
- **[Basic Workflows](./basic-workflows.md)**: More simple examples to learn from
- **[Built-in Tools](../reference/built-in-tools.md)**: Available tools and their capabilities
- **[MCP Integration](./mcp-integration.md)**: Extend workflows with external tools
- **[Configuration](../reference/configuration-options.md)**: Customizing behavior
- **[Advanced Workflows](./advanced-workflows.md)**: Complex multi-step scenarios

## Development Installation

If you're building from source:

```bash
# Clone the repository
git clone https://github.com/fbouteruche/dotnet-prompt.git
cd dotnet-prompt

# Build and install locally
dotnet pack src/DotnetPrompt.Cli --configuration Release
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli

# Verify installation
dotnet prompt --version
```

## Getting Help

- **CLI Help**: `dotnet prompt --help`
- **Command Help**: `dotnet prompt run --help`
- **Troubleshooting**: [Common Issues](./troubleshooting.md)
- **Examples**: Browse the [examples directory](../examples/) for organized workflows and configurations

Ready to automate your development workflow? Let's go! 🚀