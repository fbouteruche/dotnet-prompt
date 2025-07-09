using System.Diagnostics;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Semantic Kernel filter for MCP function execution error handling and logging
/// </summary>
public class McpExecutionFilter : IFunctionInvocationFilter
{
    private readonly ILogger<McpExecutionFilter> _logger;

    public McpExecutionFilter(ILogger<McpExecutionFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles MCP function invocation with appropriate error handling for both local and remote connections
    /// </summary>
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        if (!IsMcpFunction(context.Function))
        {
            await next(context);
            return;
        }

        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;
        var connectionType = GetMcpConnectionType(context.Function);

        using var activity = new Activity("mcp-function-execution");
        activity.Start();
        activity.SetTag("mcp.plugin_name", pluginName);
        activity.SetTag("mcp.function_name", functionName);
        activity.SetTag("mcp.connection_type", connectionType.ToString());
        activity.SetTag("correlation_id", correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing MCP function {PluginName}.{FunctionName} ({ConnectionType}) with correlation {CorrelationId}", 
                pluginName, functionName, connectionType, correlationId);

            await next(context);

            stopwatch.Stop();
            _logger.LogInformation("MCP function {PluginName}.{FunctionName} executed successfully in {ExecutionTime}ms", 
                pluginName, functionName, stopwatch.ElapsedMilliseconds);

            activity.SetTag("execution.success", true);
            activity.SetTag("execution.duration_ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity.SetTag("execution.success", false);
            activity.SetTag("execution.duration_ms", stopwatch.ElapsedMilliseconds);
            activity.SetTag("error.type", ex.GetType().Name);
            activity.SetTag("error.message", ex.Message);

            var enhancedException = EnhanceExceptionForConnectionType(ex, connectionType, pluginName, functionName);
            
            _logger.LogError(enhancedException, 
                "MCP function {PluginName}.{FunctionName} ({ConnectionType}) failed after {ExecutionTime}ms with correlation {CorrelationId}", 
                pluginName, functionName, connectionType, stopwatch.ElapsedMilliseconds, correlationId);

            throw enhancedException;
        }
    }

    /// <summary>
    /// Determines if a function is from an MCP plugin
    /// </summary>
    /// <param name="function">Function to check</param>
    /// <returns>True if function is from MCP plugin</returns>
    private static bool IsMcpFunction(KernelFunction function)
    {
        // MCP plugins should have "Mcp" suffix in their plugin name
        return function.PluginName?.EndsWith("Mcp", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Attempts to determine the MCP connection type from function metadata
    /// </summary>
    /// <param name="function">Function to analyze</param>
    /// <returns>Connection type (defaults to Stdio if not determinable)</returns>
    private static McpConnectionType GetMcpConnectionType(KernelFunction function)
    {
        // Try to extract connection type from function metadata
        // This would be set during plugin registration
        if (function.Metadata.AdditionalProperties?.TryGetValue("mcp_connection_type", out var connectionTypeValue) == true)
        {
            if (Enum.TryParse<McpConnectionType>(connectionTypeValue.ToString(), out var parsedType))
            {
                return parsedType;
            }
        }

        // Default to Stdio if not specified
        return McpConnectionType.Stdio;
    }

    /// <summary>
    /// Enhances exceptions with connection-type-specific context
    /// </summary>
    /// <param name="originalException">Original exception</param>
    /// <param name="connectionType">MCP connection type</param>
    /// <param name="pluginName">Plugin name</param>
    /// <param name="functionName">Function name</param>
    /// <returns>Enhanced exception with additional context</returns>
    private KernelException EnhanceExceptionForConnectionType(
        Exception originalException, 
        McpConnectionType connectionType, 
        string pluginName, 
        string functionName)
    {
        var baseMessage = $"MCP function '{pluginName}.{functionName}' failed";
        
        return connectionType switch
        {
            McpConnectionType.Stdio => CreateLocalMcpException(originalException, baseMessage),
            McpConnectionType.Sse => CreateRemoteMcpException(originalException, baseMessage),
            _ => new KernelException($"{baseMessage}: {originalException.Message}", originalException)
        };
    }

    /// <summary>
    /// Creates an enhanced exception for local MCP server errors
    /// </summary>
    private KernelException CreateLocalMcpException(Exception originalException, string baseMessage)
    {
        var message = $"{baseMessage} (Local MCP Server)";
        
        // Enhance message based on exception type
        message = originalException switch
        {
            InvalidOperationException => $"{message}: Server process issue - {originalException.Message}",
            TimeoutException => $"{message}: Server response timeout - check if MCP server is responsive",
            System.ComponentModel.Win32Exception => $"{message}: Failed to start MCP server process - {originalException.Message}",
            System.IO.IOException => $"{message}: I/O error communicating with MCP server - {originalException.Message}",
            _ => $"{message}: {originalException.Message}"
        };

        return new KernelException(message, originalException);
    }

    /// <summary>
    /// Creates an enhanced exception for remote MCP server errors
    /// </summary>
    private KernelException CreateRemoteMcpException(Exception originalException, string baseMessage)
    {
        var message = $"{baseMessage} (Remote MCP Server)";
        
        // Enhance message based on exception type
        message = originalException switch
        {
            HttpRequestException => $"{message}: Network communication failed - {originalException.Message}",
            TaskCanceledException => $"{message}: Request timeout - check network connectivity and server availability",
            UnauthorizedAccessException => $"{message}: Authentication failed - verify auth token and permissions",
            System.Net.Sockets.SocketException => $"{message}: Network connectivity issue - {originalException.Message}",
            System.Security.SecurityException => $"{message}: Security/SSL issue - verify endpoint security configuration",
            _ => $"{message}: {originalException.Message}"
        };

        return new KernelException(message, originalException);
    }
}