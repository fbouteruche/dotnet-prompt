# Built-in Tools Framework Specification

## Overview

This document defines the overall framework and mechanisms for built-in tools in the dotnet-prompt system. Individual tool specifications are maintained in separate documents.

## Status
✅ **COMPLETE** - Framework patterns aligned with Semantic Kernel built-in capabilities

## Built-in Tools Framework

### Purpose
Provide a standardized framework for implementing, registering, and executing built-in tools within the dotnet-prompt workflow engine using Semantic Kernel.

### Architecture Overview

#### Universal Tool Integration with Semantic Kernel
All tools (built-in and MCP) are exposed through Semantic Kernel plugins with standardized patterns for:
- Function registration and discovery
- Parameter validation and type conversion
- Error handling and reporting
- Progress tracking and cancellation
- Caching and performance optimization
- Unified response format across tool sources

#### Architecture Benefits
- **Tool Source Abstraction**: AI workflows interact with all tools through the same Semantic Kernel interface
- **MCP Integration**: External MCP servers can provide tools that appear native to the workflow engine
- **Consistent Developer Experience**: Same patterns for built-in tools and external MCP tools
- **Future Extensibility**: Easy to add new tool sources without changing workflow execution logic

## Framework Components

### 1. Tool Registration and Discovery
- ✅ **Startup Registration**: Built-in tools are compiled into the CLI and registered at startup
- ✅ **Lazy Loading**: Tool implementations are instantiated only when first invoked
- ✅ **Version Coupling**: Built-in tool versions are tied to the dotnet-prompt CLI version
- ✅ **No Runtime Discovery**: Tools are not discovered at runtime - they are compiled into the CLI

#### ✅ Plugin Registration with Semantic Kernel (Enhanced)
```csharp
// Built-in tools are registered at CLI startup with comprehensive SK integration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuiltInTools(this IServiceCollection services)
    {
        // Register tool plugins as SK functions with full annotations
        services.AddScoped<ProjectAnalysisPlugin>();
        services.AddScoped<BuildTestPlugin>();
        services.AddScoped<FileSystemPlugin>();
        
        // Add SK-specific services for tool enhancement
        services.AddScoped<IToolValidationFilter, BuiltInToolValidationFilter>();
        services.AddScoped<IToolPerformanceFilter, BuiltInToolPerformanceFilter>();
        services.AddScoped<IToolCacheManager, SkVectorStoreToolCache>();
        
        return services;
    }
    
    public static KernelBuilder AddBuiltInToolPlugins(this KernelBuilder builder)
    {
        // Register SK plugins with comprehensive metadata
        builder.Plugins.AddFromType<ProjectAnalysisPlugin>("ProjectAnalysis");
        builder.Plugins.AddFromType<BuildTestPlugin>("BuildTest");
        builder.Plugins.AddFromType<FileSystemPlugin>("FileSystem");
        
        // Add SK filters for cross-cutting concerns
        builder.Services.AddSingleton<IFunctionInvocationFilter, ToolInvocationFilter>();
        builder.Services.AddSingleton<IPromptRenderFilter, ToolSecurityFilter>();
        
        return builder;
    }
}
        
        return services;
    }
    
    public static Kernel AddBuiltInTools(this Kernel kernel, IServiceProvider services)
    {
        // Register all built-in tool plugins with the kernel at startup
        // Tools are lazy-loaded when first invoked
        kernel.Plugins.AddFromObject(services.GetRequiredService<ProjectAnalysisPlugin>(), "project");
        kernel.Plugins.AddFromObject(services.GetRequiredService<BuildTestPlugin>(), "build");
        kernel.Plugins.AddFromObject(services.GetRequiredService<FileSystemPlugin>(), "fs");
        
        return kernel;
    }
}
```

#### Tool Function Attributes
```csharp
// Standard attributes for Semantic Kernel function registration
[KernelFunction("function_name")]
[Description("Clear description of what the function does")]
public async Task<ToolResponse<ResultType>> FunctionNameAsync(
    [Description("Parameter description")] ParameterType parameter,
    CancellationToken cancellationToken = default
)
```

