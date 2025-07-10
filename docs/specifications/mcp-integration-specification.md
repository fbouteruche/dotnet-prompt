# MCP Integration Specification (Official SDK + SK Plugin Wrappers)

## Overview

This document defines the Model Context Protocol (MCP) integration using the **official MCP SDK for C#** through Semantic Kernel plugin wrappers, including tool discovery, SK function mapping, parameter validation, and server management.

## Status
🔧 **IN PROGRESS** - Transitioning to official MCP SDK implementation

## Architecture Decision: Official MCP SDK Required

**MANDATORY**: This implementation **MUST** use the official `ModelContextProtocol` NuGet package from the [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), following the patterns demonstrated in the [Microsoft DevBlogs MCP integration guide](https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-Step-guide/).

### Key Requirements:
- ✅ **Use Official SDK**: `ModelContextProtocol` NuGet package from https://github.com/modelcontextprotocol/csharp-sdk
- ✅ **Use McpClientTool**: MCP tools are already `AIFunction` instances, ready for SK integration
- ✅ **Use McpClientFactory**: Official factory pattern for creating MCP clients
- ✅ **Follow Official API**: Align with the official SDK's client and transport patterns documented at https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html
- ✅ **Maintain Enterprise Features**: Preserve configuration-driven server management and error handling

### API Reference
- **Official C# SDK Documentation**: https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html
- **GitHub Repository**: https://github.com/modelcontextprotocol/csharp-sdk
- **Microsoft DevBlogs Guide**: https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-Step-guide/

## MCP-to-SK Plugin Architecture

### Core Integration Strategy (Official SDK First)
- **Official MCP SDK**: Use `ModelContextProtocol` NuGet package from https://github.com/modelcontextprotocol/csharp-sdk
- **Direct AIFunction Integration**: MCP tools (`McpClientTool`) inherit from `AIFunction` and work directly with SK
- **Native Client Factory**: Use `McpClientFactory.CreateAsync()` for client creation
- **Configuration Layer**: YAML-based configuration system for enterprise server management
- **Error Handling**: SK filters handle MCP communication errors and retries
- **State Management**: SK conversation state includes MCP server connection status

### Implementation Approach: Hybrid Official SDK + Enterprise Configuration

```csharp
// ✅ MANDATORY: Use Official MCP SDK
using ModelContextProtocol;
using Microsoft.SemanticKernel;

public class OfficialMcpPluginFactory
{
    public async Task<KernelPlugin> CreateMcpPluginAsync(McpServerConfig serverConfig)
    {
        // 1. Use official MCP client factory (NOT custom IMcpClient)
        var transport = CreateTransportFromConfig(serverConfig);
        var mcpClient = await McpClientFactory.CreateAsync(transport);
        
        // 2. Use official tool discovery - returns McpClientTool instances (which are AIFunction)
        var tools = await mcpClient.ListToolsAsync();
        
        // 3. McpClientTool inherits from AIFunction, so they work directly with SK
        // No conversion needed - they're already compatible!
        return KernelPluginFactory.CreateFromFunctions(
            serverConfig.PluginName,
            serverConfig.Description, 
            tools); // Direct use - tools are already AIFunction instances
    }
    
    private IClientTransport CreateTransportFromConfig(McpServerConfig config)
    {
        return config.ConnectionType switch
        {
            McpConnectionType.Stdio => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = config.Name,
                Command = config.Command,
                Arguments = config.Args,
                Environment = null, // Add if needed
            }),
            McpConnectionType.Sse => new SseClientTransport(new SseClientTransportOptions
            {
                Uri = new Uri(config.Endpoint),
                // Add other SSE options as needed
            }),
            _ => throw new ArgumentException($"Unsupported connection type: {config.ConnectionType}")
        };
    }
}
```

### MCP Server Configuration (Official SDK + Enterprise Features)

