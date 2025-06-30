# MCP Integration Specification (SK Plugin Wrappers)

## Overview

This document defines the Model Context Protocol (MCP) integration through Semantic Kernel plugin wrappers, including tool discovery, SK function mapping, parameter validation, and server management.

## Status
✅ **COMPLETE** - SK-based MCP integration patterns defined

## MCP-to-SK Plugin Architecture

### Core Integration Strategy
- **SK Plugin Wrappers**: MCP servers are wrapped as SK plugins for seamless integration
- **Function Mapping**: MCP tools become SK functions with proper annotations and metadata
- **Parameter Translation**: Automatic conversion between MCP schemas and SK function parameters
- **Error Handling**: SK filters handle MCP communication errors and retries
- **State Management**: SK conversation state includes MCP server connection status

### MCP Server Configuration (SK-Enhanced)

#### Enhanced MCP Configuration with SK Integration
```json
{
  "mcp_servers": {
    "filesystem-mcp": {
      "command": "npm",
      "args": ["run", "start"],
      "cwd": "./mcp-servers/filesystem",
      "version": "1.0.0",
      "sk_plugin_config": {
        "plugin_name": "FileSystemMCP",
        "description": "File system operations via MCP server",
        "function_prefix": "fs_",
        "parameter_validation": true,
        "retry_policy": {
          "max_attempts": 3,
          "backoff_ms": [1000, 2000, 4000]
        }
      }
    },
    "git-mcp": {
      "command": "python", 
      "args": ["-m", "git_mcp"],
      "version": "2.1.0",
      "sk_plugin_config": {
        "plugin_name": "GitMCP",
        "description": "Git operations via MCP server",
        "function_prefix": "git_",
        "security_filters": ["git_operation_validator"],
        "performance_monitoring": true
      }
    }
  },
  "sk_integration": {
    "auto_discovery": true,
    "function_naming_convention": "snake_case",
    "error_handling_strategy": "sk_filters",
    "telemetry_enabled": true,
    "vector_store_caching": true
  }
}
```

## MCP-to-SK Function Registration

### Dynamic Plugin Creation
```csharp
public class McpPluginFactory
{
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ILogger<McpPluginFactory> _logger;
    
    public async Task<KernelPlugin> CreateMcpPluginAsync(McpServerConfig serverConfig)
    {
        var mcpClient = await _mcpClientFactory.CreateClientAsync(serverConfig);
        var mcpTools = await mcpClient.ListToolsAsync();
        
        var functions = new List<KernelFunction>();
        
        foreach (var mcpTool in mcpTools)
        {
            var skFunction = CreateSkFunctionFromMcpTool(mcpTool, mcpClient, serverConfig);
            functions.Add(skFunction);
        }
        
        return KernelPluginFactory.CreateFromFunctions(
            serverConfig.SkPluginConfig.PluginName,
            serverConfig.SkPluginConfig.Description,
            functions);
    }
    
    private KernelFunction CreateSkFunctionFromMcpTool(McpTool mcpTool, 
        IMcpClient mcpClient, McpServerConfig serverConfig)
    {
        var functionName = $"{serverConfig.SkPluginConfig.FunctionPrefix}{mcpTool.Name}";
        
        return KernelFunctionFactory.CreateFromMethod(
            async (KernelArguments arguments) =>
            {
                // Convert SK arguments to MCP parameters
                var mcpParameters = ConvertSkArgumentsToMcpParameters(arguments, mcpTool.Schema);
                
                // Execute MCP tool with SK error handling
                var result = await ExecuteWithSkFilters(
                    () => mcpClient.CallToolAsync(mcpTool.Name, mcpParameters),
                    serverConfig);
                
                return result;
            },
            functionName,
            mcpTool.Description,
            GenerateSkParametersFromMcpSchema(mcpTool.Schema),
            returnType: typeof(string));
    }
}

### Automatic Tool Discovery
How MCP tools are discovered and registered with the workflow engine.

### Tool Registration Process
1. MCP server connection establishment
2. Tool capability discovery
3. Tool registration with Semantic Kernel
4. Parameter mapping and validation setup

## Parameter Mapping

### Workflow to MCP Parameter Translation
How parameters from workflow calls are mapped to MCP tool parameters.

### Type Conversion
- String, number, boolean conversions
- Complex object serialization
- Array parameter handling

## Error Handling and Resilience

### Connection Management
- Server startup and lifecycle management
- Connection retry logic
- Health checks and monitoring

### Error Categories
- Connection errors
- Tool execution errors
- Parameter validation errors
- Timeout errors

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

## Next Steps

1. Design the MCP server management system
2. Implement tool discovery and registration
3. Create parameter mapping and validation logic
4. Build connection management and retry mechanisms
5. Design security and isolation strategies
6. Create comprehensive MCP integration examples