### 2. Common Response Format
- ✅ **MCP Integration Confirmed**: Semantic Kernel can add plugins from MCP Servers (both local via stdio and remote via SSE over HTTPS)
- ✅ **Unified Tool Interface**: Both built-in tools and MCP tools can be exposed as Semantic Kernel plugins
- ✅ **Standardized Response Wrapper**: Implement a common response format that works for both built-in and MCP tools
- ✅ **Tool Source Transparency**: AI workflows don't need to know whether tools are built-in or from MCP servers

#### ✅ Validated Unified Response Format
```csharp
public class ToolResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public ToolSource Source { get; set; } // BuiltIn, MCP, External
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ToolSource
{
    BuiltIn,    // Built-in dotnet-prompt tools
    MCP,        // Tools from MCP servers
    External    // Other external tools/plugins
}
```

#### ✅ Validated MCP Tool Integration Pattern
```csharp
// MCP tools are registered as Semantic Kernel plugins alongside built-in tools
public static Kernel AddAllTools(this Kernel kernel, IServiceProvider services, IMcpClientManager mcpManager)
{
    // Add built-in tools
    kernel.AddBuiltInTools(services);
    
    // Add MCP server tools as plugins
    foreach (var mcpServer in mcpManager.GetServers())
    {
        kernel.Plugins.AddFromMcp(mcpServer);
    }
    
    return kernel;
}
```

### 3. Parameter Validation Framework
- ✅ **Semantic Kernel Handles Validation**: SK automatically performs parameter validation and type conversion using `TypeConverter`
- ✅ **Built-in Type Conversion**: Arguments from `KernelArguments` are automatically converted to method parameter types
- ✅ **Required Parameter Validation**: If no argument is provided and no default value exists, SK fails the invocation
- ✅ **Description-Based Validation**: Use `[Description]` attributes to guide the AI in providing correct parameter values

#### ✅ Semantic Kernel's Built-in Parameter Handling

**Automatic Parameter Resolution:**
```csharp
[KernelFunction("analyze_project")]
[Description("Analyzes a .NET project structure and dependencies")]
public async Task<ToolResponse<ProjectAnalysisResult>> AnalyzeProjectAsync(
    [Description("Path to the project file or directory - must be a valid path")] 
    string projectPath,  // SK validates non-null if no default provided
    
    [Description("Include dependency analysis in the result")] 
    bool includeDependencies = true,  // Default value prevents validation failure
    
    CancellationToken cancellationToken = default)
{
    // SK has already:
    // 1. Validated projectPath is provided (no default value)
    // 2. Converted arguments to correct types using TypeConverter
    // 3. Applied default values where specified
    
    // Only need business logic validation here
    if (!Path.IsPathRooted(projectPath))
    {
        return new ToolResponse<ProjectAnalysisResult>
        {
            Success = false,
            ErrorCode = "INVALID_PATH",
            ErrorMessage = "Project path must be absolute",
            Source = ToolSource.BuiltIn
        };
    }
    
    // Continue with implementation...
}
```

### 4. Error Handling Standards
- ✅ **Semantic Kernel Exception Hierarchy**: SK provides `KernelException` base class and specialized exceptions
- ✅ **FunctionResult Pattern**: SK uses `FunctionResult` to wrap both success and failure outcomes
- ✅ **OpenTelemetry Integration**: SK exceptions include telemetry data in `Exception.Data` property
- ✅ **Cancellation Support**: SK provides `KernelFunctionCanceledException` for operation cancellation

#### ✅ Semantic Kernel's Built-in Error Handling

