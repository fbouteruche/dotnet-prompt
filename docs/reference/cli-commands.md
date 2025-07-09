# CLI Commands Reference

Complete reference for all dotnet-prompt CLI commands, options, and usage patterns.

## Installation and Setup

### Install dotnet-prompt

```bash
# Install as global .NET tool (when published)
dotnet tool install -g dotnet-prompt

# Update to latest version
dotnet tool update -g dotnet-prompt

# Uninstall
dotnet tool uninstall -g dotnet-prompt
```

### Development Installation

```bash
# Clone and build from source
git clone https://github.com/fbouteruche/dotnet-prompt.git
cd dotnet-prompt
dotnet pack src/DotnetPrompt.Cli --configuration Release
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli
```

## Global Commands

### `dotnet prompt --help`

Display help information for all commands.

```bash
dotnet prompt --help
dotnet prompt -h
```

### `dotnet prompt --version`

Display the current version of dotnet-prompt.

```bash
dotnet prompt --version
dotnet prompt -v
```

## Core Commands

### `run` - Execute Workflows

Execute a workflow file with various options and configurations.

#### Basic Syntax
```bash
dotnet prompt run <workflow-file> [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--context` | `-c` | string | Working directory context | Current directory |
| `--dry-run` | `-d` | flag | Validate without execution | false |
| `--verbose` | `-v` | flag | Enable verbose output | false |
| `--timeout` | `-t` | int | Execution timeout (seconds) | 300 |
| `--parameter` | `-p` | string | Parameter override (key=value) | None |
| `--provider` | | string | Override AI provider | From config |
| `--model` | `-m` | string | Override AI model | From workflow |
| `--config` | | string | Configuration file path | Auto-discovered |
| `--no-progress` | | flag | Disable progress tracking | false |
| `--resume` | `-r` | flag | Resume from progress file | false |

#### Examples

**Basic execution:**
```bash
dotnet prompt run analyze-project.prompt.md
```

**With custom context:**
```bash
dotnet prompt run analyze-project.prompt.md --context ./src
```

**Dry run validation:**
```bash
dotnet prompt run analyze-project.prompt.md --dry-run
```

**Verbose output with timeout:**
```bash
dotnet prompt run analyze-project.prompt.md --verbose --timeout 600
```

**Parameter overrides:**
```bash
dotnet prompt run analyze-project.prompt.md --parameter include_tests=false --parameter output_format=json
```

**Provider and model overrides:**
```bash
dotnet prompt run analyze-project.prompt.md --provider openai --model gpt-4
```

**Resume interrupted workflow:**
```bash
dotnet prompt run analyze-project.prompt.md --resume
```

#### Exit Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Workflow completed successfully |
| 1 | GeneralError | General execution error |
| 2 | FileNotFound | Workflow file not found |
| 3 | ValidationError | Workflow validation failed |
| 4 | ConfigurationError | Configuration error |
| 5 | TimeoutError | Execution timeout |
| 6 | UserCancelled | User cancelled execution |

### `validate` - Validate Workflows

Validate workflow files for syntax, dependencies, and configuration errors.

#### Basic Syntax
```bash
dotnet prompt validate <workflow-file> [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--check-tools` | | flag | Validate tool availability | false |
| `--check-dependencies` | | flag | Check MCP dependencies | false |
| `--strict` | `-s` | flag | Enable strict validation | false |
| `--output` | `-o` | string | Output format (text/json) | text |

#### Examples

**Basic validation:**
```bash
dotnet prompt validate workflow.prompt.md
```

**Full validation with dependencies:**
```bash
dotnet prompt validate workflow.prompt.md --check-tools --check-dependencies
```

**Strict validation with JSON output:**
```bash
dotnet prompt validate workflow.prompt.md --strict --output json
```

#### Validation Checks

1. **Syntax Validation**
   - YAML frontmatter syntax
   - Required fields presence
   - Field type validation
   - Enum value validation

2. **Tool Validation** (with `--check-tools`)
   - Built-in tool availability
   - Tool name correctness
   - Tool compatibility