#### Enhanced MCP Configuration with Official SDK Integration
```yaml
# dotnet-prompt workflow configuration
dotnet-prompt.mcp:
  # Local stdio MCP server (using official SDK)
  - server: "@modelcontextprotocol/server-filesystem"
    version: "1.0.0"
    connection_type: "stdio"
    command: "npx"
    args: ["-y", "@modelcontextprotocol/server-filesystem"]
    working_directory: "./workspace"
    config:
      root_path: "./examples"
    
  # Remote SSE MCP server (using official SDK)  
  - server: "analytics-api"
    version: "2.0.0"
    connection_type: "sse"
    endpoint: "https://api.example.com/mcp/v2"
    auth_token: "${ANALYTICS_API_TOKEN}"
    headers:
      "Authorization": "Bearer ${ANALYTICS_API_TOKEN}"
      "X-Client-ID": "dotnet-prompt"
    timeout: "30s"

# Global MCP integration settings
dotnet-prompt.mcp-settings:
  auto_discovery: true
  function_naming_convention: "snake_case"
  error_handling_strategy: "sk_filters"
  telemetry_enabled: true
  vector_store_caching: true
  retry_policy:
    max_attempts: 3
    backoff_ms: [1000, 2000, 4000]
```

#### Official SDK Transport Configuration Mapping
```csharp
// ✅ REQUIRED: Map enterprise config to official SDK transports
public static class TransportConfigurationMapper
{
    public static IClientTransport CreateTransport(McpServerConfig config)
    {
        return config.ConnectionType switch
        {
            McpConnectionType.Stdio => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = config.Name,
                Command = ResolveCommand(config), // Use existing server resolver
                Arguments = config.Args ?? Array.Empty<string>(),
                Environment = null, // Add environment variables if needed
            }),
            
            McpConnectionType.Sse => new SseClientTransport(new SseClientTransportOptions
            {
                Uri = new Uri(config.Endpoint ?? config.Server ?? throw new InvalidOperationException("Endpoint required for SSE")),
                // Add SSE-specific configuration as needed
            }),
            
            _ => throw new NotSupportedException($"Connection type {config.ConnectionType} not supported")
        };
    }
}
```

## MCP-to-SK Function Registration (Official SDK Pattern)

### Official SDK Integration Pattern
Following the [official MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk), the implementation **MUST** use the official MCP SDK pattern where `McpClientTool` instances are already `AIFunction` compatible:

```csharp
// ✅ MANDATORY: Official SDK usage pattern
public class McpWorkflowOrchestrator
{
    public async Task<Kernel> AddMcpServersToKernelAsync(
        Kernel kernel, 
        IEnumerable<McpServerConfig> mcpConfigs)
    {
        foreach (var config in mcpConfigs)
        {
            // 1. Create transport from enterprise configuration
            var transport = TransportConfigurationMapper.CreateTransport(config);
            
            // 2. Use official MCP client factory (NOT custom implementation)
            var mcpClient = await McpClientFactory.CreateAsync(transport);
            
            // 3. Discover tools using official SDK - returns McpClientTool instances
            var tools = await mcpClient.ListToolsAsync();
            
            // 4. McpClientTool inherits from AIFunction - direct SK compatibility!
            // No conversion needed - they're already compatible with SK
            kernel.Plugins.AddFromFunctions(config.PluginName, tools);
            
            _logger.LogInformation("Registered MCP server {ServerName} with {ToolCount} tools", 
                config.Name, tools.Count);
        }
        
        return kernel;
    }
}
```

### Migration from Custom Implementation

**REMOVE** the following custom implementations:
- ❌ `IMcpClient` interface and custom implementations
- ❌ `McpDynamicPlugin` custom wrapper class  
- ❌ `CreateSkFunctionFromMcpTool` manual conversion methods
- ❌ Custom `McpTool` and `McpToolResult` models

**REPLACE** with official SDK:
- ✅ `IMcpClient` from official NuGet package
- ✅ `McpClientTool` (already `AIFunction` instances)
- ✅ Official MCP data models and transport abstractions

### Enterprise Configuration Integration

**PRESERVE** these enterprise features while using official SDK:
- ✅ YAML-based MCP server configuration
- ✅ Automatic server command resolution (npm, pip, dotnet tools)
- ✅ Environment variable resolution for auth tokens
- ✅ Connection type auto-detection
- ✅ Server lifecycle management
- ✅ Retry policies and error handling via SK filters

