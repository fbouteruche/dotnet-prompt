# Configuration Options Reference

Complete guide to configuring dotnet-prompt for optimal performance and security across different environments.

## Configuration Hierarchy

dotnet-prompt uses a hierarchical configuration system where settings are merged in the following priority order:

1. **CLI Arguments** (highest priority)
2. **Workflow Frontmatter** 
3. **Project Configuration** (`.dotnet-prompt/config.json`)
4. **Global Configuration** (`~/.dotnet-prompt/config.json`)
5. **Default Values** (lowest priority)

## Configuration File Locations

### Global Configuration
```
~/.dotnet-prompt/config.json
```

### Project Configuration
```
.dotnet-prompt/config.json
./config/dotnet-prompt.json
```

### Environment Variables
All configuration options can be overridden using environment variables with the prefix `DOTNET_PROMPT_`.

## Core Configuration Options

### Provider Configuration

Configure AI providers and their authentication:

```json
{
  "providers": {
    "github": {
      "endpoint": "https://models.inference.ai.azure.com",
      "token": "${GITHUB_TOKEN}",
      "models": ["gpt-4o", "gpt-4o-mini", "gpt-3.5-turbo"],
      "default_model": "gpt-4o",
      "timeout": 30000,
      "max_retries": 3
    },
    "openai": {
      "api_key": "${OPENAI_API_KEY}",
      "organization": "${OPENAI_ORG_ID}",
      "endpoint": "https://api.openai.com/v1",
      "models": ["gpt-4", "gpt-3.5-turbo"],
      "default_model": "gpt-4",
      "timeout": 30000,
      "max_retries": 3
    },
    "azure": {
      "endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "api_key": "${AZURE_OPENAI_API_KEY}",
      "api_version": "2024-02-15-preview",
      "deployment_name": "gpt-4",
      "timeout": 30000,
      "max_retries": 3
    },
    "anthropic": {
      "api_key": "${ANTHROPIC_API_KEY}",
      "endpoint": "https://api.anthropic.com",
      "models": ["claude-3-sonnet", "claude-3-haiku"],
      "default_model": "claude-3-sonnet",
      "timeout": 30000,
      "max_retries": 3
    },
    "local": {
      "endpoint": "http://localhost:1234/v1",
      "models": ["local-model"],
      "default_model": "local-model",
      "timeout": 60000,
      "api_key": "not-needed"
    }
  },
  "default_provider": "github"
}
```

**Provider Configuration Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `endpoint` | string | Yes | API endpoint URL |
| `api_key` or `token` | string | Yes | Authentication credential |
| `models` | array | No | Available models for this provider |
| `default_model` | string | No | Default model to use |
| `timeout` | integer | No | Request timeout in milliseconds |
| `max_retries` | integer | No | Maximum retry attempts |
| `organization` | string | No | Organization ID (OpenAI only) |
| `api_version` | string | No | API version (Azure only) |
| `deployment_name` | string | No | Deployment name (Azure only) |

### Default Settings

Configure default behavior for workflows:

```json
{
  "default_model": "gpt-4o",
  "default_provider": "github",
  "default_timeout": 300,
  "default_temperature": 0.7,
  "default_max_tokens": 4000,
  "enable_verbose_logging": false,
  "enable_telemetry": true,
  "auto_save_progress": true,
  "progress_checkpoint_frequency": "tool_execution"
}
```

**Default Settings Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `default_model` | string | "gpt-4o" | Default AI model |
| `default_provider` | string | "github" | Default AI provider |
| `default_timeout` | integer | 300 | Default execution timeout (seconds) |
| `default_temperature` | number | 0.7 | Default creativity level |
| `default_max_tokens` | integer | 4000 | Default response length limit |
| `enable_verbose_logging` | boolean | false | Enable detailed logging |
| `enable_telemetry` | boolean | true | Enable usage telemetry |
| `auto_save_progress` | boolean | true | Automatically save progress |
| `progress_checkpoint_frequency` | string | "tool_execution" | Progress save frequency |

## Tool Configuration

Configure built-in tools for optimal performance and security:

### Project Analysis Tool

```json
{
  "tool_configuration": {
    "project_analysis": {
      "excluded_directories": [
        "bin", "obj", ".git", "node_modules", "packages", 
        ".vs", ".vscode", "TestResults", "coverage"
      ],
      "excluded_file_patterns": [
        "*.dll", "*.exe", "*.pdb", "*.cache", "*.tmp"
      ],
      "max_file_size_bytes": 1048576,
      "include_test_projects": true,
      "analyze_dependencies": true,
      "check_vulnerabilities": false,
      "code_metrics_enabled": true,
      "documentation_analysis": true,
      "performance_analysis": false,
      "max_analysis_depth": 10,
      "timeout_seconds": 120
    }
  }
}
```