3. **Dependency Validation** (with `--check-dependencies`)
   - MCP server availability
   - Sub-workflow file existence
   - Parameter schema validation

4. **Strict Validation** (with `--strict`)
   - Unused parameter detection
   - Unreachable workflow paths
   - Performance warnings

### `list` - Discover Workflows

List and discover workflow files in the current directory and subdirectories.

#### Basic Syntax
```bash
dotnet prompt list [path] [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--recursive` | `-r` | flag | Search subdirectories | false |
| `--show-metadata` | `-m` | flag | Show workflow metadata | false |
| `--filter` | `-f` | string | Filter by pattern | None |
| `--output` | `-o` | string | Output format (table/json/yaml) | table |

#### Examples

**List workflows in current directory:**
```bash
dotnet prompt list
```

**Recursive search with metadata:**
```bash
dotnet prompt list --recursive --show-metadata
```

**Filter by pattern:**
```bash
dotnet prompt list --recursive --filter "*analysis*"
```

**JSON output for scripting:**
```bash
dotnet prompt list --recursive --output json
```

#### Output Formats

**Table format (default):**
```
Name                    | File                          | Model   | Tools
------------------------|-------------------------------|---------|------------------
project-analysis        | project-analysis.prompt.md   | gpt-4o  | project-analysis
code-review             | code-review.prompt.md        | gpt-4o  | project-analysis, file-system
documentation-generator | docs/generator.prompt.md     | gpt-4o  | project-analysis, file-system
```

**JSON format:**
```json
[
  {
    "name": "project-analysis",
    "file": "project-analysis.prompt.md",
    "model": "gpt-4o",
    "tools": ["project-analysis"],
    "description": "Comprehensive project analysis workflow"
  }
]
```

### `resume` - Resume Workflows

Resume interrupted workflows from progress files.

#### Basic Syntax
```bash
dotnet prompt resume <workflow-file> [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--progress` | `-p` | string | Progress file path | Auto-discovered |
| `--force` | `-f` | flag | Force resume despite warnings | false |
| `--verbose` | `-v` | flag | Enable verbose output | false |

#### Examples

**Resume from automatic progress file:**
```bash
dotnet prompt resume workflow.prompt.md
```

**Resume from specific progress file:**
```bash
dotnet prompt resume workflow.prompt.md --progress ./custom-progress.md
```

**Force resume with warnings:**
```bash
dotnet prompt resume workflow.prompt.md --force
```

#### Resume Validation

Before resuming, the system validates:
- Progress file compatibility with current workflow
- Model and configuration consistency
- Tool availability
- Context and environment consistency

## Configuration Commands

### `config` - Configuration Management

Manage dotnet-prompt configuration at global and project levels.

#### Basic Syntax
```bash
dotnet prompt config <subcommand> [options]
```

#### Subcommands

##### `show` - Display Configuration

```bash
# Show effective configuration
dotnet prompt config show

# Show global configuration only
dotnet prompt config show --global

# Show project configuration only
dotnet prompt config show --project

# Show configuration sources
dotnet prompt config show --sources
```

##### `set` - Set Configuration Values

```bash
# Set global default provider
dotnet prompt config set default_provider openai --global

# Set project-level model
dotnet prompt config set default_model gpt-4 --project

# Set tool configuration
dotnet prompt config set tool_configuration.project_analysis.excluded_directories bin,obj --global
```

##### `get` - Get Configuration Values

```bash
# Get specific configuration value
dotnet prompt config get default_provider

# Get nested configuration
dotnet prompt config get providers.openai.api_key
```

##### `unset` - Remove Configuration Values

```bash
# Remove global setting
dotnet prompt config unset default_provider --global

# Remove project setting
dotnet prompt config unset default_model --project
```

##### `init` - Initialize Configuration

```bash
# Initialize project configuration
dotnet prompt init

# Initialize with specific provider
dotnet prompt init --provider azure

# Initialize with template
dotnet prompt init --template enterprise
```

#### Configuration Examples

**Set up OpenAI provider:**
```bash
dotnet prompt config set providers.openai.api_key sk-xxxxx --global
dotnet prompt config set default_provider openai --global
```

