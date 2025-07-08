using DotnetPrompt.Infrastructure.Filters;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Moq;
using System.Security;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Filters;

public class FileSystemSecurityFilterTests
{
    private readonly FileSystemSecurityFilter _filter;
    private readonly Mock<ILogger<FileSystemSecurityFilter>> _mockLogger;
    private readonly FileSystemOptions _options;
    private readonly Mock<FunctionInvocationContext> _mockContext;
    private readonly Mock<KernelFunction> _mockFunction;

    public FileSystemSecurityFilterTests()
    {
        _mockLogger = new Mock<ILogger<FileSystemSecurityFilter>>();
        _options = new FileSystemOptions
        {
            AllowedDirectories = new[] { Directory.GetCurrentDirectory() },
            BlockedDirectories = new[] { "bin", "obj", ".git" },
            MaxFileSizeBytes = 1024 * 1024,
            EnableAuditLogging = true
        };

        _filter = new FileSystemSecurityFilter(_mockLogger.Object, Options.Create(_options));
        
        _mockFunction = new Mock<KernelFunction>();
        _mockFunction.Setup(f => f.PluginName).Returns("FileSystem");
        _mockFunction.Setup(f => f.Name).Returns("file_read");

        _mockContext = new Mock<FunctionInvocationContext>();
        _mockContext.Setup(c => c.Function).Returns(_mockFunction.Object);
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments());
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithNonFileSystemPlugin_SkipsValidation()
    {
        // Arrange
        _mockFunction.Setup(f => f.PluginName).Returns("SomeOtherPlugin");
        var nextCalled = false;
        Task Next(FunctionInvocationContext context)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _filter.OnFunctionInvocationAsync(_mockContext.Object, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithFileReadFunction_ValidatesFilePath()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var testFile = Path.Combine(workingDirectory, "test.txt");
        
        _mockFunction.Setup(f => f.Name).Returns("file_read");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["filePath"] = testFile
        });

        var nextCalled = false;
        Task Next(FunctionInvocationContext context)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _filter.OnFunctionInvocationAsync(_mockContext.Object, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithUnauthorizedPath_ThrowsWorkflowExecutionException()
    {
        // Arrange
        var unauthorizedFile = Path.Combine(Path.GetTempPath(), "unauthorized.txt");
        
        _mockFunction.Setup(f => f.Name).Returns("file_read");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["filePath"] = unauthorizedFile
        });

        Task Next(FunctionInvocationContext context) => Task.CompletedTask;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WorkflowExecutionException>(() =>
            _filter.OnFunctionInvocationAsync(_mockContext.Object, Next));
        
        Assert.Contains("File system access denied", exception.Message);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithBlockedDirectory_ThrowsWorkflowExecutionException()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var blockedFile = Path.Combine(workingDirectory, "bin", "test.exe");
        
        _mockFunction.Setup(f => f.Name).Returns("file_read");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["filePath"] = blockedFile
        });

        Task Next(FunctionInvocationContext context) => Task.CompletedTask;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<WorkflowExecutionException>(() =>
            _filter.OnFunctionInvocationAsync(_mockContext.Object, Next));
        
        Assert.Contains("File system access denied", exception.Message);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithDirectoryFunction_ValidatesDirectoryPath()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var testDir = Path.Combine(workingDirectory, "testdir");
        
        _mockFunction.Setup(f => f.Name).Returns("list_directory");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["directoryPath"] = testDir
        });

        var nextCalled = false;
        Task Next(FunctionInvocationContext context)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _filter.OnFunctionInvocationAsync(_mockContext.Object, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithFileWriteAndOverwriteFalse_ValidatesExistingFile()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var testFile = Path.Combine(workingDirectory, "existing-test.txt");
        
        // Create the test file
        await File.WriteAllTextAsync(testFile, "existing content");
        
        try
        {
            _mockFunction.Setup(f => f.Name).Returns("file_write");
            _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
            {
                ["filePath"] = testFile,
                ["overwrite"] = false
            });

            Task Next(FunctionInvocationContext context) => Task.CompletedTask;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<WorkflowExecutionException>(() =>
                _filter.OnFunctionInvocationAsync(_mockContext.Object, Next));
            
            Assert.Contains("File system access denied", exception.Message);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithValidPathVariants_AcceptsAllVariants()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var testCases = new[]
        {
            ("file_read", "filePath"),
            ("file_read", "path"),
            ("file_read", "file_path"),
            ("copy_file", "source_path"),
            ("copy_file", "destination_path"),
            ("list_directory", "directoryPath"),
            ("list_directory", "directory_path")
        };

        foreach (var (functionName, paramName) in testCases)
        {
            var testPath = Path.Combine(workingDirectory, "test-file.txt");
            
            _mockFunction.Setup(f => f.Name).Returns(functionName);
            _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
            {
                [paramName] = testPath
            });

            var nextCalled = false;
            Task Next(FunctionInvocationContext context)
            {
                nextCalled = true;
                return Task.CompletedTask;
            }

            // Act
            await _filter.OnFunctionInvocationAsync(_mockContext.Object, Next);

            // Assert
            Assert.True(nextCalled, $"Function {functionName} with parameter {paramName} should have been allowed");
        }
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithAuditLoggingEnabled_LogsOperations()
    {
        // Arrange
        var workingDirectory = Directory.GetCurrentDirectory();
        var testFile = Path.Combine(workingDirectory, "audit-test.txt");
        
        _mockFunction.Setup(f => f.Name).Returns("file_read");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["filePath"] = testFile
        });

        Task Next(FunctionInvocationContext context) => Task.CompletedTask;

        // Act
        await _filter.OnFunctionInvocationAsync(_mockContext.Object, Next);

        // Assert
        // Verify that audit logging was called (checking for Information level logs)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File system operation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnFunctionInvocationAsync_WithFailure_LogsFailureWhenAuditEnabled()
    {
        // Arrange
        var unauthorizedFile = Path.Combine(Path.GetTempPath(), "unauthorized-audit.txt");
        
        _mockFunction.Setup(f => f.Name).Returns("file_read");
        _mockContext.Setup(c => c.Arguments).Returns(new KernelArguments
        {
            ["filePath"] = unauthorizedFile
        });

        Task Next(FunctionInvocationContext context) => Task.CompletedTask;

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowExecutionException>(() =>
            _filter.OnFunctionInvocationAsync(_mockContext.Object, Next));

        // Verify that failure was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File system operation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}