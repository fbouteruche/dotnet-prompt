# Built-in Tools Framework Specification

## Overview

This document defines the overall framework and mechanisms for built-in tools in the dotnet-prompt system. Individual tool specifications are maintained in separate documents.

## Status
✅ **ALIGNED** - Framework specification updated to reflect current implementation patterns and architecture

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

#### ✅ Plugin Registration with Semantic Kernel (Current Implementation)
```csharp
// Built-in tools are registered through AddAiProviderServices method
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiProviderServices(this IServiceCollection services)
    {
        // Use the new SK orchestrator instead of the old approach
        services.AddSemanticKernelOrchestrator();
        
        // Add comprehensive error handling and logging
        services.AddSemanticKernelErrorHandling();
        
        // Register essential SK plugins as transient services
        services.AddTransient<FileSystemPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        services.AddTransient<SubWorkflowPlugin>();
        // NOTE: WorkflowExecutorPlugin is intentionally excluded - replaced by SK Handlebars templating
        
        return services;
    }
    
    public static IServiceCollection AddSemanticKernelOrchestrator(this IServiceCollection services)
    {
        // Register Handlebars factory for SK native templating
        services.AddSingleton<IPromptTemplateFactory, HandlebarsPromptTemplateFactory>();
        
        // Register orchestrator (replaces WorkflowExecutorPlugin usage)
        services.AddScoped<IWorkflowOrchestrator, SemanticKernelOrchestrator>();
        
        // Register kernel factory with MCP support
        services.AddSingleton<IKernelFactory, KernelFactory>();
        
        // Add MCP integration services
        services.AddMcpIntegrationServices();
        
        return services;
    }
}

// Built-in tools are registered at kernel creation time through KernelFactory
public class KernelFactory : IKernelFactory
{
    public async Task<Kernel> CreateKernelWithWorkflowAsync(...)
    {
        // ... kernel builder configuration ...
        
        // Register built-in plugins
        var defaultPluginTypes = pluginTypes ?? new[]
        {
            typeof(Plugins.FileSystemPlugin),
            typeof(Plugins.ProjectAnalysisPlugin)
            // SubWorkflowPlugin temporarily disabled in some scenarios
        };

        foreach (var pluginType in defaultPluginTypes)
        {
            var plugin = _serviceProvider.GetRequiredService(pluginType);
            var pluginName = pluginType.Name.Replace("Plugin", "");
            kernel.Plugins.AddFromObject(plugin, pluginName);
        }
        
        return kernel;
    }
}
```

#### Tool Function Attributes
```csharp
// Standard attributes for Semantic Kernel function registration (Current Implementation)
[KernelFunction("function_name")]
[Description("Clear description of what the function does")]
public async Task<string> FunctionNameAsync(
    [Description("Parameter description")] ParameterType parameter,
    CancellationToken cancellationToken = default
)
// NOTE: Current implementation returns JSON string rather than ToolResponse<T>
// Functions return serialized JSON for AI consumption
```

### 2. Common Response Format
- ✅ **MCP Integration Confirmed**: Semantic Kernel can add plugins from MCP Servers (both local via stdio and remote via SSE over HTTPS)
- ✅ **Unified Tool Interface**: Both built-in tools and MCP tools can be exposed as Semantic Kernel plugins
- ✅ **Standardized Response Wrapper**: Implement a common response format that works for both built-in and MCP tools
- ✅ **Tool Source Transparency**: AI workflows don't need to know whether tools are built-in or from MCP servers

#### ✅ Current Response Format Implementation
```csharp
// Built-in tools currently return JSON strings for AI consumption
// FileOperationResult is used for file system operations
public class FileOperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public ToolSource Source { get; set; } = ToolSource.BuiltIn;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ToolSource
{
    BuiltIn,    // Built-in dotnet-prompt tools
    MCP,        // Tools from MCP servers
    External    // Other external tools/plugins
}
```