**Project Analysis Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `excluded_directories` | array | ["bin", "obj", ".git"] | Directories to skip |
| `excluded_file_patterns` | array | ["*.dll", "*.exe"] | File patterns to skip |
| `max_file_size_bytes` | integer | 1048576 | Max file size to analyze (1MB) |
| `include_test_projects` | boolean | true | Include test projects |
| `analyze_dependencies` | boolean | true | Perform dependency analysis |
| `check_vulnerabilities` | boolean | false | Check for vulnerabilities |
| `code_metrics_enabled` | boolean | true | Calculate code metrics |
| `documentation_analysis` | boolean | true | Analyze documentation |
| `performance_analysis` | boolean | false | Perform performance analysis |
| `max_analysis_depth` | integer | 10 | Maximum directory depth |
| `timeout_seconds` | integer | 120 | Analysis timeout |

### Build & Test Tool

```json
{
  "tool_configuration": {
    "build_test": {
      "default_configuration": "Release",
      "enable_parallel_builds": true,
      "restore_before_build": true,
      "clean_before_build": true,
      "test_timeout_seconds": 300,
      "collect_coverage": true,
      "coverage_threshold": 80,
      "coverage_formats": ["opencover", "cobertura"],
      "fail_on_warnings": false,
      "verbosity": "minimal",
      "enable_logger": true,
      "artifact_retention_days": 30
    }
  }
}
```

**Build & Test Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `default_configuration` | string | "Release" | Default build configuration |
| `enable_parallel_builds` | boolean | true | Enable parallel builds |
| `restore_before_build` | boolean | true | Restore packages before build |
| `clean_before_build` | boolean | true | Clean before build |
| `test_timeout_seconds` | integer | 300 | Test execution timeout |
| `collect_coverage` | boolean | true | Collect test coverage |
| `coverage_threshold` | integer | 80 | Minimum coverage percentage |
| `coverage_formats` | array | ["opencover"] | Coverage report formats |
| `fail_on_warnings` | boolean | false | Treat warnings as errors |
| `verbosity` | string | "minimal" | Build verbosity level |
| `enable_logger` | boolean | true | Enable build logging |
| `artifact_retention_days` | integer | 30 | Days to retain artifacts |

### File System Tool

```json
{
  "tool_configuration": {
    "file_system": {
      "working_directory_only": true,
      "max_file_size_bytes": 10485760,
      "backup_enabled": true,
      "backup_directory": "./.dotnet-prompt/backups",
      "audit_logging": true,
      "allowed_extensions": [
        ".cs", ".fs", ".vb", ".json", ".xml", ".yml", ".yaml", 
        ".md", ".txt", ".config", ".props", ".targets"
      ],
      "denied_paths": [
        ".git", "bin", "obj", "node_modules", "packages", 
        ".vs", ".vscode", "TestResults"
      ],
      "default_encoding": "utf-8",
      "create_directories": true,
      "overwrite_readonly": false,
      "preserve_permissions": true
    }
  }
}
```

**File System Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `working_directory_only` | boolean | true | Restrict to working directory |
| `max_file_size_bytes` | integer | 10485760 | Max file size (10MB) |
| `backup_enabled` | boolean | true | Create backups |
| `backup_directory` | string | "./.dotnet-prompt/backups" | Backup location |
| `audit_logging` | boolean | true | Log file operations |
| `allowed_extensions` | array | [".cs", ".json"] | Allowed file extensions |
| `denied_paths` | array | [".git", "bin"] | Denied path patterns |
| `default_encoding` | string | "utf-8" | Default text encoding |
| `create_directories` | boolean | true | Auto-create directories |
| `overwrite_readonly` | boolean | false | Overwrite readonly files |
| `preserve_permissions` | boolean | true | Preserve file permissions |

### Sub-workflow Tool

```json
{
  "tool_configuration": {
    "sub_workflow": {
      "max_depth": 5,
      "timeout_seconds": 1800,
      "inherit_context": true,
      "preserve_conversation": true,
      "parallel_execution": true,
      "error_handling": "continue_on_error",
      "parameter_validation": true,
      "cache_results": false,
      "max_concurrent_workflows": 3
    }
  }
}
```

**Sub-workflow Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `max_depth` | integer | 5 | Max sub-workflow nesting |
| `timeout_seconds` | integer | 1800 | Sub-workflow timeout |
| `inherit_context` | boolean | true | Inherit parent context |
| `preserve_conversation` | boolean | true | Maintain conversation |
| `parallel_execution` | boolean | true | Enable parallel execution |
| `error_handling` | string | "continue_on_error" | Error handling strategy |
| `parameter_validation` | boolean | true | Validate parameters |
| `cache_results` | boolean | false | Cache sub-workflow results |
| `max_concurrent_workflows` | integer | 3 | Max parallel workflows |

