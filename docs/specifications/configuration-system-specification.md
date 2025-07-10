# Configuration System Specification

## Overview

This document defines the hierarchical configuration system, including merging logic, validation rules, and environment variable support.

## Status
✅ **COMPLETE** - Configuration hierarchy and implementation patterns defined

## Configuration Hierarchy

The tool uses a hierarchical configuration system with the following precedence (highest to lowest):

1. **CLI Arguments** (`--provider`, `--verbose`, etc.)
2. **Workflow Frontmatter** (YAML configuration in .prompt.md files)
3. **Project Configuration** (`.dotnet-prompt/config.json` in project root)
4. **Global Configuration** (`~/.dotnet-prompt/config.json` in user profile)
5. **Default Values** (Built-in defaults)

## Configuration Implementation

### Configuration Resolution Logic (SK-Enhanced)

**Semantic Kernel Integration:**
The configuration system leverages SK's dependency injection patterns and service provider infrastructure for consistent configuration management across all components.

**Primary Configuration Resolution:**
```csharp
public class DotNetPromptConfiguration
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly IVectorStore _vectorStore;
    
    public static async Task<PromptConfiguration> ResolveConfigurationAsync(
        IKernelBuilder kernelBuilder,         // SK kernel builder for DI integration
        string? cliProvider = null,           // 1. CLI --provider override
        string? frontmatterModel = null,      // 2. Workflow frontmatter model
        string? projectPath = null,           // 3. Project .dotnet-prompt/config.json
        CancellationToken cancellationToken = default)  // 4. Global ~/.dotnet-prompt/config.json
    {
        var config = new PromptConfiguration();
        
        // Configure SK services based on resolved configuration
        await ConfigureSemanticKernelServices(kernelBuilder, config);
        
        // 4. Load global configuration first (lowest priority)
        var globalConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                                           ".dotnet-prompt", "config.json");
        if (File.Exists(globalConfigPath))
        {
            var globalConfig = await JsonSerializer.DeserializeAsync<GlobalConfiguration>(
                File.OpenRead(globalConfigPath), cancellationToken: cancellationToken);
            config.ApplyGlobalConfiguration(globalConfig);
            
            // Apply SK-specific global settings
            await ApplySemanticKernelGlobalSettings(kernelBuilder, globalConfig);
        }
        
        // 3. Override with project configuration
        if (!string.IsNullOrEmpty(projectPath))
        {
            var projectConfigPath = Path.Combine(projectPath, ".dotnet-prompt", "config.json");
            if (File.Exists(projectConfigPath))
            {
                var projectConfig = await JsonSerializer.DeserializeAsync<ProjectConfiguration>(
                    File.OpenRead(projectConfigPath), cancellationToken: cancellationToken);
                config.ApplyProjectConfiguration(projectConfig);
                
                // Configure SK plugins and tools based on project settings
                await ConfigureProjectSpecificTools(kernelBuilder, projectConfig);
            }
            {
                var projectConfig = await JsonSerializer.DeserializeAsync<ProjectConfiguration>(
                    File.OpenRead(projectConfigPath), cancellationToken: cancellationToken);
                config.ApplyProjectConfiguration(projectConfig);
            }
        }
        
        // 2. Override with frontmatter model (with provider/model parsing)
        if (!string.IsNullOrEmpty(frontmatterModel))
        {
            // Parse provider/model format from workflow frontmatter
            if (frontmatterModel.Contains('/'))
            {
                var parts = frontmatterModel.Split('/', 2);
                config.Provider = parts[0];
                config.Model = parts[1];
            }
            else
            {
                // Model only - use configured provider
                config.Model = frontmatterModel;
            }
        }
        
        // 1. Override with CLI provider (highest priority)
        if (!string.IsNullOrEmpty(cliProvider))
        {
            config.Provider = cliProvider;
        }
        
        // Default to GitHub Models if no provider specified
        config.Provider ??= "github";
        config.Model ??= "gpt-4o";
        
        return config;
    }
}
```

### Configuration Schema