#### ✅ MCP Tool Integration Pattern (Current Implementation)
```csharp
// MCP tools are registered using official MCP SDK
public static async Task<Kernel> AddMcpServersFromWorkflowAsync(
    this Kernel kernel,
    DotpromptWorkflow workflow,
    IServiceProvider serviceProvider)
{
    // ... configuration parsing ...
    
    foreach (var mcpConfig in mcpConfigs)
    {
        // 1. Create official SDK transport
        var transport = TransportConfigurationMapper.CreateTransport(mcpConfig);
        
        // 2. Use official MCP client factory
        var mcpClient = await McpClientFactory.CreateAsync(transport);
        
        // 3. Discover tools - returns McpClientTool instances (AIFunction)
        var tools = await mcpClient.ListToolsAsync();
        
        // 4. Convert to KernelFunction using AsKernelFunction() extension
        var kernelFunctions = tools.Select(aiFunction => aiFunction.AsKernelFunction());
        
        // 5. Add as plugin to Semantic Kernel
        kernel.Plugins.AddFromFunctions(mcpConfig.Name, kernelFunctions);
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

**Automatic Parameter Resolution (Current Implementation):**
```csharp
[KernelFunction("analyze_project")]
[Description("Analyzes a .NET project structure and dependencies")]
public async Task<string> AnalyzeProjectAsync(
    [Description("The absolute path to the project file (.csproj/.fsproj/.vbproj)")] 
    string projectPath,  // SK validates non-null if no default provided
    
    [Description("Include dependency analysis (default: true)")] 
    bool includeDependencies = true,  // Default value prevents validation failure
    
    CancellationToken cancellationToken = default)
{
    // SK has already:
    // 1. Validated projectPath is provided (no default value)
    // 2. Converted arguments to correct types using TypeConverter
    // 3. Applied default values where specified
    
    // Business logic validation
    var validatedPath = ValidateProjectPath(projectPath);
    
    if (!File.Exists(validatedPath))
    {
        throw new FileNotFoundException($"Project file not found: {validatedPath}");
    }
    
    // Return JSON string for AI consumption
    var analysis = await AnalyzeProjectContent(validatedPath, ...);
    return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
}
```

### 4. Error Handling Standards
- ✅ **Semantic Kernel Exception Hierarchy**: SK provides `KernelException` base class and specialized exceptions
- ✅ **FunctionResult Pattern**: SK uses `FunctionResult` to wrap both success and failure outcomes
- ✅ **OpenTelemetry Integration**: SK exceptions include telemetry data in `Exception.Data` property
- ✅ **Cancellation Support**: SK provides `KernelFunctionCanceledException` for operation cancellation

#### ✅ Semantic Kernel's Built-in Error Handling

**Function Result Pattern (Current Implementation):**
```csharp
// SK automatically wraps function results in FunctionResult
[KernelFunction("analyze_project")]
[Description("Analyzes a .NET project structure and dependencies")]
public async Task<string> AnalyzeProjectAsync(
    [Description("The absolute path to the project file")] string projectPath,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Let exceptions bubble up - SK will catch and wrap them
        var validatedPath = ValidateProjectPath(projectPath);
        
        if (!File.Exists(validatedPath))
        {
            throw new FileNotFoundException($"Project file not found: {validatedPath}");
        }
        
        // For business logic errors, use domain exceptions
        if (!HasValidProjectFiles(validatedPath))
        {
            throw new InvalidOperationException("No valid .NET project files found in directory");
        }
        
        // Return JSON string - SK wraps in FunctionResult
        var analysis = await AnalyzeProjectInternal(validatedPath, cancellationToken);
        return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error analyzing project {ProjectPath} via SK function", projectPath);
        throw new KernelException($"Project analysis failed: {ex.Message}", ex);
    }
}
```

### 5. Performance and Caching
- ✅ **Microsoft.Extensions.Caching Integration**: SK leverages standard .NET caching with IMemoryCache and IDistributedCache
- ✅ **Vector Store Abstractions**: SK provides Vector Store connectors for efficient data retrieval and caching
- ✅ **Dependency Injection Ready**: All caching services integrate with .NET DI container
- ✅ **Lightweight Kernel Pattern**: SK kernel is designed as a lightweight, disposable container

#### ✅ Semantic Kernel's Built-in Performance Patterns

**Memory Caching Integration (Current Implementation):**
```csharp
// SK integrates with standard .NET caching patterns
var builder = Kernel.CreateBuilder();