```csharp
// ✅ Enterprise wrapper around official SDK
public class EnterpriseWorkflowOrchestrator
{
    private readonly McpConfigurationService _configService;
    private readonly McpServerResolver _serverResolver;
    private readonly ILogger<EnterpriseWorkflowOrchestrator> _logger;
    
    public async Task<Kernel> RegisterMcpServersAsync(Kernel kernel, DotpromptWorkflow workflow)
    {
        // 1. Parse enterprise YAML configuration
        var mcpConfigs = await _configService.ParseMcpConfigurationAsync(workflow);
        
        foreach (var config in mcpConfigs)
        {
            try
            {
                // 2. Resolve command using enterprise server resolver
                if (config.ConnectionType == McpConnectionType.Stdio)
                {
                    config.Command = await _serverResolver.ResolveServerCommandAsync(config);
                }
                
                // 3. Create official SDK transport
                var transport = TransportConfigurationMapper.CreateTransport(config);
                
                // 4. Use official MCP client (NOT custom implementation)
                var mcpClient = await McpClientFactory.CreateAsync(transport);
                
                // 5. Register tools using direct compatibility - McpClientTool is AIFunction
                var tools = await mcpClient.ListToolsAsync();
                kernel.Plugins.AddFromFunctions(config.PluginName, tools);
                
                _logger.LogInformation("Successfully registered MCP server: {ServerName}", config.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register MCP server: {ServerName}", config.Name);
                // Continue with other servers
            }
        }
        
        return kernel;
    }
}
```

## Parameter Mapping and Type Conversion (Official SDK Automatic)

### Official SDK Handles Everything Automatically
The official MCP SDK's `McpClientTool` instances handle all parameter mapping and type conversion automatically since they inherit from `AIFunction`:
- ✅ **Direct AIFunction Compatibility**: `McpClientTool` inherits from `AIFunction` - no conversion needed
- ✅ **Automatic Type Conversion**: String, number, boolean, object conversions handled by the SDK
- ✅ **Schema Validation**: MCP tool input schema validation built into `McpClientTool`
- ✅ **Parameter Binding**: SK function parameter binding works directly with `McpClientTool`
- ✅ **Return Type Handling**: Automatic result type conversion built into the official SDK

**NO CUSTOM PARAMETER MAPPING REQUIRED** - the official SDK's `McpClientTool` handles this completely.

### Enterprise Validation Layer (Optional)
```csharp
// ✅ Optional: Add enterprise validation as SK filter
public class McpParameterValidationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        // Enterprise-specific validation rules
        if (context.Function.PluginName.EndsWith("Mcp"))
        {
            await ValidateEnterprisePolicy(context.Arguments);
        }
        
        await next(context);
    }
}
```

## Error Handling and Resilience (SK Filters + Official SDK)

### Official SDK Error Handling
The official MCP SDK provides built-in error handling for:
- ✅ **Connection Failures**: Automatic transport-level error handling
- ✅ **Protocol Errors**: MCP protocol error parsing and recovery
- ✅ **Timeout Management**: Built-in timeout handling per transport type

### Enterprise Error Handling via SK Filters
```csharp
// ✅ Enterprise error handling as SK filter
public class McpErrorHandlingFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        var retryPolicy = GetRetryPolicy(context.Function.PluginName);
        
        await retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                await next(context);
            }
            catch (McpException mcpEx) when (IsRetryable(mcpEx))
            {
                _logger.LogWarning("Retryable MCP error: {Error}", mcpEx.Message);
                throw; // Let retry policy handle it
            }
            catch (McpException mcpEx)
            {
                _logger.LogError("Non-retryable MCP error: {Error}", mcpEx.Message);
                context.Result = FunctionResult.FromError("MCP tool execution failed");
            }
        });
    }
}
```

### Error Categories (Handled by Official SDK)
- **Connection errors**: Transport-level failures
- **Tool execution errors**: MCP server-side failures  
- **Parameter validation errors**: Schema validation failures
- **Timeout errors**: Request timeout handling

## Clarifying Questions

### 1. MCP Server Management
- How should MCP servers be discovered and installed?
- What is the lifecycle management strategy for MCP servers?
- How should server dependencies be handled?
- Should there be a registry or marketplace for MCP servers?
- How should server versioning and compatibility be managed?

### 2. Server Configuration
- How should MCP server configuration be structured?
- What configuration options should be available per server?
- How should server-specific settings be handled?
- Should there be global MCP configuration options?
- How should configuration validation work for MCP servers?

### 3. Tool Discovery
- How should tools be discovered from MCP servers?
- What metadata should be extracted from MCP tools?
- How should tool namespacing work with multiple servers?
- Should there be tool conflict resolution?
- How should tool availability be communicated to workflows?

