using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Semantic Kernel plugin for file operations with proper security and validation
/// </summary>
public class FileOperationsPlugin
{
    private readonly ILogger<FileOperationsPlugin> _logger;

    public FileOperationsPlugin(ILogger<FileOperationsPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("read_file")]
    [Description("Reads the contents of a file from the file system")]
    [return: Description("The contents of the file as a string")]
    public async Task<string> ReadFileAsync(
        [Description("The absolute or relative path to the file to read")] string filePath,
        [Description("Text encoding to use (default: utf-8)")] string encoding = "utf-8",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Reading file via SK function: {FilePath}", filePath);
            
            // Validate file path
            var validatedPath = ValidateAndResolvePath(filePath);
            
            if (!File.Exists(validatedPath))
            {
                throw new FileNotFoundException($"File not found: {validatedPath}");
            }

            // Check file size (reasonable limit for text processing)
            var fileInfo = new FileInfo(validatedPath);
            if (fileInfo.Length > 10 * 1024 * 1024) // 10MB limit
            {
                throw new InvalidOperationException($"File too large: {fileInfo.Length} bytes. Maximum allowed: 10MB");
            }

            // Read file content
            var content = await File.ReadAllTextAsync(validatedPath, cancellationToken);
            
            _logger.LogInformation("Successfully read file {FilePath} ({Size} bytes) in {Duration}ms", 
                validatedPath, content.Length, stopwatch.ElapsedMilliseconds);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FilePath} via SK function", filePath);
            throw new KernelException($"File read failed: {ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("write_file")]
    [Description("Writes content to a file, creating directories as needed")]
    [return: Description("Success message with file path and size information")]
    public async Task<string> WriteFileAsync(
        [Description("The absolute or relative path to the file to write")] string filePath,
        [Description("The content to write to the file")] string content,
        [Description("Text encoding to use (default: utf-8)")] string encoding = "utf-8",
        [Description("Whether to overwrite existing files (default: true)")] bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Writing file via SK function: {FilePath}", filePath);
            
            // Validate file path
            var validatedPath = ValidateAndResolvePath(filePath);
            
            // Check if file exists and overwrite flag
            if (File.Exists(validatedPath) && !overwrite)
            {
                throw new InvalidOperationException($"File already exists and overwrite is disabled: {validatedPath}");
            }

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Write file content
            await File.WriteAllTextAsync(validatedPath, content, cancellationToken);
            
            var fileInfo = new FileInfo(validatedPath);
            var result = $"Successfully wrote {content.Length} characters to {validatedPath} ({fileInfo.Length} bytes) in {stopwatch.ElapsedMilliseconds}ms";
            
            _logger.LogInformation("Successfully wrote file {FilePath} ({Size} bytes) in {Duration}ms", 
                validatedPath, fileInfo.Length, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file {FilePath} via SK function", filePath);
            throw new KernelException($"File write failed: {ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("check_file_exists")]
    [Description("Checks if a file exists at the specified path")]
    [return: Description("True if the file exists, false otherwise")]
    public async Task<bool> CheckFileExistsAsync(
        [Description("The absolute or relative path to check")] string filePath,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogDebug("Checking file existence via SK function: {FilePath}", filePath);
            
            var validatedPath = ValidateAndResolvePath(filePath);
            var exists = File.Exists(validatedPath);
            
            _logger.LogDebug("File {FilePath} exists: {Exists}", validatedPath, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence {FilePath} via SK function", filePath);
            return false;
        }
    }

    [KernelFunction("get_file_info")]
    [Description("Gets information about a file including size, creation time, and modification time")]
    [return: Description("JSON object containing file information")]
    public async Task<string> GetFileInfoAsync(
        [Description("The absolute or relative path to the file")] string filePath,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogDebug("Getting file info via SK function: {FilePath}", filePath);
            
            var validatedPath = ValidateAndResolvePath(filePath);
            
            if (!File.Exists(validatedPath))
            {
                throw new FileNotFoundException($"File not found: {validatedPath}");
            }

            var fileInfo = new FileInfo(validatedPath);
            var info = new
            {
                Path = validatedPath,
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                CreatedTime = fileInfo.CreationTime,
                ModifiedTime = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension,
                Directory = fileInfo.DirectoryName,
                IsReadOnly = fileInfo.IsReadOnly
            };

            var result = System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogDebug("Retrieved file info for {FilePath}", validatedPath);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info {FilePath} via SK function", filePath);
            throw new KernelException($"Get file info failed: {ex.Message}", ex);
        }
    }

    private string ValidateAndResolvePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        // Resolve relative paths
        string resolvedPath;
        if (Path.IsPathRooted(filePath))
        {
            resolvedPath = filePath;
        }
        else
        {
            resolvedPath = Path.GetFullPath(filePath);
        }

        // Basic security validation - prevent directory traversal
        var normalizedPath = Path.GetFullPath(resolvedPath);
        var currentDirectory = Directory.GetCurrentDirectory();
        
        // Allow access to current directory and subdirectories, but be restrictive
        if (!normalizedPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            // Allow some common development paths
            var allowedRoots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.GetTempPath(),
                "/tmp", // Linux/macOS temp directory
                "C:\\temp" // Windows common temp directory
            };

            if (!allowedRoots.Any(root => normalizedPath.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Attempted access to restricted path: {Path}", normalizedPath);
                throw new UnauthorizedAccessException($"Access to path is restricted: {normalizedPath}");
            }
        }

        return normalizedPath;
    }
}