## MCP Server Configuration

Configure Model Context Protocol servers:

```json
{
  "mcp_servers": {
    "filesystem-mcp": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-filesystem"],
      "version": "1.0.0",
      "timeout": 30000,
      "auto_start": true,
      "restart_on_failure": true,
      "max_restart_attempts": 3,
      "environment": {
        "NODE_ENV": "production"
      }
    },
    "git-mcp": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-git"],
      "version": "2.0.0",
      "timeout": 30000,
      "auto_start": true,
      "working_directory": ".",
      "environment": {
        "GIT_CONFIG_GLOBAL": "/dev/null"
      }
    },
    "github-mcp": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-github"],
      "version": "2.1.0",
      "timeout": 30000,
      "auto_start": false,
      "environment": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      }
    }
  },
  "mcp_global_settings": {
    "default_timeout": 30000,
    "auto_discovery": true,
    "function_naming_convention": "snake_case",
    "error_handling_strategy": "sk_filters",
    "telemetry_enabled": true,
    "vector_store_caching": false,
    "retry_policy": {
      "max_attempts": 3,
      "backoff_ms": [1000, 2000, 4000],
      "exponential_backoff": true
    }
  }
}
```

**MCP Server Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `command` | string | Required | Command to start server |
| `args` | array | [] | Command arguments |
| `version` | string | "latest" | Server version |
| `timeout` | integer | 30000 | Connection timeout (ms) |
| `auto_start` | boolean | true | Auto-start with workflow |
| `restart_on_failure` | boolean | true | Restart on failure |
| `max_restart_attempts` | integer | 3 | Max restart attempts |
| `working_directory` | string | "." | Server working directory |
| `environment` | object | {} | Environment variables |

## Security Configuration

Configure security settings and access controls:

```json
{
  "security": {
    "enable_sandbox": true,
    "working_directory_restriction": true,
    "allowed_network_hosts": [
      "api.openai.com",
      "api.anthropic.com",
      "models.inference.ai.azure.com"
    ],
    "credential_encryption": true,
    "audit_logging": true,
    "audit_log_path": "~/.dotnet-prompt/logs/audit.log",
    "max_file_size_mb": 100,
    "max_execution_time_minutes": 60,
    "require_confirmation_for_destructive_operations": true
  }
}
```

**Security Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `enable_sandbox` | boolean | true | Enable security sandbox |
| `working_directory_restriction` | boolean | true | Restrict to working directory |
| `allowed_network_hosts` | array | [] | Allowed network hosts |
| `credential_encryption` | boolean | true | Encrypt stored credentials |
| `audit_logging` | boolean | true | Enable audit logging |
| `audit_log_path` | string | "~/.dotnet-prompt/logs/audit.log" | Audit log location |
| `max_file_size_mb` | integer | 100 | Max file size limit |
| `max_execution_time_minutes` | integer | 60 | Max execution time |
| `require_confirmation_for_destructive_operations` | boolean | true | Require confirmation |

## Logging and Telemetry

Configure logging behavior and telemetry collection:

```json
{
  "logging": {
    "level": "Information",
    "enable_console_logging": true,
    "enable_file_logging": true,
    "log_file_path": "~/.dotnet-prompt/logs/dotnet-prompt.log",
    "max_log_file_size_mb": 10,
    "max_log_files": 5,
    "log_format": "json",
    "include_timestamps": true,
    "include_correlation_ids": true,
    "mask_sensitive_data": true
  },
  "telemetry": {
    "enabled": true,
    "include_performance_metrics": true,
    "include_error_reports": true,
    "include_usage_statistics": true,
    "anonymous_data_only": true,
    "endpoint": "https://telemetry.dotnet-prompt.io",
    "batch_size": 10,
    "flush_interval_seconds": 300
  }
}
```

**Logging Configuration:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `level` | string | "Information" | Log level (Debug, Information, Warning, Error) |
| `enable_console_logging` | boolean | true | Log to console |
| `enable_file_logging` | boolean | true | Log to file |
| `log_file_path` | string | "~/.dotnet-prompt/logs/dotnet-prompt.log" | Log file location |
| `max_log_file_size_mb` | integer | 10 | Max log file size |
| `max_log_files` | integer | 5 | Max log file count |
| `log_format` | string | "json" | Log format (json, text) |
| `include_timestamps` | boolean | true | Include timestamps |
| `include_correlation_ids` | boolean | true | Include correlation IDs |
| `mask_sensitive_data` | boolean | true | Mask sensitive information |

