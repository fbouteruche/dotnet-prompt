using System.Text.Json;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Represents an MCP tool definition
/// </summary>
public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonElement InputSchema { get; set; }
}

/// <summary>
/// Represents an MCP tool execution result
/// </summary>
public class McpToolResult
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Interface for MCP client communication
/// </summary>
public interface IMcpClient : IDisposable
{
    /// <summary>
    /// Connects to the MCP server
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available tools from the MCP server
    /// </summary>
    Task<IEnumerable<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a tool on the MCP server
    /// </summary>
    Task<McpToolResult> CallToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the client is connected
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// Factory for creating MCP clients
/// </summary>
public interface IMcpClientFactory
{
    /// <summary>
    /// Creates an MCP client for the specified configuration
    /// </summary>
    Task<IMcpClient> CreateClientAsync(Core.Models.McpServerConfig config, CancellationToken cancellationToken = default);
}