**Core Configuration Classes:**
```csharp
public class PromptConfiguration
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    public bool Verbose { get; set; }
    public int TimeoutSeconds { get; set; } = 300;
    public Dictionary<string, object> ToolConfiguration { get; set; } = new();
    
    public T? GetToolConfiguration<T>(string toolName) where T : class
    {
        if (ToolConfiguration.TryGetValue(toolName, out var config))
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(config));
        }
        return null;
    }
    
    public void ApplyGlobalConfiguration(GlobalConfiguration global)
    {
        Provider ??= global.DefaultProvider;
        Model ??= global.DefaultModel;
        Verbose = global.Verbose;
        TimeoutSeconds = global.Timeout;
        
        foreach (var provider in global.Providers)
        {
            Providers[provider.Key] = provider.Value;
        }
        
        foreach (var tool in global.ToolConfiguration)
        {
            ToolConfiguration[tool.Key] = tool.Value;
        }
    }
    
    public void ApplyProjectConfiguration(ProjectConfiguration project)
    {
        Provider ??= project.DefaultProvider;
        Model ??= project.DefaultModel;
        
        foreach (var provider in project.Providers)
        {
            Providers[provider.Key] = provider.Value;
        }
        
        foreach (var tool in project.ToolConfiguration)
        {
            ToolConfiguration[tool.Key] = tool.Value;
        }
    }
}

public class GlobalConfiguration
{
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    public string? DefaultProvider { get; set; }
    public string? DefaultModel { get; set; }
    public bool Verbose { get; set; }
    public int Timeout { get; set; } = 300;
    public Dictionary<string, object> ToolConfiguration { get; set; } = new();
}

public class ProjectConfiguration
{
    public string? DefaultProvider { get; set; }
    public string? DefaultModel { get; set; }
    public Dictionary<string, ProviderConfiguration> Providers { get; set; } = new();
    public Dictionary<string, object> ToolConfiguration { get; set; } = new();
    public string? WorkflowTemplates { get; set; }
    public string? McpServers { get; set; }
}

public class ProviderConfiguration
{
    public string? Endpoint { get; set; }
    public string? Token { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiVersion { get; set; }
    public string? Provider { get; set; } // For local provider type (ollama, etc.)
}
```

### Workflow Frontmatter Model Parsing

When a workflow file specifies a `model` property in its YAML frontmatter, the configuration system supports two formats:

1. **Provider/Model Format**: `model: "provider/model"` - Specifies both provider and model
2. **Model Only Format**: `model: "model"` - Uses the configured default provider

**Parsing Logic:**
```csharp
private static (string? provider, string model) ParseModelSpecification(string modelSpec)
{
    if (modelSpec.Contains('/'))
    {
        var parts = modelSpec.Split('/', 2);
        return (parts[0], parts[1]);
    }
    return (null, modelSpec);
}
```

**Examples:**
- `model: "github/gpt-4o"` → Provider: "github", Model: "gpt-4o"
- `model: "openai/gpt-4"` → Provider: "openai", Model: "gpt-4" 
- `model: "gpt-4o"` → Provider: null (uses configured default), Model: "gpt-4o"

**Precedence Behavior:**
- Provider/model format overrides both provider and model from lower-precedence sources
- Model-only format overrides model but preserves configured provider
- CLI arguments (`--provider`, `--model`) still take highest precedence

### AI Provider Integration

**Semantic Kernel Integration with Configuration:**
```csharp
public static class KernelBuilderExtensions
{
    public static KernelBuilder AddConfiguredAIProvider(this KernelBuilder builder, PromptConfiguration config)
    {
        // Use the resolved configuration to set up AI providers
        switch (config.Provider?.ToLowerInvariant())
        {
            case "github":
                var githubConfig = config.Providers["github"];
                builder.AddOpenAIChatCompletion(
                    config.Model,
                    githubConfig.Endpoint,
                    githubConfig.Token);
                break;
                
            case "openai":
                var openaiConfig = config.Providers["openai"];
                builder.AddOpenAIChatCompletion(
                    config.Model,
                    openaiConfig.ApiKey);
                break;
                
            case "azure":
                var azureConfig = config.Providers["azure"];
                builder.AddAzureOpenAIChatCompletion(
                    config.Model,
                    azureConfig.Endpoint,
                    azureConfig.ApiKey,
                    apiVersion: azureConfig.ApiVersion);
                break;
                
            case "local":
                var localConfig = config.Providers["local"];
                builder.AddOpenAIChatCompletion(
                    config.Model,
                    localConfig.Endpoint);
                break;
                
            default:
                throw new InvalidOperationException($"Unsupported AI provider: {config.Provider}");
        }
        
        return builder;
    }
}
```

## Configuration Files