**Function Result Pattern:**
```csharp
// SK automatically wraps function results in FunctionResult
[KernelFunction("analyze_project")]
[Description("Analyzes a .NET project structure and dependencies")]
public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
    [Description("Path to the project file or directory")] string projectPath,
    CancellationToken cancellationToken = default)
{
    // Let exceptions bubble up - SK will catch and wrap them
    if (!Directory.Exists(projectPath))
    {
        throw new DirectoryNotFoundException($"Project path not found: {projectPath}");
    }
    
    // For business logic errors, use domain exceptions
    if (!HasValidProjectFiles(projectPath))
    {
        throw new InvalidOperationException("No valid .NET project files found in directory");
    }
    
    // Return success case directly - SK wraps in FunctionResult
    return await AnalyzeProjectInternal(projectPath, cancellationToken);
}
```

### 5. Performance and Caching
- ✅ **Microsoft.Extensions.Caching Integration**: SK leverages standard .NET caching with IMemoryCache and IDistributedCache
- ✅ **Vector Store Abstractions**: SK provides Vector Store connectors for efficient data retrieval and caching
- ✅ **Dependency Injection Ready**: All caching services integrate with .NET DI container
- ✅ **Lightweight Kernel Pattern**: SK kernel is designed as a lightweight, disposable container

#### ✅ Semantic Kernel's Built-in Performance Patterns

**Memory Caching Integration:**
```csharp
// SK integrates with standard .NET caching patterns
var builder = Kernel.CreateBuilder();

// Add standard .NET caching services
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // Or Redis, SQL Server, etc.

// Built-in tools can leverage these through DI
builder.Services.AddScoped<ProjectAnalysisPlugin>();
var kernel = builder.Build();
```

**Vector Store for Data Caching:**
```csharp
// SK provides vector stores for semantic caching and retrieval
public class ProjectAnalysisPlugin
{
    private readonly IVectorStore _vectorStore;
    private readonly IMemoryCache _cache;
    
    public ProjectAnalysisPlugin(IVectorStore vectorStore, IMemoryCache cache)
    {
        _vectorStore = vectorStore;
        _cache = cache;
    }
    
    [KernelFunction("analyze_project")]
    public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        // Use standard .NET caching patterns
        var cacheKey = $"project_analysis_{Path.GetFileName(projectPath)}";
        
        if (_cache.TryGetValue(cacheKey, out ProjectAnalysisResult cachedResult))
        {
            return cachedResult;
        }
        
        var result = await PerformAnalysis(projectPath);
        
        // Cache with expiration
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
        
        return result;
    }
}
```

#### Performance Best Practices
- **Transient Kernels**: Create new kernel instances for each operation (kernels are lightweight)
- **Singleton Services**: Cache expensive resources (vector stores, HTTP clients) as singletons
- **Standard Caching**: Use `IMemoryCache` for local caching, `IDistributedCache` for shared scenarios
- **Vector Stores**: Leverage SK's vector store abstractions for semantic data caching

### 6. Security and Trust Model
- ✅ **Azure Identity Integration**: SK natively supports `DefaultAzureCredential` and Azure RBAC
- ✅ **Dependency Injection Security**: Secure service injection patterns throughout SK architecture
- ✅ **OpenTelemetry Integration**: Built-in telemetry and audit logging capabilities
- ✅ **Standard .NET Security**: Leverages .NET security patterns and best practices

#### ✅ Semantic Kernel's Built-in Security Patterns

**Azure Identity Integration:**
```csharp
// SK provides native Azure Identity support
var builder = Kernel.CreateBuilder();

// Use Azure Identity for secure authentication
builder.AddAzureOpenAIChatCompletion(
    "your-model",
    "your-endpoint",
    new DefaultAzureCredential()); // Automatic credential discovery

// Built-in tools inherit the same security context
builder.Services.AddScoped<ProjectAnalysisPlugin>();
var kernel = builder.Build();
```

#### Security Principles
- **Principle of Least Privilege**: Tools only access resources they need
- **Audit Logging**: All operations logged through standard .NET logging
- **Secure Defaults**: Use `DefaultAzureCredential` for automatic credential management
- **Input Validation**: Leverage SK's parameter validation for security

