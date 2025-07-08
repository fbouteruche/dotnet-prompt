using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using System.Text.Json;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel.Plugins;

public class FileSystemPluginTests : IDisposable
{
    private readonly FileSystemPlugin _plugin;
    private readonly Mock<ILogger<FileSystemPlugin>> _mockLogger;
    private readonly FileSystemOptions _options;
    private readonly string _testDirectory;

    public FileSystemPluginTests()
    {
        _mockLogger = new Mock<ILogger<FileSystemPlugin>>();
        
        // Create a test directory in a temporary location for tests
        _testDirectory = Path.Combine(Path.GetTempPath(), "dotnet-prompt-unit-tests", Guid.NewGuid().ToString());
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        Directory.CreateDirectory(_testDirectory);
        
        _options = new FileSystemOptions
        {
            AllowedDirectories = new[] { _testDirectory }, // Only allow test directory
            BlockedDirectories = Array.Empty<string>(), // No blocked directories for tests
            MaxFileSizeBytes = 1024 * 1024, // 1MB for tests
            MaxFilesPerOperation = 100,
            EnableAuditLogging = false, // Disable audit logging for cleaner test output
            WorkingDirectoryContext = _testDirectory // Set working directory context to test directory
        };

        _plugin = new FileSystemPlugin(_mockLogger.Object, Options.Create(_options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ReadFileAsync_WithValidFile_ReturnsContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var expectedContent = "Hello, World!";
        await File.WriteAllTextAsync(testFile, expectedContent);

        // Act
        var result = await _plugin.ReadFileAsync(testFile);

        // Assert
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ThrowsKernelException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KernelException>(() => _plugin.ReadFileAsync(nonExistentFile));
        Assert.Contains("File not found", ex.Message);
    }

    [Fact]
    public async Task WriteFileAsync_WithValidPath_CreatesFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "write-test.txt");
        var content = "Test content for writing";

        // Act
        var result = await _plugin.WriteFileAsync(testFile, content);

        // Assert
        Assert.True(File.Exists(testFile));
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(content, actualContent);
        Assert.Contains("Successfully wrote", result);
    }

    [Fact]
    public async Task WriteFileAsync_WithCreateDirectoriesTrue_CreatesParentDirectories()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "subdir", "test.txt");
        var content = "Test content";

        // Act
        var result = await _plugin.WriteFileAsync(testFile, content);

        // Assert
        Assert.True(File.Exists(testFile));
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(content, actualContent);
    }

    [Fact]
    public async Task WriteFileAsync_WithCreateBackupTrue_CreatesBackupFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "backup-test.txt");
        await File.WriteAllTextAsync(testFile, "original content");
        var newContent = "new content";

        // Act
        var result = await _plugin.WriteFileAsync(testFile, newContent, create_backup: true);

        // Assert
        Assert.True(File.Exists(testFile));
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(newContent, actualContent);
        
        // Check that backup file was created
        var backupFiles = Directory.GetFiles(_testDirectory, "backup-test.txt.backup.*");
        Assert.Single(backupFiles);
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "exists-test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        // Act
        var result = await _plugin.FileExistsAsync(testFile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "does-not-exist.txt");

        // Act
        var result = await _plugin.FileExistsAsync(testFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFileInfoAsync_WithValidFile_ReturnsFileInfo()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "info-test.txt");
        var content = "Test content for file info";
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var result = await _plugin.GetFileInfoAsync(testFile);

        // Assert
        Assert.NotNull(result);
        var fileInfo = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal(testFile, fileInfo.GetProperty("Path").GetString());
        Assert.Equal("info-test.txt", fileInfo.GetProperty("Name").GetString());
        Assert.Equal(".txt", fileInfo.GetProperty("Extension").GetString());
        Assert.True(fileInfo.GetProperty("Size").GetInt64() > 0);
    }

    [Fact]
    public async Task ListDirectoryAsync_WithValidDirectory_ReturnsDirectoryListing()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "list-test");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(subDir, "file2.cs"), "content2");
        Directory.CreateDirectory(Path.Combine(subDir, "subdir"));

        // Act
        var result = await _plugin.ListDirectoryAsync(subDir);

        // Assert
        Assert.NotNull(result);
        var listing = JsonSerializer.Deserialize<DirectoryListingResult>(result);
        Assert.NotNull(listing);
        Assert.Equal(subDir, listing.Path);
        Assert.True(listing.Items.Length >= 3); // 2 files + 1 directory
        Assert.Contains(listing.Items, item => item.Name == "file1.txt" && item.Type == "file");
        Assert.Contains(listing.Items, item => item.Name == "file2.cs" && item.Type == "file");
        Assert.Contains(listing.Items, item => item.Name == "subdir" && item.Type == "directory");
    }

    [Fact]
    public async Task ListDirectoryAsync_WithPattern_FiltersResults()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "pattern-test");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(subDir, "file2.cs"), "content2");
        await File.WriteAllTextAsync(Path.Combine(subDir, "file3.txt"), "content3");

        // Act
        var result = await _plugin.ListDirectoryAsync(subDir, "*.txt");

        // Assert
        Assert.NotNull(result);
        var listing = JsonSerializer.Deserialize<DirectoryListingResult>(result);
        Assert.NotNull(listing);
        Assert.Equal(2, listing.Items.Where(i => i.Type == "file").Count());
        Assert.All(listing.Items.Where(i => i.Type == "file"), item => 
            Assert.EndsWith(".txt", item.Name));
    }

    [Fact]
    public async Task CopyFileAsync_WithValidPaths_CopiesFile()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "source.txt");
        var destFile = Path.Combine(_testDirectory, "destination.txt");
        var content = "Content to copy";
        await File.WriteAllTextAsync(sourceFile, content);

        // Act
        var result = await _plugin.CopyFileAsync(sourceFile, destFile);

        // Assert
        Assert.True(File.Exists(destFile));
        var copiedContent = await File.ReadAllTextAsync(destFile);
        Assert.Equal(content, copiedContent);
        Assert.Contains("Successfully copied", result);
    }

    [Fact]
    public async Task CopyFileAsync_WithExistingDestinationAndOverwriteFalse_ThrowsException()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "source2.txt");
        var destFile = Path.Combine(_testDirectory, "destination2.txt");
        await File.WriteAllTextAsync(sourceFile, "source content");
        await File.WriteAllTextAsync(destFile, "existing content");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KernelException>(() => 
            _plugin.CopyFileAsync(sourceFile, destFile, false));
        Assert.Contains("overwrite is disabled", ex.Message);
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithValidPath_CreatesDirectory()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "new-directory");

        // Act
        var result = await _plugin.CreateDirectoryAsync(newDir);

        // Assert
        Assert.True(Directory.Exists(newDir));
        Assert.Contains("Successfully created directory", result);
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithExistingDirectory_ReturnsAlreadyExistsMessage()
    {
        // Arrange
        var existingDir = Path.Combine(_testDirectory, "existing-dir");
        Directory.CreateDirectory(existingDir);

        // Act
        var result = await _plugin.CreateDirectoryAsync(existingDir);

        // Assert
        Assert.Contains("Directory already exists", result);
    }

    [Fact]
    public async Task ReadFileAsync_WithPathOutsideWorkingDirectory_ThrowsKernelException()
    {
        // Arrange - Use a path outside any allowed directory
        var tempFile = Path.Combine(Path.GetTempPath(), "unauthorized-test.txt");
        await File.WriteAllTextAsync(tempFile, "unauthorized content");

        try
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<KernelException>(() => _plugin.ReadFileAsync(tempFile));
            Assert.Contains("Access to path is restricted", ex.Message);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

}