**Configure GitHub Models:**
```bash
dotnet prompt config set providers.github.token ghp_xxxxx --global
dotnet prompt config set providers.github.endpoint https://models.inference.ai.azure.com --global
dotnet prompt config set default_provider github --global
```

**Project-specific settings:**
```bash
dotnet prompt config set default_model gpt-4o --project
dotnet prompt config set tool_configuration.file_system.max_file_size 5MB --project
```

### `init` - Initialize Projects

Initialize dotnet-prompt configuration for new projects.

#### Basic Syntax
```bash
dotnet prompt init [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--provider` | `-p` | string | Default AI provider | github |
| `--template` | `-t` | string | Configuration template | basic |
| `--force` | `-f` | flag | Overwrite existing config | false |

#### Examples

**Basic initialization:**
```bash
dotnet prompt init
```

**Initialize with Azure OpenAI:**
```bash
dotnet prompt init --provider azure
```

**Use enterprise template:**
```bash
dotnet prompt init --template enterprise --force
```

#### Configuration Templates

- **basic**: Basic configuration with GitHub Models
- **enterprise**: Enterprise configuration with security settings
- **development**: Development-focused configuration
- **ci-cd**: CI/CD pipeline configuration

## MCP Commands

### `mcp` - MCP Server Management

Manage Model Context Protocol servers and their integration.

#### Basic Syntax
```bash
dotnet prompt mcp <subcommand> [options]
```

#### Subcommands

##### `list` - List MCP Servers

```bash
# List configured servers
dotnet prompt mcp list

# List with detailed information
dotnet prompt mcp list --detailed

# List available servers from registry
dotnet prompt mcp list --available
```

##### `status` - Check Server Status

```bash
# Check all server status
dotnet prompt mcp status

# Check specific server
dotnet prompt mcp status filesystem-mcp

# Continuous monitoring
dotnet prompt mcp status --watch
```

##### `test` - Test Server Connectivity

```bash
# Test specific server
dotnet prompt mcp test filesystem-mcp

# Test all servers
dotnet prompt mcp test --all

# Test with verbose output
dotnet prompt mcp test filesystem-mcp --verbose
```

##### `install` - Install MCP Servers

```bash
# Install from npm
dotnet prompt mcp install @modelcontextprotocol/server-filesystem

# Install specific version
dotnet prompt mcp install @modelcontextprotocol/server-git@2.0.0

# Install globally
dotnet prompt mcp install server-name --global
```

##### `logs` - View Server Logs

```bash
# View logs for specific server
dotnet prompt mcp logs filesystem-mcp

# Follow logs in real-time
dotnet prompt mcp logs filesystem-mcp --follow

# Show last N lines
dotnet prompt mcp logs filesystem-mcp --lines 100
```

## Utility Commands

### `restore` - Restore Dependencies

Restore and install workflow dependencies including MCP servers.

#### Basic Syntax
```bash
dotnet prompt restore [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--force` | `-f` | flag | Force reinstall | false |
| `--verbose` | `-v` | flag | Verbose output | false |
| `--mcp-only` | | flag | Restore MCP servers only | false |

#### Examples

**Basic restore:**
```bash
dotnet prompt restore
```

**Force reinstall all dependencies:**
```bash
dotnet prompt restore --force --verbose
```

**Restore only MCP servers:**
```bash
dotnet prompt restore --mcp-only
```

### `clean` - Clean Artifacts

Clean generated files, progress files, and temporary artifacts.

#### Basic Syntax
```bash
dotnet prompt clean [options]
```

#### Options

| Option | Short | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--progress` | `-p` | flag | Clean progress files | false |
| `--cache` | `-c` | flag | Clean cache files | false |
| `--all` | `-a` | flag | Clean all artifacts | false |
| `--dry-run` | `-d` | flag | Show what would be cleaned | false |

#### Examples

**Clean progress files:**
```bash
dotnet prompt clean --progress
```

**Clean all artifacts:**
```bash
dotnet prompt clean --all
```

**Preview cleanup:**
```bash
dotnet prompt clean --all --dry-run
```

## Environment Variables

### Configuration Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DOTNET_PROMPT_CONFIG` | Configuration file path | Auto-discovered |
| `DOTNET_PROMPT_PROVIDER` | Default AI provider | github |
| `DOTNET_PROMPT_MODEL` | Default AI model | gpt-4o |
| `DOTNET_PROMPT_VERBOSE` | Enable verbose logging | false |
| `DOTNET_PROMPT_TIMEOUT` | Default timeout (seconds) | 300 |
| `DOTNET_PROMPT_NO_TELEMETRY` | Disable telemetry | false |

