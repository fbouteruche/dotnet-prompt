using DotnetPrompt.Application.Services;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetPrompt.UnitTests.Application;

public class WorkflowServiceTests
{
    private readonly Mock<ILogger<WorkflowService>> _mockLogger;
    private readonly Mock<IDotpromptParser> _mockParser;
    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowService>>();
        _mockParser = new Mock<IDotpromptParser>();
        _workflowService = new WorkflowService(_mockLogger.Object, _mockParser.Object);
    }

    [Fact]
    public async Task ValidateAsync_NonExistentFile_ReturnsInvalid()
    {
        // Arrange
        var nonExistentFile = "non-existent.prompt.md";
        var dotpromptResult = DotpromptValidationResult.Invalid(new DotpromptValidationError
        {
            Message = $"Workflow file not found: {nonExistentFile}",
            ErrorCode = "FILE_NOT_FOUND"
        });
        
        _mockParser.Setup(p => p.ValidateFileAsync(nonExistentFile, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(dotpromptResult);

        // Act
        var result = await _workflowService.ValidateAsync(nonExistentFile);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateAsync_InvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test content");

        var dotpromptResult = DotpromptValidationResult.Invalid(new DotpromptValidationError
        {
            Message = "Workflow file must have .prompt.md extension",
            ErrorCode = "INVALID_EXTENSION"
        });
        
        _mockParser.Setup(p => p.ValidateFileAsync(tempFile, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(dotpromptResult);

        try
        {
            // Act
            var result = await _workflowService.ValidateAsync(tempFile);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Contains(".prompt.md extension", result.Errors[0]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidateAsync_ValidFile_ReturnsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var validWorkflowFile = Path.ChangeExtension(tempFile, ".prompt.md");
        await File.WriteAllTextAsync(validWorkflowFile, "# Valid workflow content");

        _mockParser.Setup(p => p.ValidateFileAsync(validWorkflowFile, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(DotpromptValidationResult.Valid());

        try
        {
            // Act
            var result = await _workflowService.ValidateAsync(validWorkflowFile);

            // Assert
            Assert.True(result.IsValid);
        }
        finally
        {
            File.Delete(validWorkflowFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var nonExistentFile = "non-existent.prompt.md";
        var options = new WorkflowExecutionOptions();

        _mockParser.Setup(p => p.ParseFileAsync(nonExistentFile, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new DotpromptParseException($"Workflow file not found: {nonExistentFile}", nonExistentFile));

        // Act
        var result = await _workflowService.ExecuteAsync(nonExistentFile, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_ValidatesOnly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var validWorkflowFile = Path.ChangeExtension(tempFile, ".prompt.md");
        await File.WriteAllTextAsync(validWorkflowFile, "# Valid workflow content");
        var options = new WorkflowExecutionOptions(DryRun: true);

        _mockParser.Setup(p => p.ValidateFileAsync(validWorkflowFile, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(DotpromptValidationResult.Valid());

        try
        {
            // Act
            var result = await _workflowService.ExecuteAsync(validWorkflowFile, options);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Dry run completed", result.Output ?? string.Empty);
        }
        finally
        {
            File.Delete(validWorkflowFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NormalRun_ReturnsFoundationMessage()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var validWorkflowFile = Path.ChangeExtension(tempFile, ".prompt.md");
        await File.WriteAllTextAsync(validWorkflowFile, "# Valid workflow content");
        var options = new WorkflowExecutionOptions(DryRun: false);

        var mockWorkflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            FilePath = validWorkflowFile,
            Content = new WorkflowContent { RawMarkdown = "# Valid workflow content" }
        };
        
        _mockParser.Setup(p => p.ParseFileAsync(validWorkflowFile, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);

        try
        {
            // Act
            var result = await _workflowService.ExecuteAsync(validWorkflowFile, options);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("CLI foundation established", result.Output ?? string.Empty);
        }
        finally
        {
            File.Delete(validWorkflowFile);
        }
    }
}