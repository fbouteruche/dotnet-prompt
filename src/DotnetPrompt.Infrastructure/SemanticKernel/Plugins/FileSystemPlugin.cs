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
        [Description("Absolute or relative path to the file")] string file_path,
        [Description("File encoding (default: UTF-8)")] string encoding = "utf-8",
        [Description("Maximum file size in MB (default: 10)")] int max_size_mb = 10,
        [Description("Maximum lines to read (0 = all)")] int max_lines = 0,
        [Description("Lines to skip from beginning")] int skip_lines = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Reading file via SK function: {FilePath}", file_path);
            
            // Validate file path with enhanced security
            var validatedPath = ValidateAndResolvePath(file_path);
            
            if (!File.Exists(validatedPath))
            {
                throw new FileNotFoundException($"File not found: {validatedPath}");
            }

            // Check file size against max_size_mb parameter (convert to bytes)
            var maxSizeBytes = max_size_mb * 1024 * 1024;
            var fileInfo = new FileInfo(validatedPath);
            if (fileInfo.Length > maxSizeBytes)
            {
                throw new InvalidOperationException($"File too large: {fileInfo.Length} bytes. Maximum allowed: {maxSizeBytes} bytes ({max_size_mb} MB)");
            }

            // Read file content with line control
            string content;
            if (max_lines > 0 || skip_lines > 0)
            {
                var lines = await File.ReadAllLinesAsync(validatedPath, cancellationToken);
                
                // Skip lines if requested
                if (skip_lines > 0)
                {
                    lines = lines.Skip(skip_lines).ToArray();
                }
                
                // Take only max_lines if specified
                if (max_lines > 0)
                {
                    lines = lines.Take(max_lines).ToArray();
                }
                
                content = string.Join(Environment.NewLine, lines);
            }
            else
            {
                content = await File.ReadAllTextAsync(validatedPath, cancellationToken);
            }
            
            _logger.LogInformation("Successfully read file {FilePath} ({Size} bytes) in {Duration}ms", 
                validatedPath, content.Length, stopwatch.ElapsedMilliseconds);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {FilePath} via SK function", file_path);
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
        [Description("Absolute or relative path to the file")] string file_path,
        [Description("Content to write to the file")] string content,
        [Description("File encoding (default: UTF-8)")] string encoding = "utf-8",
        [Description("Create backup before overwrite")] bool create_backup = true,
        [Description("Whether to overwrite existing files (default: true)")] bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Writing file via SK function: {FilePath}", file_path);
            
            // Validate file path
            var validatedPath = ValidateAndResolvePath(file_path);
            
            // Create backup if file exists and create_backup is true
            string? backupPath = null;
            if (File.Exists(validatedPath) && create_backup)
            {
                backupPath = $"{validatedPath}.backup.{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                File.Copy(validatedPath, backupPath);
                _logger.LogDebug("Created backup: {BackupPath}", backupPath);
            }
            
            // Check if file exists and overwrite flag
            if (File.Exists(validatedPath) && !overwrite)
            {
                throw new InvalidOperationException($"File already exists and overwrite is disabled: {validatedPath}");
            }

            // Create directory if it doesn't exist (always create parent directories for write operations)
            var directory = Path.GetDirectoryName(validatedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }

            // Write file content
            await File.WriteAllTextAsync(validatedPath, content, cancellationToken);
            
            var fileInfo = new FileInfo(validatedPath);
            var result = backupPath != null 
                ? $"Successfully wrote {content.Length} characters to {validatedPath} ({fileInfo.Length} bytes) with backup at {backupPath} in {stopwatch.ElapsedMilliseconds}ms"
                : $"Successfully wrote {content.Length} characters to {validatedPath} ({fileInfo.Length} bytes) in {stopwatch.ElapsedMilliseconds}ms";
            
            _logger.LogInformation("Successfully wrote file {FilePath} ({Size} bytes) in {Duration}ms", 
                validatedPath, fileInfo.Length, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file {FilePath} via SK function", file_path);
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
        [Description("Absolute or relative path to check")] string file_path,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogDebug("Checking file existence via SK function: {FilePath}", file_path);
            
            var validatedPath = ValidateAndResolvePath(file_path);
            var exists = File.Exists(validatedPath);
            
            _logger.LogDebug("File {FilePath} exists: {Exists}", validatedPath, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence {FilePath} via SK function", file_path);
            return false;
        }
    }

    [KernelFunction("get_file_info")]
    [Description("Gets information about a file including size, creation time, and modification time")]
    [return: Description("JSON object containing file information")]
    public async Task<string> GetFileInfoAsync(
        [Description("The absolute or relative path to the file")] string file_path,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        try
        {
            _logger.LogDebug("Getting file info via SK function: {FilePath}", file_path);
            
            var validatedPath = ValidateAndResolvePath(file_path);
            
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
            _logger.LogError(ex, "Error getting file info {FilePath} via SK function", file_path);
            throw new KernelException($"Get file info failed: {ex.Message}", ex);
        }
    }

    [KernelFunction("list_directory")]
    [Description("List directory contents with pattern filtering and recursion")]
    [return: Description("JSON object containing directory listing with file metadata")]
    public async Task<string> ListDirectoryAsync(
        [Description("Directory path to list")] string directory_path,
        [Description("File pattern filter (e.g., *.cs, *.json)")] string pattern = "*",
        [Description("Include subdirectories")] bool recursive = false,
        [Description("Include hidden files")] bool include_hidden = false,
        [Description("Maximum recursion depth (0 = unlimited)")] int max_depth = 0,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Listing directory via SK function: {DirectoryPath}", directory_path);
            
            var validatedPath = ValidateAndResolvePath(directory_path);
            
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
                    if (!include_hidden && dirInfo.Attributes.HasFlag(FileAttributes.Hidden))
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
                    if (!include_hidden && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
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
            _logger.LogError(ex, "Error listing directory {DirectoryPath} via SK function", directory_path);
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
        [Description("Source file path")] string source_path,
        [Description("Destination file path")] string destination_path,
        [Description("Allow overwriting destination")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Copying file via SK function: {SourcePath} to {DestinationPath}", source_path, destination_path);
            
            var validatedSourcePath = ValidateAndResolvePath(source_path);
            var validatedDestinationPath = ValidateAndResolvePath(destination_path);
            
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
            _logger.LogError(ex, "Error copying file from {SourcePath} to {DestinationPath} via SK function", source_path, destination_path);
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
        [Description("Directory path to create")] string directory_path,
        [Description("Create parent directories")] bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async compliance for SK function
        
        try
        {
            _logger.LogInformation("Creating directory via SK function: {DirectoryPath}", directory_path);
            
            var validatedPath = ValidateAndResolvePath(directory_path);
            
            if (Directory.Exists(validatedPath))
            {
                return $"Directory already exists: {validatedPath}";
            }

            if (recursive)
            {
                Directory.CreateDirectory(validatedPath);
            }
            else
            {
                // Check if parent directory exists
                var parentDirectory = Path.GetDirectoryName(validatedPath);
                if (!string.IsNullOrEmpty(parentDirectory) && !Directory.Exists(parentDirectory))
                {
                    throw new DirectoryNotFoundException($"Parent directory does not exist and recursive is false: {parentDirectory}");
                }
                Directory.CreateDirectory(validatedPath);
            }
            
            var result = $"Successfully created directory: {validatedPath}";
            
            _logger.LogInformation("Successfully created directory {DirectoryPath}", validatedPath);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory {DirectoryPath} via SK function", directory_path);
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