### 7. Configuration Management
- ✅ **Configuration System Integration**: Built-in tools integrate with the dotnet-prompt configuration hierarchy
- ✅ **Tool-Specific Options**: Built-in tools can define and access their own configuration sections
- ✅ **Provider Inheritance**: Built-in tools automatically use the same AI provider as workflow execution
- ✅ **Runtime Configuration Access**: Tools receive resolved configuration through dependency injection

#### ✅ Built-in Tools Configuration Integration

**Tool Configuration Access Pattern:**
```csharp
public class ProjectAnalysisPlugin
{
    private readonly PromptConfiguration _config;
    private readonly ILogger<ProjectAnalysisPlugin> _logger;
    
    public ProjectAnalysisPlugin(PromptConfiguration config, ILogger<ProjectAnalysisPlugin> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    [KernelFunction("analyze_project")]
    public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        // Built-in tools can access their specific configuration
        var analysisOptions = _config.GetToolConfiguration<ProjectAnalysisOptions>("project_analysis");
        
        // Use configuration for tool behavior
        var includePrivateMembers = analysisOptions?.IncludePrivateMembers ?? false;
        var maxFileSize = analysisOptions?.MaxFileSizeBytes ?? (1024 * 1024); // 1MB default
        var excludedDirs = analysisOptions?.ExcludedDirectories ?? new[] { "bin", "obj", ".git" };
        
        _logger.LogInformation("Analyzing project with config: Provider={Provider}, Model={Model}", 
                              _config.Provider, _config.Model);
        
        // Apply configuration to analysis logic
        return await AnalyzeProjectWithOptions(projectPath, analysisOptions);
    }
}
```

**Tool-Specific Configuration Options:**
```csharp
// Built-in tools define their own configuration option classes
public class ProjectAnalysisOptions
{
    public bool IncludePrivateMembers { get; set; } = false;
    public bool AnalyzeDependencies { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 1024 * 1024; // 1MB
    public string[] ExcludedDirectories { get; set; } = { "bin", "obj", ".git", "node_modules" };
    public int CacheExpirationMinutes { get; set; } = 30;
}

public class BuildTestOptions
{
    public string DefaultConfiguration { get; set; } = "Debug";
    public bool VerboseLogging { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes
    public bool ParallelExecution { get; set; } = true;
    public string[] ExcludedTestCategories { get; set; } = Array.Empty<string>();
}

public class FileSystemOptions
{
    public string[] AllowedDirectories { get; set; } = Array.Empty<string>();
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public bool EnableAuditLogging { get; set; } = true;
}
```

**Service Registration with Configuration:**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBuiltInToolsWithConfiguration(
        this IServiceCollection services, 
        PromptConfiguration config)
    {
        // Register the resolved configuration as a singleton
        services.AddSingleton(config);
        
        // Register tool plugins - they will receive configuration via DI
        services.AddScoped<ProjectAnalysisPlugin>();
        services.AddScoped<BuildTestPlugin>();
        services.AddScoped<FileSystemPlugin>();
        
        return services;
    }
}
```

#### Configuration Integration Principles
- **Hierarchy Compliance**: Tools respect the 4-level configuration hierarchy (CLI → frontmatter → project → global)
- **Provider Consistency**: Built-in tools automatically use the same AI provider as workflow execution
- **Tool Isolation**: Each built-in tool can have its own configuration section in `tool_configuration`
- **Default Fallbacks**: Tools provide sensible defaults when configuration is not specified
- **Runtime Resolution**: Configuration is resolved once at workflow startup and injected into tools

*Note: For complete configuration system implementation details, see [configuration-system-specification.md](./configuration-system-specification.md)*

### 8. Integration with Workflow Engine
- ✅ **Function Calling Integration**: SK's native function calling mechanism for tool invocation
- ✅ **Planner Integration**: SK planners can orchestrate built-in tools with LLM reasoning
- ✅ **Plugin Architecture**: Built-in tools implemented as SK plugins for seamless integration
- ✅ **Conversation Management**: SK's ChatHistory and conversation patterns

#### ✅ Semantic Kernel's Built-in Workflow Patterns

**Function Calling Workflow:**
```csharp
// Built-in tools integrate as SK functions for automatic orchestration
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4", endpoint, credential);

