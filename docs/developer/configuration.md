# Configuration System

The dotnet-prompt configuration system provides hierarchical configuration management with support for global settings, project-specific configurations, environment variables, and CLI overrides.

## Configuration Hierarchy

Configuration values are resolved in the following order (highest to lowest precedence):

1. **CLI Arguments** - `--provider`, `--model`, `--verbose`, etc.
2. **Environment Variables** - `DOTNET_PROMPT_*` prefixed variables
3. **Workflow Frontmatter** - `model` and other properties in `.prompt.md` YAML frontmatter
4. **Project Configuration** - `./dotnet-prompt.yaml` in current directory
5. **Global Configuration** - `~/.dotnet-prompt/config.yaml` in user profile
6. **Default Values** - Built-in sensible defaults

### Model Provider Specification in Workflow Files

When you specify the `model` property in a workflow file's YAML frontmatter, you can use two formats:

1. **Provider/Model Format**: `model: "provider/model"` - Specifies both provider and model
2. **Model Only Format**: `model: "model"` - Uses configured default provider

**Examples:**
```yaml
---
# Explicit provider/model specification (overrides all configuration)
model: "github/gpt-4o"        # Use GitHub Models provider with gpt-4o
model: "openai/gpt-4"         # Use OpenAI provider with gpt-4
model: "azure/gpt-35-turbo"   # Use Azure OpenAI with gpt-35-turbo
model: "local/llama2"         # Use local Ollama with llama2

# Model only (uses default_provider from configuration)
model: "gpt-4o"               # Use configured provider with gpt-4o
---
```

**Resolution Behavior:**
- If `model` contains a `/`, it's parsed as `provider/model` and overrides both provider and model settings
- If `model` contains no `/`, it's treated as just the model name and uses the configured `default_provider`
- Frontmatter model specification takes precedence over project/global configuration but can be overridden by CLI arguments

## Configuration Files

### Supported Formats

- **YAML** (recommended): `.yaml` or `.yml` extensions
- **JSON**: `.json` extension

### File Locations

- **Global**: `~/.dotnet-prompt/config.yaml` (Unix) or `%USERPROFILE%\.dotnet-prompt\config.yaml` (Windows)
- **Project**: `./dotnet-prompt.yaml` in project directory

## Configuration Schema

```yaml
# Provider and model defaults
default_provider: "github"        # Default AI provider
default_model: "gpt-4o"          # Default model

# Request settings
timeout: 300                     # Request timeout in seconds
cache_enabled: true              # Enable response caching
cache_directory: "./.dotnet-prompt/cache"
telemetry_enabled: true          # Enable usage telemetry

# Provider configurations
providers:
  github:
    token: "${GITHUB_TOKEN}"
    base_url: "https://models.inference.ai.azure.com"
  
  openai:
    api_key: "${OPENAI_API_KEY}"
    base_url: "https://api.openai.com/v1"
    timeout: 300
    max_retries: 3
  
  azure:
    api_key: "${AZURE_OPENAI_API_KEY}"
    endpoint: "${AZURE_OPENAI_ENDPOINT}"
    deployment: "gpt-4"
    api_version: "2024-06-01"

# Logging configuration
logging:
  level: "Information"           # Trace, Debug, Information, Warning, Error, Critical
  console: true                  # Enable console output
  file: "./logs/dotnet-prompt.log"  # Log file path
  structured: false              # JSON structured logging
  include_scopes: false          # Include log scopes

# Tool-specific configuration
tool_configuration:
  project_analysis:
    include_private_members: false
    max_file_size_bytes: 1048576
    excluded_directories: ["bin", "obj", ".git"]
  
  build_test:
    default_configuration: "Debug"
    verbose_logging: false
    parallel_execution: true
```

## Environment Variables

Environment variables override configuration file values:

- `DOTNET_PROMPT_PROVIDER` - Default AI provider
- `DOTNET_PROMPT_MODEL` - Default model
- `DOTNET_PROMPT_VERBOSE` - Enable verbose logging (true/false)
- `DOTNET_PROMPT_CONFIG` - Path to configuration file
- `DOTNET_PROMPT_TIMEOUT` - Default timeout in seconds
- `DOTNET_PROMPT_NO_TELEMETRY` - Disable telemetry (true/false)
- `DOTNET_PROMPT_CACHE_DIR` - Directory for caching

### Environment Variable Substitution

Configuration files support environment variable substitution using `${VAR_NAME}` syntax:

```yaml
providers:
  openai:
    api_key: "${OPENAI_API_KEY}"     # Replaced with actual env var value
    base_url: "${OPENAI_BASE_URL:-https://api.openai.com/v1}"  # With default fallback
```

## CLI Commands

### Initialize Configuration

```bash
# Initialize minimal project configuration
dotnet-prompt config init --minimal

# Initialize comprehensive project configuration
dotnet-prompt config init

# Initialize global configuration
dotnet-prompt config init --global
```

### View Configuration

```bash
# Show effective configuration (all sources merged)
dotnet-prompt config show

# Show only global configuration
dotnet-prompt config show --global

# Show only project configuration
dotnet-prompt config show --project
```

### Validate Configuration

```bash
# Validate current directory configuration
dotnet-prompt config validate

# Validate specific path
dotnet-prompt config validate --path ./my-project
```

