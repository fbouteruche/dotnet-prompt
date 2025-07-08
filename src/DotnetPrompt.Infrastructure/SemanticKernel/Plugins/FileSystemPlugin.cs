using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Comprehensive Semantic Kernel plugin for file system operations with security and working directory context
/// </summary>
public class FileSystemPlugin
{
    private readonly ILogger<FileSystemPlugin> _logger;
    private readonly FileSystemOptions _options;
    private readonly FileSystemSecurityPolicy _securityPolicy;

    public FileSystemPlugin(ILogger<FileSystemPlugin> logger, IOptions<FileSystemOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _securityPolicy = new FileSystemSecurityPolicy();
    }

    [KernelFunction("file_read")]
    [Description("Read file content with security validation and encoding support")]
    [return: Description("File content as string")]
    public async Task<string> ReadFileAsync(
        [Description("Absolute or relative path to the file")] string filePath,
        [Description("File encoding (default: UTF-8)")] string encoding = "utf-8",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Reading file via SK function: {FilePath}", filePath);
            
            // Validate file path with enhanced security
            var validatedPath = ValidateAndResolvePath(filePath);
            
            if (!File.Exists(validatedPath))
            {
                throw new FileNotFoundException($"File not found: {validatedPath}");
            }

            // Check file size against options
            var fileInfo = new FileInfo(validatedPath);
            if (fileInfo.Length > _options.MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File too large: {fileInfo.Length} bytes. Maximum allowed: {_options.MaxFileSizeBytes} bytes");
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

    [KernelFunction("file_write")]
    [Description("Write content to file with automatic directory creation")]
    [return: Description("Success message with file path and size information")]
    public async Task<string> WriteFileAsync(
        [Description("Absolute or relative path to the file")] string filePath,
        [Description("Content to write to the file")] string content,
        [Description("Create parent directories if they don't exist")] bool createDirectories = true,
        [Description("File encoding (default: UTF-8)")] string encoding = "utf-8",
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

            // Create directory if it doesn't exist and createDirectories is true
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                if (createDirectories)
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogDebug("Created directory: {Directory}", directory);
                }
                else
                {
                    throw new DirectoryNotFoundException($"Directory does not exist and createDirectories is false: {directory}");
                }
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

    [KernelFunction("file_exists")]
    [Description("Check if a file exists at the specified path")]
    [return: Description("True if the file exists, false otherwise")]
    public async Task<bool> FileExistsAsync(
        [Description("Absolute or relative path to check")] string filePath,
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

    [KernelFunction("list_directory")]
    [Description("List directory contents with pattern filtering and recursion")]
    [return: Description("JSON object containing directory listing with file metadata")]
    public async Task<string> ListDirectoryAsync(
        [Description("Directory path to list")] string directoryPath,
        [Description("File pattern filter (e.g., *.cs, *.json)")] string pattern = "*",
        [Description("Include subdirectories")] bool recursive = false,
        [Description("Include hidden files")] bool includeHidden = false,
        [Description("Maximum recursion depth (0 = unlimited)")] int maxDepth = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Listing directory via SK function: {DirectoryPath}", directoryPath);
            
            var validatedPath = ValidateAndResolvePath(directoryPath);
            
            if (!Directory.Exists(validatedPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {validatedPath}");
            }

            var items = new List<FileSystemItem>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            try
            {
                // Get directories
                var directories = Directory.GetDirectories(validatedPath, "*", searchOption);
                foreach (var dir in directories.Take(_options.MaxFilesPerOperation))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if (!includeHidden && dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;
                        
                    items.Add(new FileSystemItem(
                        dirInfo.Name,
                        dir,
                        "directory",
                        0,
                        dirInfo.CreationTime,
                        dirInfo.LastWriteTime,
                        null,
                        dirInfo.Attributes.HasFlag(FileAttributes.Hidden)
                    ));
                }

                // Get files
                var files = Directory.GetFiles(validatedPath, pattern, searchOption);
                foreach (var file in files.Take(_options.MaxFilesPerOperation - items.Count))
                {
                    var fileInfo = new FileInfo(file);
                    if (!includeHidden && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;
                        
                    items.Add(new FileSystemItem(
                        fileInfo.Name,
                        file,
                        "file",
                        fileInfo.Length,
                        fileInfo.CreationTime,
                        fileInfo.LastWriteTime,
                        fileInfo.Extension,
                        fileInfo.Attributes.HasFlag(FileAttributes.Hidden)
                    ));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied while listing directory {DirectoryPath}", validatedPath);
                throw new KernelException($"Access denied to directory: {validatedPath}", ex);
            }

            var totalFiles = items.Count(i => i.Type == "file");
            var totalDirectories = items.Count(i => i.Type == "directory");
            var totalSize = items.Where(i => i.Type == "file").Sum(i => i.SizeBytes);
            var truncated = items.Count >= _options.MaxFilesPerOperation;

            var result = new DirectoryListingResult(
                validatedPath,
                items.ToArray(),
                totalFiles,
                totalDirectories,
                totalSize,
                truncated
            );

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("Successfully listed directory {DirectoryPath} ({ItemCount} items) in {Duration}ms", 
                validatedPath, items.Count, stopwatch.ElapsedMilliseconds);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory {DirectoryPath} via SK function", directoryPath);
            throw new KernelException($"Directory listing failed: {ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("copy_file")]
    [Description("Copy file to destination with safety controls")]
    [return: Description("Success message with operation details")]
    public async Task<string> CopyFileAsync(
        [Description("Source file path")] string sourcePath,
        [Description("Destination file path")] string destinationPath,
        [Description("Allow overwriting destination")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Copying file via SK function: {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
            
            var validatedSourcePath = ValidateAndResolvePath(sourcePath);
            var validatedDestinationPath = ValidateAndResolvePath(destinationPath);
            
            if (!File.Exists(validatedSourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {validatedSourcePath}");
            }

            if (File.Exists(validatedDestinationPath) && !overwrite)
            {
                throw new InvalidOperationException($"Destination file exists and overwrite is disabled: {validatedDestinationPath}");
            }

            // Create destination directory if needed
            var destinationDirectory = Path.GetDirectoryName(validatedDestinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
                _logger.LogDebug("Created destination directory: {Directory}", destinationDirectory);
            }

            // Check source file size
            var sourceInfo = new FileInfo(validatedSourcePath);
            if (sourceInfo.Length > _options.MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"Source file too large: {sourceInfo.Length} bytes. Maximum allowed: {_options.MaxFileSizeBytes} bytes");
            }

            File.Copy(validatedSourcePath, validatedDestinationPath, overwrite);
            
            var destinationInfo = new FileInfo(validatedDestinationPath);
            var result = $"Successfully copied {sourceInfo.Length} bytes from {validatedSourcePath} to {validatedDestinationPath} in {stopwatch.ElapsedMilliseconds}ms";
            
            _logger.LogInformation("Successfully copied file from {SourcePath} to {DestinationPath} ({Size} bytes) in {Duration}ms", 
                validatedSourcePath, validatedDestinationPath, sourceInfo.Length, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file from {SourcePath} to {DestinationPath} via SK function", sourcePath, destinationPath);
            throw new KernelException($"File copy failed: {ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("create_directory")]
    [Description("Create directory structure")]
    [return: Description("Success message with directory path")]
    public async Task<string> CreateDirectoryAsync(
        [Description("Directory path to create")] string directoryPath,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        
        try
        {
            _logger.LogInformation("Creating directory via SK function: {DirectoryPath}", directoryPath);
            
            var validatedPath = ValidateAndResolvePath(directoryPath);
            
            if (Directory.Exists(validatedPath))
            {
                return $"Directory already exists: {validatedPath}";
            }

            Directory.CreateDirectory(validatedPath);
            
            var result = $"Successfully created directory: {validatedPath}";
            
            _logger.LogInformation("Successfully created directory {DirectoryPath}", validatedPath);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory {DirectoryPath} via SK function", directoryPath);
            throw new KernelException($"Directory creation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validate and resolve path with enhanced security using working directory context
    /// </summary>
    private string ValidateAndResolvePath(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(inputPath));
        }

        // Get working directory context
        var workingDirectory = _securityPolicy.GetWorkingDirectory(_options.WorkingDirectoryContext);

        // Resolve relative paths against working directory
        string resolvedPath;
        if (Path.IsPathRooted(inputPath))
        {
            resolvedPath = inputPath;
        }
        else
        {
            resolvedPath = Path.Combine(workingDirectory, inputPath);
        }

        var normalizedPath = Path.GetFullPath(resolvedPath);

        // Use security policy for validation
        if (!_securityPolicy.IsPathAllowed(inputPath, workingDirectory, _options))
        {
            _logger.LogWarning("Access denied to path: {Path} (resolved: {ResolvedPath})", inputPath, normalizedPath);
            throw new UnauthorizedAccessException($"Access to path is restricted: {normalizedPath}");
        }

        return normalizedPath;
    }
}