## Environment-Specific Configuration

### Development Environment

```json
{
  "default_provider": "github",
  "default_model": "gpt-4o-mini",
  "default_temperature": 0.8,
  "enable_verbose_logging": true,
  "logging": {
    "level": "Debug"
  },
  "tool_configuration": {
    "project_analysis": {
      "check_vulnerabilities": false,
      "performance_analysis": false
    },
    "build_test": {
      "default_configuration": "Debug",
      "fail_on_warnings": false
    }
  }
}
```

### Staging Environment

```json
{
  "default_provider": "azure",
  "default_model": "gpt-4",
  "default_temperature": 0.5,
  "enable_verbose_logging": false,
  "tool_configuration": {
    "project_analysis": {
      "check_vulnerabilities": true,
      "performance_analysis": true
    },
    "build_test": {
      "default_configuration": "Release",
      "fail_on_warnings": true,
      "coverage_threshold": 80
    }
  },
  "security": {
    "require_confirmation_for_destructive_operations": true
  }
}
```

### Production Environment

```json
{
  "default_provider": "azure",
  "default_model": "gpt-4",
  "default_temperature": 0.3,
  "enable_verbose_logging": false,
  "logging": {
    "level": "Warning"
  },
  "tool_configuration": {
    "project_analysis": {
      "check_vulnerabilities": true,
      "performance_analysis": true,
      "timeout_seconds": 300
    },
    "build_test": {
      "default_configuration": "Release",
      "fail_on_warnings": true,
      "coverage_threshold": 90
    },
    "file_system": {
      "backup_enabled": true,
      "audit_logging": true
    }
  },
  "security": {
    "enable_sandbox": true,
    "credential_encryption": true,
    "audit_logging": true,
    "require_confirmation_for_destructive_operations": true
  }
}
```

## Configuration Management

### Environment Variables

All configuration options can be set via environment variables:

```bash
# Provider configuration
export DOTNET_PROMPT_DEFAULT_PROVIDER=openai
export DOTNET_PROMPT_DEFAULT_MODEL=gpt-4
export OPENAI_API_KEY=sk-your-key-here

# Tool configuration
export DOTNET_PROMPT_TOOL_CONFIGURATION__PROJECT_ANALYSIS__MAX_FILE_SIZE_BYTES=2097152

# Logging configuration
export DOTNET_PROMPT_LOGGING__LEVEL=Debug
export DOTNET_PROMPT_ENABLE_VERBOSE_LOGGING=true

# Security configuration
export DOTNET_PROMPT_SECURITY__ENABLE_SANDBOX=true
```

### Configuration Templates

#### Basic Template

```bash
dotnet prompt init --template basic
```

Creates minimal configuration with GitHub Models provider.

#### Enterprise Template

```bash
dotnet prompt init --template enterprise
```

Creates comprehensive configuration with security features, audit logging, and multiple providers.

#### Development Template

```bash
dotnet prompt init --template development
```

Creates development-friendly configuration with verbose logging and relaxed security.

### Configuration Validation

Validate configuration before use:

```bash
# Validate current configuration
dotnet prompt config validate

# Check specific provider configuration
dotnet prompt config validate --provider openai

# Test provider connectivity
dotnet prompt config test --provider github
```

### Configuration Migration

Migrate configuration between versions:

```bash
# Backup current configuration
dotnet prompt config backup

# Migrate to new version
dotnet prompt config migrate --from 1.0 --to 2.0

# Restore from backup if needed
dotnet prompt config restore --backup-file config-backup-20241201.json
```

## Best Practices

### Security

1. **Use Environment Variables**: Store sensitive data in environment variables
2. **Enable Encryption**: Encrypt stored credentials
3. **Audit Logging**: Enable comprehensive audit logging
4. **Principle of Least Privilege**: Restrict permissions to minimum required
5. **Regular Rotation**: Rotate API keys and credentials regularly

### Performance

1. **Optimize Tool Configuration**: Exclude unnecessary files and directories
2. **Use Appropriate Models**: Choose models based on task complexity
3. **Enable Caching**: Use caching for repeated operations
4. **Parallel Processing**: Enable parallel execution where possible
5. **Monitor Resource Usage**: Track memory and CPU usage

### Maintenance

1. **Regular Updates**: Keep configuration current with new versions
2. **Monitor Logs**: Review logs regularly for issues
3. **Test Configuration Changes**: Validate changes in non-production environments
4. **Document Custom Settings**: Document any custom configuration choices
5. **Backup Configuration**: Regularly backup configuration files

This comprehensive configuration guide ensures optimal performance, security, and maintainability of dotnet-prompt across different environments and use cases.