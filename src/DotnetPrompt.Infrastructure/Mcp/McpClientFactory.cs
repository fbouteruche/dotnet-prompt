using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Factory for creating MCP clients based on configuration
/// </summary>
public class McpClientFactory : IMcpClientFactory
{
    private readonly McpServerResolver _serverResolver;
    private readonly ILogger<McpClientFactory> _logger;

    public McpClientFactory(McpServerResolver serverResolver, ILogger<McpClientFactory> logger)
    {
        _serverResolver = serverResolver;
        _logger = logger;
    }

    /// <summary>
    /// Creates an MCP client for the specified configuration
    /// </summary>
    public async Task<IMcpClient> CreateClientAsync(McpServerConfig config, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating MCP client for {ServerName} ({ConnectionType})", config.Name, config.ConnectionType);

        return config.ConnectionType switch
        {
            McpConnectionType.Stdio => await CreateStdioClientAsync(config, cancellationToken),
            McpConnectionType.Sse => await CreateSseClientAsync(config, cancellationToken),
            _ => throw new ArgumentException($"Unsupported MCP connection type: {config.ConnectionType}")
        };
    }

    /// <summary>
    /// Creates a client for local stdio-based MCP servers
    /// </summary>
    private async Task<IMcpClient> CreateStdioClientAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        var command = await _serverResolver.ResolveServerCommandAsync(config);
        
        return new StdioMcpClient(command, config.Args ?? Array.Empty<string>(), config.WorkingDirectory, _logger);
    }

    /// <summary>
    /// Creates a client for remote SSE-based MCP servers
    /// </summary>
    private Task<IMcpClient> CreateSseClientAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        var endpoint = config.Endpoint ?? config.Server;
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("SSE MCP client requires an endpoint");
        }

        var client = new SseMcpClient(endpoint, config.AuthToken, config.Headers, config.Timeout, _logger);
        return Task.FromResult<IMcpClient>(client);
    }
}

/// <summary>
/// Basic implementation of stdio-based MCP client
/// This is a simplified implementation focusing on the integration pattern
/// </summary>
public class StdioMcpClient : IMcpClient
{
    private readonly string _command;
    private readonly string[] _args;
    private readonly string? _workingDirectory;
    private readonly ILogger _logger;
    private bool _connected = false;
    private bool _disposed = false;

    public StdioMcpClient(string command, string[] args, string? workingDirectory, ILogger logger)
    {
        _command = command;
        _args = args;
        _workingDirectory = workingDirectory;
        _logger = logger;
    }

    public bool IsConnected => _connected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Connecting to stdio MCP server: {Command} {Args}", _command, string.Join(" ", _args));
        
        // In a full implementation, this would start the MCP server process
        // and establish stdio communication
        await Task.Delay(100, cancellationToken); // Simulate connection time
        _connected = true;
        
        _logger.LogInformation("Connected to stdio MCP server: {Command}", _command);
    }

    public async Task<IEnumerable<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected)
        {
            throw new InvalidOperationException("MCP client is not connected");
        }

        // In a full implementation, this would send an MCP "tools/list" request
        // For now, return a mock tool list
        await Task.Delay(50, cancellationToken);
        
        return new[]
        {
            new McpTool 
            { 
                Name = "example_tool", 
                Description = "Example MCP tool",
                InputSchema = System.Text.Json.JsonDocument.Parse("{}").RootElement
            }
        };
    }

    public async Task<McpToolResult> CallToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!_connected)
        {
            throw new InvalidOperationException("MCP client is not connected");
        }

        _logger.LogDebug("Calling MCP tool: {ToolName} with parameters: {Parameters}", toolName, parameters);

        // In a full implementation, this would send an MCP "tools/call" request
        await Task.Delay(100, cancellationToken);

        return new McpToolResult
        {
            Success = true,
            Content = $"Mock result from {toolName}",
            Metadata = new Dictionary<string, object> { ["tool"] = toolName, ["server_type"] = "stdio" }
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // In a full implementation, this would terminate the MCP server process
            _connected = false;
            _disposed = true;
        }
    }
}

/// <summary>
/// Basic implementation of SSE-based MCP client
/// This is a simplified implementation focusing on the integration pattern
/// </summary>
public class SseMcpClient : IMcpClient
{
    private readonly string _endpoint;
    private readonly string? _authToken;
    private readonly Dictionary<string, string>? _headers;
    private readonly TimeSpan? _timeout;
    private readonly ILogger _logger;
    private bool _connected = false;
    private bool _disposed = false;

    public SseMcpClient(string endpoint, string? authToken, Dictionary<string, string>? headers, TimeSpan? timeout, ILogger logger)
    {
        _endpoint = endpoint;
        _authToken = authToken;
        _headers = headers;
        _timeout = timeout;
        _logger = logger;
    }

    public bool IsConnected => _connected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Connecting to SSE MCP server: {Endpoint}", _endpoint);
        
        // In a full implementation, this would establish SSE connection to the endpoint
        await Task.Delay(200, cancellationToken); // Simulate connection time
        _connected = true;
        
        _logger.LogInformation("Connected to SSE MCP server: {Endpoint}", _endpoint);
    }

    public async Task<IEnumerable<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected)
        {
            throw new InvalidOperationException("MCP client is not connected");
        }

        // In a full implementation, this would send an HTTP request to the MCP endpoint
        await Task.Delay(100, cancellationToken);
        
        return new[]
        {
            new McpTool 
            { 
                Name = "remote_tool", 
                Description = "Example remote MCP tool",
                InputSchema = System.Text.Json.JsonDocument.Parse("{}").RootElement
            }
        };
    }

    public async Task<McpToolResult> CallToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!_connected)
        {
            throw new InvalidOperationException("MCP client is not connected");
        }

        _logger.LogDebug("Calling remote MCP tool: {ToolName} at {Endpoint}", toolName, _endpoint);

        // In a full implementation, this would send an HTTP request to execute the tool
        await Task.Delay(150, cancellationToken);

        return new McpToolResult
        {
            Success = true,
            Content = $"Mock result from remote {toolName}",
            Metadata = new Dictionary<string, object> { ["tool"] = toolName, ["server_type"] = "sse", ["endpoint"] = _endpoint }
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // In a full implementation, this would close the HTTP connection
            _connected = false;
            _disposed = true;
        }
    }
}