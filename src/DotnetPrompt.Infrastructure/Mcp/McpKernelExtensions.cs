using DotnetPrompt.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Extension methods for registering MCP servers with Semantic Kernel
/// </summary>
public static class McpKernelExtensions
{
    /// <summary>
    /// Adds MCP servers from workflow configuration to the kernel
    /// </summary>
    /// <param name="kernel">Kernel to register MCP plugins with</param>
    /// <param name="workflow">Workflow containing MCP configuration</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <returns>The kernel with MCP plugins registered</returns>
    public static async Task<Kernel> AddMcpServersFromWorkflowAsync(
        this Kernel kernel,
        DotpromptWorkflow workflow,
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Kernel>>();
        var mcpConfigService = serviceProvider.GetRequiredService<McpConfigurationService>();

        try
        {
            var mcpConfigs = await mcpConfigService.ParseMcpConfigurationAsync(workflow);
            
            if (!mcpConfigs.Any())
            {
                logger.LogDebug("No MCP servers configured in workflow");
                return kernel;
            }

            logger.LogInformation("Registering {Count} MCP servers from workflow", mcpConfigs.Count());

            foreach (var mcpConfig in mcpConfigs)
            {
                try
                {
                    await RegisterMcpServerAsync(kernel, mcpConfig, serviceProvider);
                    logger.LogInformation("Successfully registered MCP server: {ServerName} ({ConnectionType})", 
                        mcpConfig.Name, mcpConfig.ConnectionType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to register MCP server: {ServerName}", mcpConfig.Name);
                    // Continue with other servers - don't fail the entire workflow
                }
            }

            return kernel;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process MCP configuration from workflow");
            return kernel; // Return kernel without MCP plugins rather than failing
        }
    }

    /// <summary>
    /// Registers a single MCP server as a Semantic Kernel plugin
    /// </summary>
    /// <param name="kernel">Kernel to register with</param>
    /// <param name="config">MCP server configuration</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    private static async Task RegisterMcpServerAsync(
        Kernel kernel, 
        McpServerConfig config, 
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Kernel>>();

        try
        {
            // Create an MCP plugin wrapper for this server
            var mcpPlugin = await CreateMcpPluginAsync(config, serviceProvider);
            
            // Register the plugin with Semantic Kernel
            kernel.Plugins.AddFromObject(mcpPlugin, config.Name);
            
            logger.LogDebug("MCP plugin {PluginName} registered with {FunctionCount} functions", 
                config.Name, mcpPlugin.GetType().GetMethods().Count(m => m.GetCustomAttributes(typeof(Microsoft.SemanticKernel.KernelFunctionAttribute), false).Any()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create and register MCP plugin for {ServerName}", config.Name);
            throw;
        }
    }

    /// <summary>
    /// Creates an MCP plugin wrapper for the specified configuration
    /// </summary>
    /// <param name="config">MCP server configuration</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    /// <returns>MCP plugin instance</returns>
    private static async Task<object> CreateMcpPluginAsync(McpServerConfig config, IServiceProvider serviceProvider)
    {
        var mcpClientFactory = serviceProvider.GetRequiredService<IMcpClientFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<McpDynamicPlugin>>();

        // Create MCP client for this server
        var mcpClient = await mcpClientFactory.CreateClientAsync(config);
        
        // Connect to the server
        await mcpClient.ConnectAsync();
        
        // Create dynamic plugin wrapper
        var dynamicPlugin = new McpDynamicPlugin(mcpClient, config, logger);
        await dynamicPlugin.InitializeAsync();
        
        return dynamicPlugin;
    }
}

/// <summary>
/// Dynamic MCP plugin that wraps MCP server functionality as SK functions
/// </summary>
public class McpDynamicPlugin : IDisposable
{
    private readonly IMcpClient _mcpClient;
    private readonly McpServerConfig _config;
    private readonly ILogger<McpDynamicPlugin> _logger;
    private IEnumerable<McpTool> _tools = Array.Empty<McpTool>();
    private bool _disposed = false;

    public McpDynamicPlugin(IMcpClient mcpClient, McpServerConfig config, ILogger<McpDynamicPlugin> logger)
    {
        _mcpClient = mcpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the plugin by discovering available tools
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _tools = await _mcpClient.ListToolsAsync();
            _logger.LogInformation("Discovered {ToolCount} tools from MCP server {ServerName}", 
                _tools.Count(), _config.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover tools from MCP server {ServerName}", _config.Name);
            throw;
        }
    }

    /// <summary>
    /// Generic tool execution method that can be called by Semantic Kernel
    /// This is a placeholder - in a full implementation, we would dynamically generate
    /// methods with proper KernelFunction attributes for each discovered tool
    /// </summary>
    [KernelFunction("execute_tool")]
    [System.ComponentModel.Description("Executes an MCP tool with the specified parameters")]
    public async Task<string> ExecuteToolAsync(
        [System.ComponentModel.Description("Name of the tool to execute")] string toolName,
        [System.ComponentModel.Description("Tool parameters as JSON")] string parameters = "{}",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tool = _tools.FirstOrDefault(t => t.Name == toolName);
            if (tool == null)
            {
                throw new InvalidOperationException($"Tool '{toolName}' not found in MCP server '{_config.Name}'");
            }

            // Parse parameters
            var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parameters) 
                           ?? new Dictionary<string, object>();

            // Execute the tool
            var result = await _mcpClient.CallToolAsync(toolName, paramDict, cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"MCP tool execution failed: {result.ErrorMessage}");
            }

            return result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute MCP tool {ToolName} on server {ServerName}", toolName, _config.Name);
            throw;
        }
    }

    /// <summary>
    /// Lists available tools in this MCP server
    /// </summary>
    [KernelFunction("list_tools")]
    [System.ComponentModel.Description("Lists all available tools in this MCP server")]
    public Task<string> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var toolList = _tools.Select(t => new { name = t.Name, description = t.Description }).ToArray();
            return Task.FromResult(System.Text.Json.JsonSerializer.Serialize(toolList));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tools from MCP server {ServerName}", _config.Name);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _mcpClient?.Dispose();
            _disposed = true;
        }
    }
}