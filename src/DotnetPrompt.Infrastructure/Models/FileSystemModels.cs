using System.ComponentModel;

namespace DotnetPrompt.Infrastructure.Models;

/// <summary>
/// Configuration options for file system operations with security policies
/// </summary>
public class FileSystemOptions
{
    /// <summary>
    /// Directories where file operations are allowed. Default: current working directory
    /// </summary>
    public string[] AllowedDirectories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Directories that are blocked even within allowed directories
    /// </summary>
    public string[] BlockedDirectories { get; set; } = { "bin", "obj", ".git", "node_modules" };

    /// <summary>
    /// File extensions that are allowed (empty = all allowed)
    /// </summary>
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// File extensions that are blocked
    /// </summary>
    public string[] BlockedExtensions { get; set; } = { ".exe", ".dll", ".so", ".dylib" };

    /// <summary>
    /// Maximum file size in bytes for operations
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum number of files per bulk operation
    /// </summary>
    public int MaxFilesPerOperation { get; set; } = 1000;

    /// <summary>
    /// Require confirmation for destructive operations
    /// </summary>
    public bool RequireConfirmationForDelete { get; set; } = true;

    /// <summary>
    /// Enable audit logging for all file operations
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Working directory context override (CLI --context parameter)
    /// </summary>
    public string? WorkingDirectoryContext { get; set; }
}

/// <summary>
/// Comprehensive security policy for file system operations
/// </summary>
public class FileSystemSecurityPolicy
{
    /// <summary>
    /// Get the working directory context, resolving CLI context or current directory
    /// </summary>
    public string GetWorkingDirectory(string? cliContext = null)
    {
        return cliContext ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// Validate if a path is allowed according to security policy
    /// </summary>
    public bool IsPathAllowed(string requestedPath, string workingDirectory, FileSystemOptions options)
    {
        var resolvedPath = Path.IsPathRooted(requestedPath) 
            ? requestedPath 
            : Path.Combine(workingDirectory, requestedPath);
            
        var normalizedPath = Path.GetFullPath(resolvedPath);
        
        // Check against allowed directories (default to working directory if empty)
        var allowedDirs = options.AllowedDirectories.Length > 0 
            ? options.AllowedDirectories 
            : new[] { workingDirectory };
            
        var isInAllowedDirectory = allowedDirs.Any(allowedDir =>
        {
            var normalizedAllowedDir = Path.GetFullPath(allowedDir);
            return normalizedPath.StartsWith(normalizedAllowedDir, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Equals(normalizedAllowedDir, StringComparison.OrdinalIgnoreCase);
        });
                                    
        if (!isInAllowedDirectory)
        {
            return false;
        }
        
        // Check against blocked directories
        var isInBlockedDirectory = options.BlockedDirectories.Any(blockedDir =>
        {
            var pathSegments = normalizedPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return pathSegments.Contains(blockedDir, StringComparer.OrdinalIgnoreCase);
        });
            
        if (isInBlockedDirectory)
        {
            return false;
        }
        
        // Check file extension restrictions
        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        
        if (options.AllowedExtensions.Length > 0 && 
            !options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        
        if (options.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        
        return true;
    }
}

/// <summary>
/// Result of a file system operation
/// </summary>
/// <typeparam name="T">Type of operation result data</typeparam>
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

/// <summary>
/// Tool source enumeration
/// </summary>
public enum ToolSource
{
    BuiltIn,
    MCP,
    External
}

/// <summary>
/// Directory listing result with comprehensive metadata
/// </summary>
public record DirectoryListingResult(
    string Path,
    FileSystemItem[] Items,
    int TotalFiles,
    int TotalDirectories,
    long TotalSizeBytes,
    bool Truncated);

/// <summary>
/// File system item with metadata
/// </summary>
public record FileSystemItem(
    string Name,
    string Path,
    string Type, // "file" | "directory"
    long SizeBytes,
    DateTime Created,
    DateTime Modified,
    string? Extension,
    bool IsHidden);