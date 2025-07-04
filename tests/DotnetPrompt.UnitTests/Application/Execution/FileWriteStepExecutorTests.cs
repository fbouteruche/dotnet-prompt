using DotnetPrompt.Application.Execution.Steps;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetPrompt.UnitTests.Application.Execution;

public class FileWriteStepExecutorTests
{
    private readonly Mock<ILogger<FileWriteStepExecutor>> _mockLogger;
    private readonly Mock<IVariableResolver> _mockVariableResolver;
    private readonly FileWriteStepExecutor _executor;

    public FileWriteStepExecutorTests()
    {
        _mockLogger = new Mock<ILogger<FileWriteStepExecutor>>();
        _mockVariableResolver = new Mock<IVariableResolver>();
        _executor = new FileWriteStepExecutor(_mockLogger.Object, _mockVariableResolver.Object);
    }

    [Fact]
    public void StepType_ReturnsCorrectType()
    {
        // Act & Assert
        Assert.Equal("file_write", _executor.StepType);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidPath_WritesFileSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var testContent = "Test file content for writing";

        var step = new WorkflowStep
        {
            Name = "write_test",
            Type = "file_write",
            Properties = { 
                { "path", tempFile },
                { "content", testContent }
            }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ResolveVariables(tempFile, context))
                           .Returns(tempFile);
        _mockVariableResolver.Setup(v => v.ResolveVariables(testContent, context))
                           .Returns(testContent);

        try
        {
            // Act
            var result = await _executor.ExecuteAsync(step, context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(tempFile, result.Result);
            Assert.Null(result.ErrorMessage);

            // Verify file was actually written
            var writtenContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(testContent, writtenContent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithNewDirectory_CreatesDirectoryAndFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var filePath = Path.Combine(tempDir, "test.txt");
        var testContent = "Test content in new directory";

        var step = new WorkflowStep
        {
            Name = "write_test",
            Type = "file_write",
            Properties = { 
                { "path", filePath },
                { "content", testContent }
            }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ResolveVariables(filePath, context))
                           .Returns(filePath);
        _mockVariableResolver.Setup(v => v.ResolveVariables(testContent, context))
                           .Returns(testContent);

        try
        {
            // Act
            var result = await _executor.ExecuteAsync(step, context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(filePath, result.Result);
            
            // Verify directory and file were created
            Assert.True(Directory.Exists(tempDir));
            Assert.True(File.Exists(filePath));
            
            var writtenContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal(testContent, writtenContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
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

        var step = new WorkflowStep
        {
            Name = "write_test",
            Type = "file_write",
            Properties = { 
                { "path", relativePath },
                { "content", testContent }
            }
        };

        var context = new WorkflowExecutionContext
        {
            WorkingDirectory = tempDir
        };

        _mockVariableResolver.Setup(v => v.ResolveVariables(relativePath, context))
                           .Returns(relativePath);
        _mockVariableResolver.Setup(v => v.ResolveVariables(testContent, context))
                           .Returns(testContent);

        try
        {
            // Act
            var result = await _executor.ExecuteAsync(step, context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(fullPath, result.Result);
            
            // Verify file was written to correct location
            var writtenContent = await File.ReadAllTextAsync(fullPath);
            Assert.Equal(testContent, writtenContent);
        }
        finally
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_ReturnsFailure()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "write_test",
            Type = "file_write",
            Properties = { 
                { "path", "" },
                { "content", "some content" }
            }
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
            Name = "write_test",
            Type = "file_write",
            Properties = { 
                { "path", "/some/path.txt" },
                { "content", "test content" }
            }
        };

        var context = new WorkflowExecutionContext();

        _mockVariableResolver.Setup(v => v.ValidateTemplate("/some/path.txt", context))
                           .Returns(new VariableValidationResult(true));
        _mockVariableResolver.Setup(v => v.ValidateTemplate("test content", context))
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
    public async Task ValidateAsync_WithMissingRequiredProperties_ReturnsInvalid()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "write_test",
            Type = "file_write",
            Properties = { } // Missing both path and content
        };

        var context = new WorkflowExecutionContext();

        // Act
        var result = await _executor.ValidateAsync(step, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Required property 'path' is missing"));
        Assert.Contains(result.Errors, e => e.Contains("Required property 'content' is missing"));
    }
}