# CLI Interface Specification (SK-Enhanced)

## Overview

This document defines the complete command-line interface for the dotnet-prompt tool, including all commands, options, flags, arguments, and usage patterns optimized for Semantic Kernel integration.

## Status
✅ **COMPLETE** - SK-enhanced CLI patterns defined

## Global Installation (SK-Enabled)

```bash
# Install the tool globally with SK capabilities
dotnet tool install -g dotnet-prompt

# Update to latest version (includes SK updates)
dotnet tool update -g dotnet-prompt

# Uninstall
dotnet tool uninstall -g dotnet-prompt

# Verify SK integration
dotnet prompt --version --sk-info
```

## Main Command Structure (SK-Powered)

```
dotnet prompt <command> [options] [arguments]

Global SK Options:
  --sk-debug                 Enable SK debugging and detailed function call logging
  --sk-telemetry             Enable SK telemetry and performance monitoring
  --sk-cache-dir <path>      Directory for SK conversation state and cache
  --sk-vector-store <type>   Vector store type for SK memory (memory|file|qdrant|azuresearch)
```

## Commands (SK-Enhanced)

### 1. run - Execute Workflow (SK Orchestration)

Execute a workflow file using SK automatic function calling and planning.

```bash
dotnet prompt run <workflow-file> [options]
```

**Arguments:**
- `<workflow-file>`: Path to the .prompt.md workflow file (required)

**SK-Enhanced Options:**
- `--context <path>`: Working directory context (default: current directory)
- `--project <path>`: Target .NET project file (.csproj/.sln)
- `--provider <provider>`: Override AI provider (github|openai|azure|anthropic|local)
- `--model <model>`: Override AI model name
- `--verbose`: Enable verbose output with SK function call details
- `--output <path>`: Output directory for generated files
- `--parameters <json>`: JSON string with workflow parameters (SK validated)
- `--parameter <key=value>`: Individual parameter with SK type validation (repeatable)
- `--timeout <seconds>`: Execution timeout in seconds (applies to SK function calls)

**SK-Specific Options:**
- `--sk-function-choice <behavior>`: SK function choice behavior (auto|required|none)
- `--sk-temperature <value>`: Override temperature for SK execution
- `--sk-max-tokens <value>`: Override max tokens for SK responses
- `--sk-plugins <list>`: Comma-separated list of SK plugins to enable
- `--sk-disable-cache`: Disable SK conversation state and result caching
- `--sk-parallel-functions`: Enable parallel SK function execution when possible
- `--sk-retry-attempts <count>`: Number of retry attempts for failed SK functions
- `--sk-conversation-id <id>`: Resume from specific SK conversation state
- `--no-cache`: Disable caching of tool results
- `--dry-run`: Validate workflow without execution

**Examples:**
```bash
# Basic workflow execution
dotnet prompt run analyze-project.prompt.md

# Execute with specific context and project
dotnet prompt run generate-tests.prompt.md --context ./src --project MyApp.csproj

# Override provider and model
dotnet prompt run code-review.prompt.md --provider openai --model gpt-4

# Pass parameters
dotnet prompt run deploy.prompt.md --parameter environment=staging --parameter region=westus

# Verbose output with timeout
dotnet prompt run complex-workflow.prompt.md --verbose --timeout 600
```

### 2. restore - Install Dependencies

Install and configure MCP server dependencies.

```bash
dotnet prompt restore [options]
```

**Options:**
- `--force`: Force reinstallation of dependencies
- `--verbose`: Show detailed installation progress
- `--config <path>`: Custom MCP configuration file path
- `--global`: Install dependencies globally vs project-local

**Examples:**
```bash
# Restore dependencies for current project
dotnet prompt restore

# Force reinstall with verbose output
dotnet prompt restore --force --verbose

# Use custom configuration
dotnet prompt restore --config ./custom-mcp.json
```

### 3. list - Discover Workflows

List available workflows in the current directory or specified path.

```bash
dotnet prompt list [path] [options]
```

**Arguments:**
- `[path]`: Directory to search for workflows (default: current directory)

**Options:**
- `--recursive`: Search subdirectories recursively
- `--format <format>`: Output format (table|json|yaml) (default: table)
- `--filter <pattern>`: Filter workflows by name pattern
- `--show-metadata`: Include workflow metadata in output

**Examples:**
```bash
# List workflows in current directory
dotnet prompt list

# Recursive search with metadata
dotnet prompt list --recursive --show-metadata

# Filter and format output
dotnet prompt list --filter "*test*" --format json
```

### 4. validate - Validate Workflows

Validate workflow syntax, dependencies, and configuration.

```bash
dotnet prompt validate <workflow-file> [options]
```

**Arguments:**
- `<workflow-file>`: Path to workflow file or directory

**Options:**
- `--check-dependencies`: Validate MCP server dependencies
- `--check-tools`: Validate tool availability
- `--check-providers`: Validate AI provider configuration
- `--format <format>`: Output format (text|json) (default: text)
- `--strict`: Enable strict validation mode

**Examples:**
```bash
# Basic validation
dotnet prompt validate workflow.prompt.md

# Comprehensive validation
dotnet prompt validate workflow.prompt.md --check-dependencies --check-tools --check-providers

# Validate all workflows in directory
dotnet prompt validate ./workflows --strict
```

### 5. resume - Resume Workflow

Resume a previously interrupted workflow from saved progress.