// Register built-in tools as plugins
builder.Plugins.AddFromType<ProjectAnalysisPlugin>();
builder.Plugins.AddFromType<BuildTestPlugin>();
builder.Plugins.AddFromType<FileSystemPlugin>();

var kernel = builder.Build();

// SK automatically handles function calling in workflows
var workflow = @"
# Project Setup Workflow

Analyze the current project structure and run the build to identify any issues.

## Steps:
1. Use project analysis to understand the current state
2. If issues are found, suggest fixes
3. Run build and tests to validate
";

// SK orchestrates tool calls automatically
var result = await kernel.InvokePromptAsync(workflow);
```

#### Workflow Integration Principles
- **Tool Discovery**: SK automatically discovers and registers built-in tools as functions
- **Automatic Orchestration**: LLM decides when and how to use tools based on conversation context
- **State Management**: SK manages conversation state and tool call history
- **Error Recovery**: Built-in error handling and retry logic through SK patterns

## Framework Implementation Summary

This framework specification leverages Semantic Kernel's built-in capabilities to provide a robust, maintainable foundation for built-in tools. Key advantages of this SK-aligned approach:

1. **Zero Custom Infrastructure**: All patterns use SK's proven implementations
2. **Automatic Tool Discovery**: SK handles registration and function calling automatically  
3. **Standard .NET Patterns**: Configuration, DI, caching all use familiar .NET conventions
4. **Security by Default**: Azure Identity and RBAC built into the framework
5. **Performance Optimized**: Vector stores and caching strategies proven at scale
6. **Workflow Ready**: Native integration with SK planners and conversation management

The built-in tools (Project Analysis, Build & Test, File System) can now be implemented as SK plugins following these established patterns, ensuring consistency, maintainability, and seamless integration with the broader dotnet-prompt workflow engine.

## Individual Tool Specifications

The following built-in tools have dedicated specification documents:

| Tool | Document | Status |
|------|----------|--------|
| Project Analysis Tool | [project-analysis-tool.md](./project-analysis-tool.md) | 🚧 Draft |
| Build & Test Tool | [build-test-tool.md](./build-test-tool.md) | 🚧 Draft |
| File System Tool | [file-system-tool.md](./file-system-tool.md) | 🚧 Draft |

## Open Questions for Future Consideration

*Note: The following questions are specific to built-in tools implementation and framework enhancement.*

### Built-in Tool Development
- Should we provide a template or scaffold for creating new built-in tools?
- How should we handle versioning and compatibility of built-in tools across CLI versions?
- Should there be a plugin development kit for built-in tools?
- What testing frameworks and utilities should be provided for built-in tool development?

### Tool Capabilities and Scope
- Should the File System tool be included in the MVP, or added post-MVP?
- What additional built-in tools would provide the most value for .NET developers?
- Should built-in tools have read-only modes for safety in certain environments?
- How granular should built-in tool functions be (single purpose vs. multi-purpose tools)?

### Integration with .NET Ecosystem
- Should built-in tools integrate with MSBuild targets and properties beyond basic project analysis?
- How should built-in tools handle different .NET SDK versions and target frameworks?
- Should there be integration with NuGet package management operations?
- How should built-in tools handle solution-level operations versus project-level operations?

### Tool Result Formats and Interoperability
- Should built-in tools support multiple output formats (JSON, XML, custom) for different use cases?
- How should built-in tools handle large result sets that might exceed AI context windows?
- Should there be result streaming capabilities for long-running tool operations?
- How should built-in tools coordinate when multiple tools need to operate on the same files?
