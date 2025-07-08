using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Text.Json;
using Xunit;

namespace DotnetPrompt.IntegrationTests.FileSystem;

public class FileSystemPluginIntegrationTests : IDisposable
{
    private readonly FileSystemPlugin _plugin;
    private readonly string _testDirectory;

    public FileSystemPluginIntegrationTests()
    {
        // Create test directory in a predictable location
        _testDirectory = Path.Combine(Path.GetTempPath(), "dotnet-prompt-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Configure options to allow the test directory
        var options = new FileSystemOptions
        {
            AllowedDirectories = new[] { _testDirectory },
            BlockedDirectories = Array.Empty<string>(),
            MaxFileSizeBytes = 1024 * 1024,
            MaxFilesPerOperation = 100,
            EnableAuditLogging = false, // Disable for cleaner test output
            WorkingDirectoryContext = _testDirectory
        };

        var logger = new MockLogger<FileSystemPlugin>();
        _plugin = new FileSystemPlugin(logger, Options.Create(options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task FileSystemWorkflow_CreateWriteReadDelete_WorksCorrectly()
    {
        // Create a subdirectory
        var subDir = Path.Combine(_testDirectory, "test-subdir");
        var createResult = await _plugin.CreateDirectoryAsync(subDir);
        Assert.Contains("Successfully created directory", createResult);
        Assert.True(Directory.Exists(subDir));

        // Write a file
        var testFile = Path.Combine(subDir, "test.txt");
        var content = "Hello, World! This is a test file.";
        var writeResult = await _plugin.WriteFileAsync(testFile, content);
        Assert.Contains("Successfully wrote", writeResult);

        // Check if file exists
        var exists = await _plugin.FileExistsAsync(testFile);
        Assert.True(exists);

        // Read the file
        var readContent = await _plugin.ReadFileAsync(testFile);
        Assert.Equal(content, readContent);

        // Get file info
        var fileInfoJson = await _plugin.GetFileInfoAsync(testFile);
        var fileInfo = JsonSerializer.Deserialize<JsonElement>(fileInfoJson);
        Assert.Equal("test.txt", fileInfo.GetProperty("Name").GetString());
        Assert.Equal(".txt", fileInfo.GetProperty("Extension").GetString());
        Assert.True(fileInfo.GetProperty("Size").GetInt64() > 0);

        // List directory
        var listingJson = await _plugin.ListDirectoryAsync(subDir);
        var listing = JsonSerializer.Deserialize<DirectoryListingResult>(listingJson);
        Assert.Single(listing.Items);
        Assert.Equal("test.txt", listing.Items[0].Name);
        Assert.Equal("file", listing.Items[0].Type);

        // Copy the file
        var copyTarget = Path.Combine(subDir, "test-copy.txt");
        var copyResult = await _plugin.CopyFileAsync(testFile, copyTarget);
        Assert.Contains("Successfully copied", copyResult);
        Assert.True(File.Exists(copyTarget));

        var copiedContent = await _plugin.ReadFileAsync(copyTarget);
        Assert.Equal(content, copiedContent);
    }

    [Fact]
    public async Task FileSystemPlugin_SecurityValidation_RestrictsUnauthorizedAccess()
    {
        // Try to access a file outside the allowed directory
        var unauthorizedFile = Path.Combine(Path.GetTempPath(), "unauthorized.txt");
        
        var ex = await Assert.ThrowsAsync<KernelException>(() => 
            _plugin.ReadFileAsync(unauthorizedFile));
        Assert.Contains("Access to path is restricted", ex.Message);
    }

    [Fact]
    public async Task FileSystemPlugin_DirectoryListing_WithPatternFiltering_WorksCorrectly()
    {
        // Create test files with different extensions
        await _plugin.WriteFileAsync(Path.Combine(_testDirectory, "file1.txt"), "content1");
        await _plugin.WriteFileAsync(Path.Combine(_testDirectory, "file2.cs"), "content2");
        await _plugin.WriteFileAsync(Path.Combine(_testDirectory, "file3.txt"), "content3");

        // List all files
        var allFilesJson = await _plugin.ListDirectoryAsync(_testDirectory);
        var allFiles = JsonSerializer.Deserialize<DirectoryListingResult>(allFilesJson);
        Assert.Equal(3, allFiles.TotalFiles);

        // List only .txt files
        var txtFilesJson = await _plugin.ListDirectoryAsync(_testDirectory, "*.txt");
        var txtFiles = JsonSerializer.Deserialize<DirectoryListingResult>(txtFilesJson);
        Assert.Equal(2, txtFiles.Items.Where(i => i.Type == "file").Count());
        Assert.All(txtFiles.Items.Where(i => i.Type == "file"), 
            item => Assert.EndsWith(".txt", item.Name));
    }
}

// Mock logger implementation for tests
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}