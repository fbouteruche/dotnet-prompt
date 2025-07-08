using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CoreWorkflowExecutionContext = DotnetPrompt.Core.Models.WorkflowExecutionContext;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel.Plugins;

public class SubWorkflowPluginTests : IDisposable
{
    private readonly SubWorkflowPlugin _plugin;
    private readonly Mock<IDotpromptParser> _mockParser;
    private readonly Mock<IWorkflowOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<SubWorkflowPlugin>> _mockLogger;
    private readonly string _testDirectory;

    public SubWorkflowPluginTests()
    {
        _mockParser = new Mock<IDotpromptParser>();
        _mockOrchestrator = new Mock<IWorkflowOrchestrator>();
        _mockLogger = new Mock<ILogger<SubWorkflowPlugin>>();
        
        _plugin = new SubWorkflowPlugin(
            _mockParser.Object, 
            _mockOrchestrator.Object, 
            _mockLogger.Object);

        // Create test directory for test workflows
        _testDirectory = Path.Combine(Path.GetTempPath(), "subworkflow-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithValidWorkflow_ReturnsSuccessResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "test.prompt.md");
        var parameters = """{"test_param": "test_value"}""";
        
        var mockWorkflow = CreateMockWorkflow("test-workflow");
        var mockExecutionResult = new WorkflowExecutionResult(
            Success: true,
            Output: "Sub-workflow completed successfully",
            ExecutionTime: TimeSpan.FromMilliseconds(100));

        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ExecuteWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExecutionResult);

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(workflowPath, parameters, true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Sub-workflow completed successfully", result.Output);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);

        // Verify parser was called with correct path
        _mockParser.Verify(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify orchestrator was called with workflow and context
        _mockOrchestrator.Verify(x => x.ExecuteWorkflowAsync(
            It.Is<DotpromptWorkflow>(w => w.Name == "test-workflow"),
            It.Is<CoreWorkflowExecutionContext>(c => c.Variables.ContainsKey("test_param")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithFailedExecution_ReturnsFailureResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "test.prompt.md");
        var parameters = "{}";
        
        var mockWorkflow = CreateMockWorkflow("test-workflow");
        var mockExecutionResult = new WorkflowExecutionResult(
            Success: false,
            ErrorMessage: "Sub-workflow execution failed",
            ExecutionTime: TimeSpan.FromMilliseconds(50));

        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ExecuteWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExecutionResult);

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(workflowPath, parameters, true);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Equal("Sub-workflow execution failed", result.ErrorMessage);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithInvalidParameters_ContinuesWithEmptyContext()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "test.prompt.md");
        var invalidParameters = """{"invalid": json}"""; // Invalid JSON
        
        var mockWorkflow = CreateMockWorkflow("test-workflow");
        var mockExecutionResult = new WorkflowExecutionResult(
            Success: true,
            Output: "Workflow completed",
            ExecutionTime: TimeSpan.FromMilliseconds(100));

        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ExecuteWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExecutionResult);

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(workflowPath, invalidParameters, true);

        // Assert - Should still succeed but with empty context due to invalid JSON
        Assert.True(result.Success);
        
        // Verify orchestrator was called with empty context (no parameters added due to JSON error)
        _mockOrchestrator.Verify(x => x.ExecuteWorkflowAsync(
            It.IsAny<DotpromptWorkflow>(),
            It.Is<CoreWorkflowExecutionContext>(c => c.Variables.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "nonexistent.prompt.md");
        
        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(workflowPath, "{}", true);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Contains("Sub-workflow execution failed", result.ErrorMessage);
        Assert.Contains("File not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSubWorkflowAsync_WithValidWorkflow_ReturnsValidResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "test.prompt.md");
        var parameters = """{"param1": "value1"}""";
        
        var mockWorkflow = CreateMockWorkflow("test-workflow");
        var mockValidationResult = new WorkflowValidationResult(
            IsValid: true,
            Errors: null,
            Warnings: null);

        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ValidateWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        // Act
        var result = await _plugin.ValidateSubWorkflowAsync(workflowPath, parameters);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Null(result.Warnings);

        // Verify validation was called
        _mockOrchestrator.Verify(x => x.ValidateWorkflowAsync(
            It.Is<DotpromptWorkflow>(w => w.Name == "test-workflow"),
            It.IsAny<CoreWorkflowExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSubWorkflowAsync_WithInvalidWorkflow_ReturnsInvalidResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "invalid.prompt.md");
        
        var mockWorkflow = CreateMockWorkflow("invalid-workflow");
        var mockValidationResult = new WorkflowValidationResult(
            IsValid: false,
            Errors: new[] { "Template syntax error", "Missing required parameter" },
            Warnings: new[] { "Deprecated syntax used" });

        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ValidateWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        // Act
        var result = await _plugin.ValidateSubWorkflowAsync(workflowPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Length);
        Assert.Contains("Template syntax error", result.Errors);
        Assert.Contains("Missing required parameter", result.Errors);
        Assert.NotNull(result.Warnings);
        Assert.Contains("Deprecated syntax used", result.Warnings);
    }

    [Fact]
    public async Task ValidateSubWorkflowAsync_WithException_ReturnsInvalidResult()
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "error.prompt.md");
        
        _mockParser.Setup(x => x.ParseFileAsync(workflowPath, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("Parse error"));

        // Act
        var result = await _plugin.ValidateSubWorkflowAsync(workflowPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Validation failed: Parse error", result.Errors[0]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"param1": "value1"}""")]
    [InlineData("""{"param1": "value1", "param2": 42, "param3": true}""")]
    public async Task ExecuteSubWorkflowAsync_WithVariousParameterFormats_ParsesCorrectly(string parameters)
    {
        // Arrange
        var workflowPath = Path.Combine(_testDirectory, "test.prompt.md");
        var mockWorkflow = CreateMockWorkflow("test-workflow");
        var mockExecutionResult = new WorkflowExecutionResult(Success: true, Output: "Success");

        _mockParser.Setup(x => x.ParseFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(mockWorkflow);
        
        _mockOrchestrator.Setup(x => x.ExecuteWorkflowAsync(
                It.IsAny<DotpromptWorkflow>(), 
                It.IsAny<CoreWorkflowExecutionContext>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockExecutionResult);

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(workflowPath, parameters, true);

        // Assert
        Assert.True(result.Success);
        
        // Verify that context was created (exact parameter count verification would require JSON parsing)
        _mockOrchestrator.Verify(x => x.ExecuteWorkflowAsync(
            It.IsAny<DotpromptWorkflow>(),
            It.IsAny<CoreWorkflowExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DotpromptWorkflow CreateMockWorkflow(string name)
    {
        return new DotpromptWorkflow
        {
            Name = name,
            Model = "gpt-4o",
            Content = new WorkflowContent 
            { 
                RawMarkdown = "Test workflow content",
                ParameterReferences = new HashSet<string>()
            },
            HasFrontmatter = true
        };
    }
}