### Global Configuration (`~/.dotnet-prompt/config.json`)
```json
{
  "providers": {
    "github": {
      "endpoint": "https://models.inference.ai.azure.com",
      "token": "github_pat_xxxxx"
    },
    "openai": {
      "api_key": "sk-xxxxx"
    },
    "azure": {
      "endpoint": "https://myinstance.openai.azure.com",
      "api_key": "xxxxx",
      "api_version": "2024-02-01"
    },
    "local": {
      "endpoint": "http://localhost:11434",
      "provider": "ollama"
    }
  },
  "default_provider": "github",
  "default_model": "gpt-4o",
  "verbose": false,
  "timeout": 300,
  "tool_configuration": {
    "project_analysis": {
      "include_private_members": false,
      "analyze_dependencies": true,
      "max_file_size_bytes": 1048576,
      "excluded_directories": ["bin", "obj", ".git", "node_modules"],
      "cache_expiration_minutes": 30
    },
    "build_test": {
      "default_configuration": "Debug",
      "verbose_logging": false,
      "timeout_seconds": 300,
      "parallel_execution": true,
      "excluded_test_categories": []
    }
  }
}
```

### Project Configuration (`.dotnet-prompt/config.json`)
```json
{
  "default_provider": "azure",
  "providers": {
    "azure": {
      "endpoint": "https://mycompany.openai.azure.com"
    }
  },
  "workflow_templates": "./templates",
  "mcp_servers": "./mcp.json",
  "tool_configuration": {
    "project_analysis": {
      "excluded_directories": ["bin", "obj", ".git", "node_modules", "packages"]
    }
  }
}
```

## Open Questions for Future Implementation

### Configuration Enhancement
- Should we provide utility methods for common validation patterns (path validation, etc.)?
- How should we handle configuration change notifications for runtime updates?
- Should we standardize tool-specific telemetry and metrics collection?

## Environment Variable Support

Environment variables that can override configuration settings:
- `DOTNET_PROMPT_PROVIDER`: Default AI provider
- `DOTNET_PROMPT_VERBOSE`: Enable verbose logging
- Additional environment variables to be defined

## Configuration Merging Logic

How configuration values are resolved and merged across the hierarchy.

## Clarifying Questions

### 1. Configuration Schema
- What are all the configuration options available?
- How should nested configuration objects be merged?
- What are the valid values for each configuration option?
- How should configuration validation work?
- What configuration options can be overridden at runtime?

### 2. Provider Configuration
- How should each AI provider be configured?
- What authentication methods are supported for each provider?
- How should provider-specific settings be handled?
- Should there be provider auto-discovery mechanisms?
- How should invalid provider configurations be handled?

### 3. Security and Secrets Management
- How should API keys and tokens be stored securely?
- Should sensitive configuration be encrypted?
- How should the tool handle missing credentials?
- What is the strategy for credential rotation?
- Should there be integration with system credential stores?

### 4. Environment Variable Support
- Which configuration options can be set via environment variables?
- What is the naming convention for environment variables?
- How do environment variables interact with the configuration hierarchy?
- Should there be support for environment-specific configuration files?

### 5. Configuration Validation
- What validation rules should be applied to configuration?
- How should validation errors be reported to users?
- Should there be a configuration validation command?
- How should deprecated configuration options be handled?
- What happens when required configuration is missing?

### 6. Project-Specific Configuration
- What configuration should be project-specific vs global?
- How should team-shared configuration work?
- Should project configuration be version controlled?
- How should configuration inheritance work in monorepos?
- What configuration should be discoverable vs explicit?

### 7. Configuration Management Commands
- Should there be commands to manage configuration?
- How should users view current effective configuration?
- Should there be configuration import/export functionality?
- How should configuration updates be handled?
- Should there be configuration backup/restore capabilities?

### 8. Default Values and Fallbacks
- What are the default values for all configuration options?
- How should the tool behave with minimal configuration?
- What fallback strategies should be implemented?
- How should missing optional configuration be handled?

### 9. Configuration File Discovery
- How should the tool discover configuration files?
- What is the search strategy for project configuration?
- Should there be support for configuration file templates?
- How should configuration file conflicts be resolved?

### 10. Runtime Configuration Changes
- Can configuration be changed during workflow execution?
- How should configuration changes affect running workflows?
- Should there be hot-reload support for configuration?
- How should configuration caching work?

## Next Steps

1. Define the complete configuration schema
2. Implement configuration merging and validation logic
3. Design secure credential storage mechanisms
4. Create configuration management commands
5. Document configuration best practices
6. Implement configuration discovery and loading