// Add standard .NET caching services (configured in ServiceCollectionExtensions)
// services.AddMemoryCache(); // Not explicitly added in current implementation
// services.AddDistributedMemoryCache(); // Not explicitly added in current implementation

// Built-in tools are registered as transient services
services.AddTransient<ProjectAnalysisPlugin>();
var kernel = builder.Build();
```

**Vector Store for Data Caching (Future Enhancement):**
```csharp
// SK provides vector stores for semantic caching and retrieval
// Note: Not currently implemented but available for future enhancement
public class ProjectAnalysisPlugin
{
    private readonly ILogger<ProjectAnalysisPlugin> _logger;
    
    public ProjectAnalysisPlugin(ILogger<ProjectAnalysisPlugin> logger)
    {
        _logger = logger;
    }
    
    [KernelFunction("analyze_project")]
    public async Task<string> AnalyzeProjectAsync(string projectPath)
    {
        // Current implementation returns JSON directly without caching
        var analysis = await PerformAnalysis(projectPath);
        return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

#### Performance Best Practices (Current Implementation)
- **Transient Plugins**: Built-in tool plugins are registered as transient services
- **Singleton Services**: Infrastructure services (KernelFactory, Configuration) are singletons
- **Standard Logging**: Uses `ILogger<T>` for diagnostics and error tracking
- **JSON Serialization**: Direct JSON string returns for efficient AI consumption
- **Future Enhancement**: Caching and vector store integration planned but not yet implemented

### 6. Security and Trust Model
- ✅ **Azure Identity Integration**: SK natively supports `DefaultAzureCredential` and Azure RBAC
- ✅ **Dependency Injection Security**: Secure service injection patterns throughout SK architecture
- ✅ **OpenTelemetry Integration**: Built-in telemetry and audit logging capabilities
- ✅ **Standard .NET Security**: Leverages .NET security patterns and best practices

#### ✅ Semantic Kernel's Built-in Security Patterns

**Azure Identity Integration (Current Implementation):**
```csharp
// SK provides native Azure Identity support - implemented in KernelFactory
private void ConfigureAzureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
{
    var endpoint = GetConfigValue(config, "Endpoint") 
                ?? _configuration["AI:Azure:Endpoint"]
                ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");

    var apiKey = GetConfigValue(config, "ApiKey") 
              ?? _configuration["AI:Azure:ApiKey"]
              ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
              ?? throw new InvalidOperationException("Azure OpenAI API key not configured");

    var model = GetConfigValue(config, "Model") 
             ?? _configuration["AI:Azure:Model"] 
             ?? "gpt-4o";

    builder.AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
    // Note: DefaultAzureCredential integration available for future enhancement
}
```

#### Security Principles (Current Implementation)
- **Configuration-Based Security**: API keys and endpoints configured through environment variables and configuration files
- **Path Validation**: Built-in tools validate file paths and prevent directory traversal
- **Exception Wrapping**: KernelException used to wrap business logic errors
- **Security Filters**: File system security filter and other security validations through SK filters
- **Future Enhancement**: Azure Identity integration and RBAC planned

### 7. Configuration Management
- ✅ **Configuration System Integration**: Built-in tools integrate with the dotnet-prompt configuration hierarchy
- ✅ **Tool-Specific Options**: Built-in tools can define and access their own configuration sections
- ✅ **Provider Inheritance**: Built-in tools automatically use the same AI provider as workflow execution
- ✅ **Runtime Configuration Access**: Tools receive resolved configuration through dependency injection

#### ✅ Built-in Tools Configuration Integration

**Tool Configuration Access Pattern (Current Implementation):**
```csharp
public class ProjectAnalysisPlugin
{
    private readonly ILogger<ProjectAnalysisPlugin> _logger;
    
    public ProjectAnalysisPlugin(ILogger<ProjectAnalysisPlugin> logger)
    {
        _logger = logger;
    }
    
    [KernelFunction("analyze_project")]
    public async Task<string> AnalyzeProjectAsync(string projectPath)
    {
        // Current implementation uses default configuration
        // Configuration system integration is available but not yet fully implemented
        var includePrivateMembers = false; // Default value
        var maxFileSize = 1024 * 1024; // 1MB default
        var excludedDirs = new[] { "bin", "obj", ".git" };
        
        _logger.LogInformation("Analyzing .NET project via SK function: {ProjectPath}", projectPath);
        
        // Apply configuration to analysis logic
        var analysis = await AnalyzeProjectWithOptions(projectPath, ...);
        return JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
    }
}
```

**Tool-Specific Configuration Options (Future Enhancement):**
```csharp
// Defined in FileSystemOptions model - available for future tool configuration enhancement
public class FileSystemOptions
{
    public string[] AllowedDirectories { get; set; } = Array.Empty<string>();
    public string[] BlockedDirectories { get; set; } = { "bin", "obj", ".git", "node_modules" };
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string[] BlockedExtensions { get; set; } = { ".exe", ".dll", ".so", ".dylib" };
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxFilesPerOperation { get; set; } = 1000;
    public bool RequireConfirmationForDelete { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
}

// Project analysis and build options can be defined similarly for future enhancement
public class ProjectAnalysisOptions
{
    public bool IncludePrivateMembers { get; set; } = false;
    public bool AnalyzeDependencies { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 1024 * 1024; // 1MB
    public string[] ExcludedDirectories { get; set; } = { "bin", "obj", ".git", "node_modules" };
    public int CacheExpirationMinutes { get; set; } = 30;
}
```

**Service Registration with Configuration (Current Implementation):**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Add file system options configuration (currently the only tool with explicit configuration)
        services.Configure<FileSystemOptions>(options =>
        {
            // Default configuration - will be overridden by appsettings.json or CLI parameters
            options.AllowedDirectories = Array.Empty<string>();
            options.BlockedDirectories = new[] { "bin", "obj", ".git", "node_modules" };
            options.AllowedExtensions = Array.Empty<string>();
            options.BlockedExtensions = new[] { ".exe", ".dll", ".so", ".dylib" };
            options.MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
            options.MaxFilesPerOperation = 1000;
            options.RequireConfirmationForDelete = true;
            options.EnableAuditLogging = true;
        });
        
        return services;
    }
}
```

#### Configuration Integration Principles (Current Implementation)
- **Infrastructure Configuration**: AI provider configuration managed through KernelFactory and environment variables
- **Tool Configuration**: FileSystemOptions is currently the only tool with explicit configuration integration
- **Default Behavior**: Built-in tools use sensible defaults when configuration is not specified
- **Service Registration**: Tools registered as transient services through AddAiProviderServices
- **Future Enhancement**: Full tool configuration hierarchy integration planned

*Note: For complete configuration system implementation details, see [configuration-system-specification.md](./configuration-system-specification.md)*

### 8. Integration with Workflow Engine
- ✅ **Function Calling Integration**: SK's native function calling mechanism for tool invocation
- ✅ **Planner Integration**: SK planners can orchestrate built-in tools with LLM reasoning
- ✅ **Plugin Architecture**: Built-in tools implemented as SK plugins for seamless integration
- ✅ **Conversation Management**: SK's ChatHistory and conversation patterns

#### ✅ Semantic Kernel's Built-in Workflow Patterns

**Function Calling Workflow (Current Implementation):**
```csharp
// Built-in tools integrate as SK functions through KernelFactory
public class KernelFactory : IKernelFactory
{
    public async Task<Kernel> CreateKernelWithWorkflowAsync(...)
    {
        var builder = Kernel.CreateBuilder();
        
        // Configure AI services (OpenAI, Azure, GitHub Models, etc.)
        await ConfigureAIServicesAsync(builder, providerName, configuration);
        
        var kernel = builder.Build();

        // Register built-in plugins
        var defaultPluginTypes = new[]
        {
            typeof(Plugins.FileSystemPlugin),
            typeof(Plugins.ProjectAnalysisPlugin)
            // SubWorkflowPlugin temporarily disabled in some scenarios
        };

        foreach (var pluginType in defaultPluginTypes)
        {
            var plugin = _serviceProvider.GetRequiredService(pluginType);
            var pluginName = pluginType.Name.Replace("Plugin", "");
            kernel.Plugins.AddFromObject(plugin, pluginName);
        }

        // Add MCP servers if specified in workflow
        if (workflow != null)
        {
            await kernel.AddMcpServersFromWorkflowAsync(workflow, _serviceProvider);
        }
        
        return kernel;
    }
}

