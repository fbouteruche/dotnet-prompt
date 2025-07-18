using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Moq;
using Xunit;
using DotnetPrompt.Core.Interfaces;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel;

public class SemanticKernelOrchestratorTests
{
    private readonly Mock<IKernelFactory> _mockKernelFactory;
    private readonly Mock<IPromptTemplateFactory> _mockHandlebarsFactory;
    private readonly Mock<ILogger<SemanticKernelOrchestrator>> _mockLogger;
    private readonly SemanticKernelOrchestrator _orchestrator;

    public SemanticKernelOrchestratorTests()
    {
        _mockKernelFactory = new Mock<IKernelFactory>();
        _mockHandlebarsFactory = new Mock<IPromptTemplateFactory>();
        _mockLogger = new Mock<ILogger<SemanticKernelOrchestrator>>();
        
        _orchestrator = new SemanticKernelOrchestrator(
            _mockKernelFactory.Object,
            _mockHandlebarsFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateWorkflowAsync_WithEmptyContent_ReturnsInvalidResult()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "ollama/test-model",
            Content = new WorkflowContent { RawMarkdown = "" }
        };
        
        var context = new WorkflowExecutionContext();

        // Act
        var result = await _orchestrator.ValidateWorkflowAsync(workflow, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Workflow content cannot be empty", result.Errors!);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithNullContent_ReturnsFailureResult()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "gpt-4o",
            Content = null
        };
        
        var context = new WorkflowExecutionContext();

        // Act
        var result = await _orchestrator.ExecuteWorkflowAsync(workflow, context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SK execution failed", result.ErrorMessage!);
    }

    [Fact]
    public async Task GetChatHistoryAsync_WithNewWorkflowId_ReturnsEmptyHistory()
    {
        // Arrange
        var workflowId = "new-workflow-123";

        // Act
        var history = await _orchestrator.GetChatHistoryAsync(workflowId);

        // Assert
        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task SaveChatHistoryAsync_ThenGetChatHistoryAsync_ReturnsSavedHistory()
    {
        // Arrange
        var workflowId = "test-workflow-123";
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Test message");

        // Act
        await _orchestrator.SaveChatHistoryAsync(workflowId, chatHistory);
        var retrievedHistory = await _orchestrator.GetChatHistoryAsync(workflowId);

        // Assert
        Assert.NotNull(retrievedHistory);
        Assert.Single(retrievedHistory);
        Assert.Equal("Test message", retrievedHistory.First().Content!);
    }

    [Fact]
    public void GetKernel_WithoutInitialization_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _orchestrator.GetKernel());
        Assert.Contains("Kernel not initialized", exception.Message);
    }
}