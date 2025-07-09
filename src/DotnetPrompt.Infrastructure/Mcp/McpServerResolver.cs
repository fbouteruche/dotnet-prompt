using System.Diagnostics;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Mcp;

/// <summary>
/// Resolves MCP server commands from package managers and validates server availability
/// </summary>
public class McpServerResolver
{
    private readonly ILogger<McpServerResolver> _logger;

    public McpServerResolver(ILogger<McpServerResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves the command to execute for a local MCP server
    /// </summary>
    /// <param name="config">MCP server configuration</param>
    /// <returns>Resolved command or throws if server cannot be resolved</returns>
    /// <exception cref="InvalidOperationException">Thrown when server cannot be resolved</exception>
    public async Task<string> ResolveServerCommandAsync(McpServerConfig config)
    {
        if (string.IsNullOrEmpty(config.Server))
        {
            throw new InvalidOperationException("MCP server name is required for command resolution");
        }

        // If command is explicitly provided, validate and return it
        if (!string.IsNullOrEmpty(config.Command))
        {
            await ValidateCommandAsync(config.Command);
            return config.Command;
        }

        var serverName = config.Server;
        _logger.LogDebug("Resolving MCP server command for: {ServerName}", serverName);

        try
        {
            // Try different resolution strategies
            var resolvedCommand = await TryResolveFromNpmAsync(serverName) ??
                                 await TryResolveFromPipAsync(serverName) ??
                                 await TryResolveFromDotnetToolAsync(serverName) ??
                                 await TryResolveDirectCommandAsync(serverName);

            if (resolvedCommand != null)
            {
                _logger.LogInformation("Resolved MCP server {ServerName} to command: {Command}", serverName, resolvedCommand);
                return resolvedCommand;
            }

            throw new InvalidOperationException($"Cannot resolve MCP server: {serverName}. " +
                "Server not found in npm, pip, dotnet tools, or as direct command.");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error resolving MCP server {ServerName}", serverName);
            throw new InvalidOperationException($"Failed to resolve MCP server '{serverName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Attempts to resolve server from npm packages
    /// </summary>
    private async Task<string?> TryResolveFromNpmAsync(string serverName)
    {
        try
        {
            // Check if npm is available
            if (!await IsCommandAvailableAsync("npm"))
            {
                return null;
            }

            // Check for scoped packages (@modelcontextprotocol/server-*)
            if (serverName.StartsWith("@"))
            {
                var isInstalled = await IsNpmPackageInstalledAsync(serverName);
                if (isInstalled)
                {
                    return $"npx {serverName}";
                }
            }

            // Check for MCP packages ending with -mcp
            if (serverName.EndsWith("-mcp"))
            {
                var isInstalled = await IsNpmPackageInstalledAsync(serverName);
                if (isInstalled)
                {
                    return $"npx {serverName}";
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve from npm: {ServerName}", serverName);
            return null;
        }
    }

    /// <summary>
    /// Attempts to resolve server from Python packages
    /// </summary>
    private async Task<string?> TryResolveFromPipAsync(string serverName)
    {
        try
        {
            // Check if python is available
            if (!await IsCommandAvailableAsync("python") && !await IsCommandAvailableAsync("python3"))
            {
                return null;
            }

            var pythonCmd = await IsCommandAvailableAsync("python") ? "python" : "python3";

            // Check if it's a Python module (contains underscores or is known pattern)
            if (serverName.Contains("_") || serverName.EndsWith("_mcp"))
            {
                var isInstalled = await IsPythonPackageInstalledAsync(serverName, pythonCmd);
                if (isInstalled)
                {
                    return $"{pythonCmd} -m {serverName}";
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve from pip: {ServerName}", serverName);
            return null;
        }
    }

    /// <summary>
    /// Attempts to resolve server from dotnet tools
    /// </summary>
    private async Task<string?> TryResolveFromDotnetToolAsync(string serverName)
    {
        try
        {
            // Check if dotnet is available
            if (!await IsCommandAvailableAsync("dotnet"))
            {
                return null;
            }

            // Check if it's installed as a global tool
            var isInstalled = await IsDotnetToolInstalledAsync(serverName);
            if (isInstalled)
            {
                return serverName;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve from dotnet tools: {ServerName}", serverName);
            return null;
        }
    }

    /// <summary>
    /// Attempts to resolve as a direct command
    /// </summary>
    private async Task<string?> TryResolveDirectCommandAsync(string serverName)
    {
        try
        {
            await ValidateCommandAsync(serverName);
            return serverName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates that a command is available in the system PATH
    /// </summary>
    private async Task ValidateCommandAsync(string command)
    {
        var isAvailable = await IsCommandAvailableAsync(command);
        if (!isAvailable)
        {
            throw new InvalidOperationException($"Command '{command}' not found in PATH");
        }
    }

    /// <summary>
    /// Checks if a command is available in the system PATH
    /// </summary>
    private async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var whichCommand = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = whichCommand,
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an npm package is installed globally or locally
    /// </summary>
    private async Task<bool> IsNpmPackageInstalledAsync(string packageName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = $"list -g {packageName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a Python package is installed
    /// </summary>
    private async Task<bool> IsPythonPackageInstalledAsync(string packageName, string pythonCommand)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonCommand,
                    Arguments = $"-c \"import {packageName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a dotnet tool is installed globally
    /// </summary>
    private async Task<bool> IsDotnetToolInstalledAsync(string toolName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool list -g",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0 && output.Contains(toolName);
        }
        catch
        {
            return false;
        }
    }
}