// SK automatically handles function calling in workflows through AI model function calling
// Tools return JSON strings that the AI can parse and understand
```

#### Workflow Integration Principles (Current Implementation)
- **Tool Discovery**: Built-in tools registered through KernelFactory at kernel creation time
- **Automatic Orchestration**: AI models use function calling to invoke tools based on natural language descriptions
- **State Management**: SK manages conversation state and tool call history through SemanticKernelOrchestrator
- **Error Recovery**: Built-in error handling and retry logic through SK filters and middleware
- **JSON Communication**: Tools return structured JSON for AI consumption and parsing

## Framework Implementation Summary

This framework specification reflects the current implementation that leverages Semantic Kernel's built-in capabilities to provide a robust, maintainable foundation for built-in tools. Key characteristics of the current implementation:

1. **Transient Tool Registration**: Built-in tools are registered as transient services and plugins are added to kernels at creation time
2. **JSON String Returns**: Tools return serialized JSON strings for efficient AI consumption rather than structured response objects  
3. **Standard .NET Patterns**: Configuration, DI, logging all use familiar .NET conventions
4. **Official MCP SDK Integration**: MCP servers integrated using official ModelContextProtocol SDK
5. **Flexible AI Provider Support**: Supports OpenAI, Azure OpenAI, GitHub Models, and local providers through unified KernelFactory
6. **Filter-Based Architecture**: Cross-cutting concerns handled through SK filters and middleware

### Current Tool Implementation Status

| Tool | Status | Implementation Notes |
|------|--------|---------------------|
| **Project Analysis Tool** | ✅ **IMPLEMENTED** | Full project analysis with JSON output |
| **File System Tool** | ✅ **IMPLEMENTED** | Secure file operations with configuration options |
| **Sub-workflow Tool** | 🚧 **PARTIAL** | Implemented but disabled in some test scenarios |
| **Build & Test Tool** | ❌ **PLANNED** | Not yet implemented |

### Enhancement Opportunities

The current implementation provides a solid foundation with several opportunities for enhancement:

- **Unified Response Format**: Implement ToolResponse<T> wrapper for consistent tool responses
- **Enhanced Caching**: Add vector store and memory caching integration
- **Full Configuration Integration**: Extend tool-specific configuration beyond FileSystemOptions
- **Azure Identity**: Implement DefaultAzureCredential for secure cloud authentication
- **Performance Monitoring**: Enhanced telemetry and performance tracking

The built-in tools can be enhanced to follow more standardized patterns while maintaining compatibility with the current JSON-based approach that works well with AI function calling.

## Individual Tool Specifications

The following built-in tools have dedicated specification documents:

| Tool | Document | Status |
|------|----------|--------|
| Project Analysis Tool | [project-analysis-tool.md](../developer/project-analysis-tool.md) | ✅ Implemented |
| File System Tool | [file-system-tool.md](../developer/file-system-tool.md) | ✅ Implemented |
| Sub-workflow Tool | [Built-in Tools Reference](../reference/built-in-tools.md#sub-workflow-tool) | 🚧 Partial |
| Build & Test Tool | [build-test-tool.md](../developer/build-test-tool.md) | � Planned |

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
