using DotnetPrompt.Application.Execution;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetPrompt.UnitTests.Application.Execution;

public class WorkflowEngineTests
{
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IVariableResolver> _mockVariableResolver;
    private readonly Mock<IStepExecutor> _mockStepExecutor;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockVariableResolver = new Mock<IVariableResolver>();
        _mockStepExecutor = new Mock<IStepExecutor>();
        
        _mockStepExecutor.Setup(e => e.StepType).Returns("prompt");
        
        var stepExecutors = new[] { _mockStepExecutor.Object };
        _workflowEngine = new WorkflowEngine(_mockLogger.Object, _mockVariableResolver.Object, stepExecutors);
    }

    [Fact]
    public async Task ExecuteAsync_WithSimpleWorkflow_ExecutesSuccessfully()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();

        _mockStepExecutor.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new StepExecutionResult(true, "Mock response", null, TimeSpan.FromMilliseconds(100)));

        // Act
        var result = await _workflowEngine.ExecuteAsync(workflow, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Workflow executed successfully", result.Output);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepFailure_ReturnsFailure()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();

        _mockStepExecutor.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new StepExecutionResult(false, null, "Step failed for testing", TimeSpan.FromMilliseconds(50)));

        // Act
        var result = await _workflowEngine.ExecuteAsync(workflow, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Step 'main_prompt' failed", result.ErrorMessage);
        Assert.Contains("Step failed for testing", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyWorkflow_ReturnsSuccessWithNoSteps()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "empty-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "" // Empty content
            }
        };

        var context = new WorkflowExecutionContext();

        // Act
        var result = await _workflowEngine.ExecuteAsync(workflow, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("No steps to execute", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ReturnsCancelled()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();
        var cancellationTokenSource = new CancellationTokenSource();

        _mockStepExecutor.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .Returns(async (WorkflowStep step, WorkflowExecutionContext ctx, CancellationToken ct) =>
                         {
                             // Simulate cancellation during step execution
                             cancellationTokenSource.Cancel();
                             ct.ThrowIfCancellationRequested();
                             return new StepExecutionResult(true, "Should not reach here");
                         });

        // Act
        var result = await _workflowEngine.ExecuteAsync(workflow, context, cancellationTokenSource.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("canceled", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_WithValidWorkflow_ReturnsValid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();

        _mockStepExecutor.Setup(e => e.ValidateAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new StepValidationResult(true));

        // Act
        var result = await _workflowEngine.ValidateAsync(workflow, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidStep_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();

        _mockStepExecutor.Setup(e => e.ValidateAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new StepValidationResult(false, new[] { "Step validation failed" }));

        // Act
        var result = await _workflowEngine.ValidateAsync(workflow, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Contains("Step validation failed", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateAsync_WithUnsupportedStepType_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();

        // Create engine with no step executors to simulate unsupported step type
        var emptyWorkflowEngine = new WorkflowEngine(_mockLogger.Object, _mockVariableResolver.Object, Array.Empty<IStepExecutor>());

        // Act
        var result = await emptyWorkflowEngine.ValidateAsync(workflow, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.Errors);
        Assert.Contains("Unsupported step type 'prompt'", result.Errors[0]);
    }

    [Fact]
    public async Task ExecuteAsync_StoresStepResultInOutputVariable()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Content = new WorkflowContent
            {
                RawMarkdown = "Test prompt content"
            }
        };

        var context = new WorkflowExecutionContext();
        var stepResult = "Test step result";

        _mockStepExecutor.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowStep>(), It.IsAny<WorkflowExecutionContext>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new StepExecutionResult(true, stepResult));

        // Act
        var result = await _workflowEngine.ExecuteAsync(workflow, context);

        // Assert
        Assert.True(result.Success);
        
        // Verify the result was stored in the expected output variable
        Assert.True(context.Variables.ContainsKey("workflow_result"));
        Assert.Equal(stepResult, context.Variables["workflow_result"]);
    }
}