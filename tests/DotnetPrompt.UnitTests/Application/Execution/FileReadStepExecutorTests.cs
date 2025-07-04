using DotnetPrompt.Application.Execution.Steps;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetPrompt.UnitTests.Application.Execution;

public class FileReadStepExecutorTests
{
    private readonly Mock<ILogger<FileReadStepExecutor>> _mockLogger;
    private readonly Mock<IVariableResolver> _mockVariableResolver;
    private readonly FileReadStepExecutor _executor;

    public FileReadStepExecutorTests()
    {
        _mockLogger = new Mock<ILogger<FileReadStepExecutor>>();
        _mockVariableResolver = new Mock<IVariableResolver>();
        _executor = new FileReadStepExecutor(_mockLogger.Object, _mockVariableResolver.Object);
    }

    [Fact]
    public void StepType_ReturnsCorrectType()
    {
        // Act & Assert
        Assert.Equal("file_read", _executor.StepType);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFile_ReturnsSuccess()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var testContent = "Test file content";
        await File.WriteAllTextAsync(tempFile, testContent);

        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { { "path", tempFile } }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ResolveVariables(tempFile, context))
                           .Returns(tempFile);

        try
        {
            // Act
            var result = await _executor.ExecuteAsync(step, context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(testContent, result.Result);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = "/non/existent/file.txt";
        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { { "path", nonExistentPath } }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ResolveVariables(nonExistentPath, context))
                           .Returns(nonExistentPath);

        // Act
        var result = await _executor.ExecuteAsync(step, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithRelativePath_ResolvesAgainstWorkingDirectory()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var fileName = Path.GetRandomFileName();
        var relativePath = fileName;
        var fullPath = Path.Combine(tempDir, fileName);
        var testContent = "Test relative path content";
        
        await File.WriteAllTextAsync(fullPath, testContent);

        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { { "path", relativePath } }
        };

        var context = new WorkflowExecutionContext
        {
            WorkingDirectory = tempDir
        };

        _mockVariableResolver.Setup(v => v.ResolveVariables(relativePath, context))
                           .Returns(relativePath);

        try
        {
            // Act
            var result = await _executor.ExecuteAsync(step, context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(testContent, result.Result);
        }
        finally
        {
            File.Delete(fullPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_ReturnsFailure()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { { "path", "" } }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ResolveVariables("", context))
                           .Returns("");

        // Act
        var result = await _executor.ExecuteAsync(step, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File path is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_WithValidStep_ReturnsValid()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { { "path", "/some/path.txt" } }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ValidateTemplate("/some/path.txt", context))
                           .Returns(new VariableValidationResult(true));

        _mockVariableResolver.Setup(v => v.ResolveVariables("/some/path.txt", context))
                           .Returns("/some/path.txt");

        // Act
        var result = await _executor.ValidateAsync(step, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingPath_ReturnsInvalid()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "read_test",
            Type = "file_read",
            Properties = { } // Missing path property
        };

        var context = new WorkflowExecutionContext();

        // Act
        var result = await _executor.ValidateAsync(step, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Contains("Required property 'path' is missing", result.Errors[0]);
    }
}