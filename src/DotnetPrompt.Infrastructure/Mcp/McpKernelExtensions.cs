using DotnetPrompt.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Extension methods for registering MCP servers with Semantic Kernel using official MCP SDK
/// </summary>
public static class McpKernelExtensions
{
    /// <summary>
    /// Adds MCP servers from workflow configuration to the kernel using official MCP SDK
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
        var mcpServerResolver = serviceProvider.GetRequiredService<McpServerResolver>();

        try
        {
            var mcpConfigs = await mcpConfigService.ParseMcpConfigurationAsync(workflow);
            
            if (!mcpConfigs.Any())
            {
                logger.LogDebug("No MCP servers configured in workflow");
                return kernel;
            }

            logger.LogInformation("Registering {Count} MCP servers from workflow using official SDK", mcpConfigs.Count());

            foreach (var mcpConfig in mcpConfigs)
            {
                try
                {
                    await RegisterMcpServerAsync(kernel, mcpConfig, mcpServerResolver, logger);
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
    /// Registers a single MCP server as a Semantic Kernel plugin using official MCP SDK
    /// </summary>
    /// <param name="kernel">Kernel to register with</param>
    /// <param name="config">MCP server configuration</param>
    /// <param name="serverResolver">Server resolver for command resolution</param>
    /// <param name="logger">Logger for diagnostics</param>
    private static async Task RegisterMcpServerAsync(
        Kernel kernel, 
        McpServerConfig config, 
        McpServerResolver serverResolver,
        ILogger logger)
    {
        try
        {
            // 1. Resolve command using enterprise server resolver
            if (config.ConnectionType == McpConnectionType.Stdio)
            {
                config.Command = await serverResolver.ResolveServerCommandAsync(config);
            }

            // 2. Create official SDK transport
            var transport = TransportConfigurationMapper.CreateTransport(config);
            
            // 3. Use official MCP client factory (NOT custom implementation)
            var mcpClient = await McpClientFactory.CreateAsync(transport);
            
            // 4. Discover tools using official SDK - returns McpClientTool instances (AIFunction)
            var tools = await mcpClient.ListToolsAsync();

            // 4.1 Log discovered tools
            if (tools == null || !tools.Any())
            {
                logger.LogWarning("No tools discovered for MCP server: {ServerName}", config.Name);
                return;
            }
            else
            {
                logger.LogDebug("Discovered {ToolCount} tools for MCP server: {ServerName}", 
                    tools.Count(), config.Name);
            }
            
            // 4.2 Log each tool's name and description
            foreach (var tool in tools)
            {
                logger.LogDebug("Tool discovered - Name: {ToolName}, Description: {ToolDescription}", 
                    tool.Name, tool.Description);
            }

            // 5. Convert McpClientTool (AIFunction) to KernelFunction using the AsKernelFunction() extension
            // Note: AsKernelFunction() is experimental but officially recommended by Microsoft DevBlogs example
#pragma warning disable SKEXP0001 // AsKernelFunction is experimental
            var kernelFunctions = tools.Select(aiFunction => aiFunction.AsKernelFunction());
#pragma warning restore SKEXP0001
            
            // 6. Add the functions as a plugin to Semantic Kernel
            kernel.Plugins.AddFromFunctions(config.Name, kernelFunctions);
            
            logger.LogDebug("MCP plugin {PluginName} registered with {ToolCount} tools using official SDK", 
                config.Name, tools.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create and register MCP plugin for {ServerName}", config.Name);
            throw;
        }
    }
}