### Update Configuration

```bash
# Set configuration values
dotnet-prompt config set default_provider openai
dotnet-prompt config set timeout 600

# Set in global configuration
dotnet-prompt config set default_provider github --global
```

## Examples

### Model Provider Override in Workflow Files

**Scenario 1: Override both provider and model**
```yaml
# Global config: default_provider: "openai", default_model: "gpt-3.5-turbo"
# Workflow frontmatter:
---
name: "analysis-workflow"
model: "github/gpt-4o"  # Overrides to use GitHub Models with gpt-4o
tools: ["project-analysis"]
---
```
Result: Uses GitHub Models provider with gpt-4o model, ignoring global configuration.

**Scenario 2: Override model only, keep configured provider**
```yaml
# Global config: default_provider: "azure", default_model: "gpt-35-turbo"
# Workflow frontmatter:
---
name: "code-review"
model: "gpt-4"  # No slash, so uses configured provider (azure) with gpt-4
---
```
Result: Uses Azure OpenAI provider with gpt-4 model.

**Scenario 3: CLI override wins**
```bash
# Workflow has: model: "github/gpt-4o"
# CLI command:
dotnet prompt run workflow.prompt.md --provider openai --model gpt-3.5-turbo
```
Result: Uses OpenAI provider with gpt-3.5-turbo, overriding the workflow frontmatter.

### Minimal Configuration

```yaml
default_provider: "openai"
providers:
  openai:
    api_key: "${OPENAI_API_KEY}"
```

### Team Project Configuration

```yaml
# .dotnet-prompt/config.yaml - committed to version control
default_provider: "azure"
default_model: "gpt-4"

providers:
  azure:
    endpoint: "https://company.openai.azure.com"
    deployment: "gpt-4-company"
    api_version: "2024-06-01"
    # API key comes from environment variable
    api_key: "${AZURE_OPENAI_API_KEY}"

logging:
  level: "Information"
  structured: true
  file: "./logs/dotnet-prompt.log"

tool_configuration:
  project_analysis:
    excluded_directories: ["bin", "obj", ".git", "packages"]
    include_private_members: false
  
  build_test:
    default_configuration: "Release"
    excluded_test_categories: ["Integration"]
```

### Developer-Specific Global Configuration

```yaml
# ~/.dotnet-prompt/config.yaml - personal settings
default_provider: "openai"
default_model: "gpt-4o"

providers:
  openai:
    api_key: "${OPENAI_API_KEY}"
  
  github:
    token: "${GITHUB_TOKEN}"
  
  local:
    base_url: "http://localhost:11434"  # Local Ollama

logging:
  level: "Debug"
  console: true

cache_enabled: true
cache_directory: "~/.dotnet-prompt/cache"
```

## Validation

The configuration system provides comprehensive validation with helpful error messages:

```bash
$ dotnet-prompt config validate
Configuration Validation Results:

✗ Configuration has errors

Errors:
  ✗ providers.azure.endpoint: Invalid URL format for provider 'azure' endpoint
    Code: INVALID_URL
  ✗ timeout: Timeout must be greater than 0
    Code: INVALID_TIMEOUT

Warnings:
  ⚠ providers.openai: Provider 'openai' has no API key or token configured
    Code: MISSING_CREDENTIALS
```

## Security Considerations

1. **Never commit API keys** - Use environment variables with `${VAR_NAME}` syntax
2. **Use appropriate file permissions** - Global config should be readable only by user
3. **Separate team and personal configs** - Keep sensitive credentials in global/environment variables
4. **Regular credential rotation** - Update environment variables, not config files

## Best Practices

1. **Use minimal project configs** - Only override what's needed for the project
2. **Document required environment variables** - Include `.env.example` or README sections
3. **Validate regularly** - Run `config validate` as part of CI/CD
4. **Use structured logging in CI** - Set `logging.structured: true` for automated environments
5. **Cache in project directories** - Use relative paths for `cache_directory` in project configs
6. **Specify providers explicitly in workflows** - Use `provider/model` format for reproducible results across different environments

## Troubleshooting

### Common Model Provider Issues

**Issue**: Workflow fails with "Unknown provider" error
```
Error: Unknown provider: github
```
**Solution**: Check that the provider is configured in your configuration file:
```yaml
providers:
  github:
    token: "${GITHUB_TOKEN}"
    base_url: "https://models.inference.ai.azure.com"
```

**Issue**: Model not found error
```
Error: Model 'gpt-5' not found for provider 'openai'
```
**Solution**: Verify the model name is correct for the provider. Use `dotnet prompt config show --effective` to see resolved configuration.

**Issue**: Authentication failure
```
Error: Authentication failed for provider 'azure'
```
**Solution**: Check that environment variables are set correctly and that API keys/tokens are valid:
```bash
echo $AZURE_OPENAI_API_KEY  # Should show your key
echo $AZURE_OPENAI_ENDPOINT # Should show your endpoint
```

**Issue**: Workflow uses wrong provider
**Solution**: Check the precedence order. CLI arguments override workflow frontmatter:
1. CLI: `--provider openai --model gpt-4`
2. Workflow: `model: "github/gpt-4o"`
3. Project config: `default_provider: "azure"`

Use `dotnet prompt run workflow.prompt.md --dry-run --verbose` to see which configuration will be used.