### 4. Parameter Mapping and Validation
- How should complex parameter types be handled?
- What validation should be performed on parameters?
- How should parameter defaults work with MCP tools?
- Should there be parameter transformation capabilities?
- How should optional vs required parameters be handled?

### 5. Connection and Communication
- What communication protocols should be supported?
- How should connection pooling work?
- What timeout and retry strategies should be implemented?
- How should authentication work with MCP servers?
- Should there be connection encryption requirements?

### 6. Error Handling
- How should different types of MCP errors be handled?
- What retry logic should be implemented?
- How should error messages be propagated to users?
- Should there be fallback mechanisms for failed MCP calls?
- How should partial failures be handled in multi-tool workflows?

### 7. Performance and Scalability
- How should concurrent MCP tool calls be handled?
- What caching strategies should be implemented?
- How should long-running MCP operations be managed?
- Should there be resource limits for MCP operations?
- How should memory usage be controlled?

### 8. Security and Isolation
- How should MCP server processes be isolated?
- What security restrictions should be applied to MCP tools?
- How should sensitive data be handled in MCP communications?
- Should there be permission controls for MCP operations?
- How should audit logging work for MCP tool usage?

### 9. Development and Debugging
- How should MCP integration be debugged?
- What logging should be available for MCP operations?
- Should there be a development mode for MCP testing?
- How should MCP tool development be supported?
- What diagnostic tools should be available?

### 10. Workflow Integration
- How should MCP tools be referenced in workflows?
- Should there be syntax sugar for common MCP operations?
- How should MCP tool results be formatted for AI consumption?
- Should there be tool composition capabilities?
- How should MCP tools integrate with built-in tools?

## Implementation Roadmap (Official SDK First)

### Phase 1: Official SDK Integration (IMMEDIATE)
1. ✅ **Add Official NuGet Package**: Install `ModelContextProtocol` package
2. ✅ **Remove Custom Implementations**: Delete `IMcpClient`, `McpTool`, `McpToolResult` 
3. ✅ **Implement Transport Mapping**: Map enterprise config to official SDK transports
4. ✅ **Use .AsKernelFunction()**: Replace custom function conversion with official extension
5. ✅ **Test Official SDK Integration**: Verify compatibility with existing configuration

### Phase 2: Enterprise Features Integration
1. Preserve YAML configuration parsing
2. Maintain server command resolution
3. Add SK filters for enterprise error handling
4. Implement configuration validation
5. Add comprehensive logging and telemetry

### Phase 3: Advanced Features  
1. Connection pooling and lifecycle management
2. Security filters and isolation strategies
3. Performance monitoring and caching
4. Development and debugging tools
5. Comprehensive integration examples

## Migration Checklist

### ❌ REMOVE (Custom Implementation)
- [ ] `src/DotnetPrompt.Infrastructure/Mcp/IMcpClient.cs`
- [ ] `src/DotnetPrompt.Infrastructure/Mcp/McpClientFactory.cs` (custom implementation)
- [ ] `McpDynamicPlugin` class in `McpKernelExtensions.cs`
- [ ] Custom `McpTool` and `McpToolResult` models
- [ ] `CreateSkFunctionFromMcpTool` manual conversion methods

### ✅ ADD (Official SDK Integration)  
- [ ] `ModelContextProtocol` NuGet package reference
- [ ] `TransportConfigurationMapper` for enterprise config mapping
- [ ] `EnterpriseWorkflowOrchestrator` using official SDK
- [ ] SK filters for enterprise error handling and validation
- [ ] Integration tests with real MCP servers

### 🔄 PRESERVE (Enterprise Features)
- [ ] `McpConfigurationService` - YAML parsing and validation
- [ ] `McpServerResolver` - Command resolution from package managers
- [ ] `McpConnectionTypeDetector` - Auto-detection logic
- [ ] Enterprise configuration models and validation
- [ ] Environment variable resolution and security features

## Success Criteria

1. **Official SDK Usage**: All MCP communication uses `ModelContextProtocol` NuGet package
2. **SK Integration**: All tools registered via `.AsKernelFunction()` extension
3. **Configuration Compatibility**: Existing YAML configurations work without changes
4. **Feature Parity**: All enterprise features (resolution, validation, error handling) preserved
5. **Performance**: No degradation in workflow execution performance
6. **Reliability**: Improved reliability through official SDK protocol handling