### Provider-Specific Variables

| Variable | Description |
|----------|-------------|
| `OPENAI_API_KEY` | OpenAI API key |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key |
| `ANTHROPIC_API_KEY` | Anthropic API key |
| `GITHUB_TOKEN` | GitHub token for Models API |

### MCP Variables

| Variable | Description |
|----------|-------------|
| `MCP_SERVER_PATH` | Additional MCP server search path |
| `MCP_TIMEOUT` | Default MCP server timeout |
| `MCP_LOG_LEVEL` | MCP logging level (debug/info/warn/error) |

## Global Options

These options are available for most commands:

| Option | Short | Description |
|--------|-------|-------------|
| `--help` | `-h` | Show help information |
| `--verbose` | `-v` | Enable verbose output |
| `--quiet` | `-q` | Suppress non-essential output |
| `--config` | `-c` | Configuration file path |
| `--no-color` | | Disable colored output |

## Exit Codes

All commands use consistent exit codes:

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Command completed successfully |
| 1 | GeneralError | General command error |
| 2 | FileNotFound | Required file not found |
| 3 | ValidationError | Validation or syntax error |
| 4 | ConfigurationError | Configuration error |
| 5 | TimeoutError | Operation timeout |
| 6 | UserCancelled | User cancelled operation |
| 7 | NetworkError | Network connectivity error |
| 8 | AuthenticationError | Authentication failed |
| 9 | PermissionError | Insufficient permissions |
| 10 | DependencyError | Missing dependencies |

## Scripting and Automation

### JSON Output

Many commands support JSON output for scripting:

```bash
# Get workflow list as JSON
dotnet prompt list --output json

# Get configuration as JSON
dotnet prompt config show --output json

# Validate with JSON results
dotnet prompt validate workflow.prompt.md --output json
```

### Exit Code Handling

```bash
#!/bin/bash
dotnet prompt run workflow.prompt.md
case $? in
  0) echo "Success" ;;
  3) echo "Validation error" ;;
  4) echo "Configuration error" ;;
  *) echo "Other error: $?" ;;
esac
```

### Batch Processing

```bash
#!/bin/bash
# Process all workflows in directory
for workflow in *.prompt.md; do
  echo "Processing $workflow..."
  dotnet prompt run "$workflow" --timeout 600
  if [ $? -ne 0 ]; then
    echo "Failed: $workflow"
  fi
done
```

## Troubleshooting Commands

### Diagnostic Information

```bash
# Show version and environment info
dotnet prompt --version --verbose

# Test MCP server connectivity
dotnet prompt mcp test --all

# Validate configuration
dotnet prompt config show --sources

# Check workflow syntax
dotnet prompt validate workflow.prompt.md --strict
```

### Common Issues

**Tool not found:**
```bash
# Check installation
dotnet tool list -g | grep dotnet-prompt

# Reinstall if needed
dotnet tool uninstall -g dotnet-prompt
dotnet tool install -g dotnet-prompt
```

**Configuration issues:**
```bash
# Show effective configuration
dotnet prompt config show --sources

# Reset configuration
dotnet prompt config init --force
```

**MCP server issues:**
```bash
# Check server status
dotnet prompt mcp status

# View server logs
dotnet prompt mcp logs server-name --follow
```

## Next Steps

- **[Configuration Options](./configuration-options.md)**: Detailed configuration guide
- **[Built-in Tools](./built-in-tools.md)**: Reference for available tools
- **[Error Codes](./error-codes.md)**: Complete error code reference
- **[Troubleshooting](../user-guide/troubleshooting.md)**: Common issues and solutions