```bash
dotnet prompt resume <workflow-file> [options]
```

**Arguments:**
- `<workflow-file>`: Original workflow file that was interrupted

**Options:**
- `--progress <path>`: Custom progress file path (default: ./progress.md)
- `--from-step <step>`: Resume from specific step number
- `--verbose`: Show detailed resume process
- `--force`: Force resume even with warnings

**Examples:**
```bash
# Resume from default progress file
dotnet prompt resume workflow.prompt.md

# Resume from custom progress file
dotnet prompt resume workflow.prompt.md --progress ./backup/progress.md

# Resume from specific step
dotnet prompt resume workflow.prompt.md --from-step 3
```

### 6. config - Manage Configuration

Manage global and project configuration settings.

```bash
dotnet prompt config <subcommand> [options]
```

**Subcommands:**

#### show - Display Configuration
```bash
dotnet prompt config show [options]
```

**Options:**
- `--global`: Show global configuration only
- `--local`: Show project configuration only
- `--effective`: Show effective merged configuration
- `--format <format>`: Output format (yaml|json|table)

#### set - Set Configuration Value
```bash
dotnet prompt config set <key> <value> [options]
```

**Options:**
- `--global`: Set in global configuration
- `--local`: Set in project configuration

#### unset - Remove Configuration Value
```bash
dotnet prompt config unset <key> [options]
```

**Options:**
- `--global`: Remove from global configuration
- `--local`: Remove from project configuration

**Examples:**
```bash
# Show effective configuration
dotnet prompt config show --effective

# Set default provider globally
dotnet prompt config set default_provider openai --global

# Set project-specific model
dotnet prompt config set default_model gpt-4 --local

# Remove configuration value
dotnet prompt config unset providers.azure.endpoint --global
```

### 7. init - Initialize Project

Initialize dotnet-prompt configuration in a project.

```bash
dotnet prompt init [options]
```

**Options:**
- `--template <template>`: Use configuration template (basic|enterprise|team)
- `--provider <provider>`: Set default AI provider
- `--force`: Overwrite existing configuration

**Examples:**
```bash
# Basic initialization
dotnet prompt init

# Initialize with template
dotnet prompt init --template enterprise --provider azure

# Force overwrite existing config
dotnet prompt init --force
```

## Global Options

Available for all commands:

- `--help, -h`: Show help information
- `--version`: Show version information
- `--quiet, -q`: Suppress non-essential output
- `--no-color`: Disable colored output
- `--config-file <path>`: Use specific configuration file

## Environment Variables

Environment variables that affect tool behavior:

- `DOTNET_PROMPT_PROVIDER`: Default AI provider
- `DOTNET_PROMPT_VERBOSE`: Enable verbose logging (true/false)
- `DOTNET_PROMPT_CONFIG`: Path to configuration file
- `DOTNET_PROMPT_TIMEOUT`: Default timeout in seconds
- `DOTNET_PROMPT_NO_TELEMETRY`: Disable telemetry (true/false)

## Exit Codes

- `0`: Success
- `1`: General error
- `2`: Configuration error
- `3`: Workflow validation error
- `4`: Execution timeout
- `5`: Authentication error
- `6`: Network error
- `7`: Permission error

## Clarifying Questions

### 1. Command Structure
- Are there additional commands needed beyond the core set?
- Should there be command aliases or shortcuts?
- How should command grouping work for related operations?
- Should there be hidden/advanced commands for power users?
- How should command discoverability work?

### 2. Option Design
- What is the complete set of options for each command?
- How should option defaults be determined?
- Which options should be global vs command-specific?
- Should there be option validation and constraint checking?
- How should conflicting options be handled?

### 3. Parameter Handling
- How should complex parameters be passed via CLI?
- Should there be support for parameter files?
- How should parameter validation work at the CLI level?
- Should there be parameter templates or presets?
- How should sensitive parameters be handled securely?

### 4. Output Formatting
- What output formats should be supported?
- How should structured data be displayed in different formats?
- Should there be customizable output templates?
- How should error output be formatted?
- Should there be machine-readable output options?

### 5. Interactive Features
- Should there be interactive modes for any commands?
- How should user prompts and confirmations work?
- Should there be wizard-style command execution?
- How should progress indication work for long operations?
- Should there be real-time status updates?

### 6. Help and Documentation
- How should command help be structured and displayed?
- Should there be examples in help output?
- How should subcommand help work?
- Should there be man page generation?
- How should command completion work?

### 7. Configuration Integration
- How should CLI options interact with configuration files?
- Which settings should be overridable via command line?
- How should environment variables be prioritized?
- Should there be configuration validation commands?
- How should configuration discovery work?

### 8. Error Handling
- How should CLI parsing errors be handled?
- What validation should occur before command execution?
- How should missing dependencies be reported?
- Should there be error recovery suggestions?
- How should partial command completion work?

### 9. Shell Integration
- Should there be shell completion scripts?
- How should the tool integrate with different shells?
- Should there be shell-specific features?
- How should command history work?
- Should there be shell function generation?

### 10. Advanced Features
- Should there be batch execution capabilities?
- How should command chaining or piping work?
- Should there be daemon/service mode?
- How should remote execution work?
- Should there be plugin architecture for custom commands?

## Next Steps

1. Finalize the complete command set and options
2. Implement CLI parsing and validation
3. Create help system and documentation
4. Build shell completion support
5. Design output formatting system
6. Implement configuration integration
