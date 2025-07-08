using System.Diagnostics;
using System.Security;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Filters;

/// <summary>
/// Semantic Kernel filter for file system security validation and access control
/// </summary>
public class FileSystemSecurityFilter : IFunctionInvocationFilter
{
    private readonly ILogger<FileSystemSecurityFilter> _logger;
    private readonly FileSystemOptions _options;
    private readonly FileSystemSecurityPolicy _securityPolicy;

    public FileSystemSecurityFilter(
        ILogger<FileSystemSecurityFilter> logger,
        IOptions<FileSystemOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _securityPolicy = new FileSystemSecurityPolicy();
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var functionName = context.Function.Name;
        var pluginName = context.Function.PluginName;

        // Only apply security validation to file system functions
        if (pluginName != "FileSystem")
        {
            await next(context);
            return;
        }

        try
        {
            _logger.LogDebug("Validating file system access for function {PluginName}.{FunctionName} with correlation {CorrelationId}", 
                pluginName, functionName, correlationId);

            // Validate file system access based on function type
            await ValidateFileSystemAccess(context, correlationId);
            
            await next(context);
            
            // Log successful operation if audit logging is enabled
            if (_options.EnableAuditLogging)
            {
                LogFileSystemOperation(context, correlationId, true);
            }
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "File system security validation failed for function {PluginName}.{FunctionName} with correlation {CorrelationId}", 
                pluginName, functionName, correlationId);
            
            if (_options.EnableAuditLogging)
            {
                LogFileSystemOperation(context, correlationId, false, ex.Message);
            }
            
            throw new WorkflowExecutionException(
                $"File system access denied for function {pluginName}.{functionName}: {ex.Message}",
                ex,
                context.Function,
                context,
                correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file system validation for function {PluginName}.{FunctionName} with correlation {CorrelationId}", 
                pluginName, functionName, correlationId);
            
            if (_options.EnableAuditLogging)
            {
                LogFileSystemOperation(context, correlationId, false, ex.Message);
            }
            
            throw;
        }
    }

    /// <summary>
    /// Validate file system access based on the function being called
    /// </summary>
    private Task ValidateFileSystemAccess(FunctionInvocationContext context, string correlationId)
    {
        var functionName = context.Function.Name;
        var workingDirectory = _securityPolicy.GetWorkingDirectory(_options.WorkingDirectoryContext);

        return functionName switch
        {
            "file_read" or "file_write" or "file_exists" or "get_file_info" or "copy_file" 
                => ValidateFileAccess(context, workingDirectory, correlationId),
            "list_directory" or "create_directory" 
                => ValidateDirectoryAccess(context, workingDirectory, correlationId),
            _ => Task.CompletedTask // Allow other functions to proceed
        };
    }

    /// <summary>
    /// Validate file access for file-based operations
    /// </summary>
    private Task ValidateFileAccess(FunctionInvocationContext context, string workingDirectory, string correlationId)
    {
        // Extract file path from common parameter names
        var filePath = GetPathParameter(context, "filePath", "path", "file_path", "source_path", "destination_path");
        
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogWarning("No file path parameter found in function {FunctionName} with correlation {CorrelationId}", 
                context.Function.Name, correlationId);
            return Task.CompletedTask;
        }

        ValidatePathSecurity(filePath, workingDirectory, correlationId, "file");
        
        // Additional validation for file operations
        if (context.Function.Name == "file_write" || context.Function.Name == "copy_file")
        {
            ValidateWriteOperation(context, correlationId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate directory access for directory-based operations
    /// </summary>
    private Task ValidateDirectoryAccess(FunctionInvocationContext context, string workingDirectory, string correlationId)
    {
        var directoryPath = GetPathParameter(context, "directoryPath", "directory_path", "path");
        
        if (string.IsNullOrEmpty(directoryPath))
        {
            _logger.LogWarning("No directory path parameter found in function {FunctionName} with correlation {CorrelationId}", 
                context.Function.Name, correlationId);
            return Task.CompletedTask;
        }

        ValidatePathSecurity(directoryPath, workingDirectory, correlationId, "directory");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate path security according to security policy
    /// </summary>
    private void ValidatePathSecurity(string path, string workingDirectory, string correlationId, string pathType)
    {
        if (!_securityPolicy.IsPathAllowed(path, workingDirectory, _options))
        {
            var resolvedPath = Path.IsPathRooted(path) ? path : Path.Combine(workingDirectory, path);
            var normalizedPath = Path.GetFullPath(resolvedPath);
            
            _logger.LogWarning("Access denied to {PathType} path {Path} (resolved: {ResolvedPath}) with correlation {CorrelationId}", 
                pathType, path, normalizedPath, correlationId);
                
            throw new SecurityException($"Access denied to {pathType} path: {normalizedPath}. " +
                                      "Path is outside allowed directories or contains blocked patterns.");
        }
    }

    /// <summary>
    /// Additional validation for write operations
    /// </summary>
    private void ValidateWriteOperation(FunctionInvocationContext context, string correlationId)
    {
        // Check for overwrite protection
        if (context.Function.Name == "file_write" && 
            context.Arguments.TryGetValue("overwrite", out var overwriteValue) && 
            overwriteValue is false)
        {
            var filePath = GetPathParameter(context, "filePath", "path", "file_path");
            if (!string.IsNullOrEmpty(filePath))
            {
                var workingDirectory = _securityPolicy.GetWorkingDirectory(_options.WorkingDirectoryContext);
                var resolvedPath = Path.IsPathRooted(filePath) 
                    ? filePath 
                    : Path.Combine(workingDirectory, filePath);
                var normalizedPath = Path.GetFullPath(resolvedPath);
                
                if (File.Exists(normalizedPath))
                {
                    _logger.LogInformation("Write operation blocked - file exists and overwrite disabled: {Path} with correlation {CorrelationId}", 
                        normalizedPath, correlationId);
                    throw new SecurityException($"File exists and overwrite is disabled: {normalizedPath}");
                }
            }
        }
    }

    /// <summary>
    /// Get path parameter from context arguments using multiple possible parameter names
    /// </summary>
    private string? GetPathParameter(FunctionInvocationContext context, params string[] parameterNames)
    {
        foreach (var paramName in parameterNames)
        {
            if (context.Arguments.TryGetValue(paramName, out var value))
            {
                return value?.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// Log file system operation for audit trail
    /// </summary>
    private void LogFileSystemOperation(FunctionInvocationContext context, string correlationId, bool success, string? error = null)
    {
        var operation = new
        {
            CorrelationId = correlationId,
            Function = context.Function.Name,
            Plugin = context.Function.PluginName,
            Success = success,
            Error = error,
            Timestamp = DateTime.UtcNow,
            Arguments = context.Arguments.Where(kvp => !IsSensitiveParameter(kvp.Key))
                              .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString())
        };

        if (success)
        {
            _logger.LogInformation("File system operation completed: {@Operation}", operation);
        }
        else
        {
            _logger.LogWarning("File system operation failed: {@Operation}", operation);
        }
    }

    /// <summary>
    /// Check if parameter name indicates sensitive data that should not be logged
    /// </summary>
    private static bool IsSensitiveParameter(string parameterName)
    {
        var name = parameterName.ToLowerInvariant();
        return name.Contains("password") || name.Contains("secret") || name.Contains("key") || name.Contains("token");
    }
}