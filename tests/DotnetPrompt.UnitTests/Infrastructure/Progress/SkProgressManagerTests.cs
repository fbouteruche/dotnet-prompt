using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Progress;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Progress;

/// <summary>
/// Unit tests for SkProgressManager
/// </summary>
public class SkProgressManagerTests
{
    private readonly SkProgressManager _progressManager;
    private readonly NullLogger<SkProgressManager> _logger;

    public SkProgressManagerTests()
    {
        _logger = new NullLogger<SkProgressManager>();
        _progressManager = new SkProgressManager(_logger);
    }

    [Fact]
    public async Task SaveProgressAsync_WithValidData_ShouldSaveSuccessfully()
    {
        // Arrange
        var workflowId = "test-workflow-1";
        var context = new WorkflowExecutionContext
        {
            CurrentStep = 1,
            Variables = { ["test_var"] = "test_value" }
        };
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Test message");

        // Act
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        // Assert
        var loadedProgress = await _progressManager.LoadProgressAsync(workflowId);
        loadedProgress.Should().NotBeNull();
        loadedProgress.Value.Context.Should().NotBeNull();
        loadedProgress.Value.ChatHistory.Should().NotBeNull();
        loadedProgress.Value.Context!.CurrentStep.Should().Be(1);
        loadedProgress.Value.Context.Variables.Should().ContainKey("test_var");
    }

    [Fact]
    public async Task LoadProgressAsync_WithNonExistentWorkflow_ShouldReturnNull()
    {
        // Arrange
        var workflowId = "non-existent-workflow";

        // Act
        var result = await _progressManager.LoadProgressAsync(workflowId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TrackStepCompletionAsync_WithExistingProgress_ShouldUpdateProgress()
    {
        // Arrange
        var workflowId = "test-workflow-2";
        var context = new WorkflowExecutionContext
        {
            CurrentStep = 1,
            Variables = { ["test_var"] = "test_value" }
        };
        var chatHistory = new ChatHistory();
        
        // Save initial progress
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        var stepProgress = new StepProgress
        {
            StepName = "test_step",
            StepType = "function",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(500)
        };

        // Act
        await _progressManager.TrackStepCompletionAsync(workflowId, stepProgress);

        // Assert
        var loadedProgress = await _progressManager.LoadProgressAsync(workflowId);
        loadedProgress.Should().NotBeNull();
        loadedProgress.Value.Context.Should().NotBeNull();
        loadedProgress.Value.Context!.ExecutionHistory.Should().HaveCount(1);
        loadedProgress.Value.Context.ExecutionHistory[0].StepName.Should().Be("test_step");
        loadedProgress.Value.Context.ExecutionHistory[0].Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateWorkflowCompatibilityAsync_WithSameContent_ShouldReturnTrue()
    {
        // Arrange
        var workflowId = "test-workflow-3";
        var workflowContent = "Test workflow content";
        var context = new WorkflowExecutionContext();
        context.SetVariable("workflow_hash", ComputeTestHash(workflowContent));
        
        var chatHistory = new ChatHistory();
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        // Act
        var result = await _progressManager.ValidateWorkflowCompatibilityAsync(workflowId, workflowContent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAndLoadProgress_ShouldPreserveVariables()
    {
        // Arrange
        var workflowId = "test-debug";
        var context = new WorkflowExecutionContext();
        context.SetVariable("test_hash", "abc123");
        context.SetVariable("other_var", "other_value");
        
        var chatHistory = new ChatHistory();

        // Act
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);
        var loadedProgress = await _progressManager.LoadProgressAsync(workflowId);

        // Assert
        loadedProgress.Should().NotBeNull();
        var loadedContext = loadedProgress.Value.Context;
        loadedContext.Should().NotBeNull();
        
        // Use GetVariable method which handles type conversion
        var loadedHash = loadedContext!.GetVariable<string>("test_hash");
        var loadedOther = loadedContext.GetVariable<string>("other_var");
        
        loadedHash.Should().Be("abc123");
        loadedOther.Should().Be("other_value");
    }

    [Fact]
    public async Task GetAvailableConversationStatesAsync_ShouldReturnInProgressWorkflows()
    {
        // Arrange
        var workflowId1 = "test-workflow-5";
        var workflowId2 = "test-workflow-6";
        
        var context = new WorkflowExecutionContext();
        var chatHistory = new ChatHistory();
        
        await _progressManager.SaveProgressAsync(workflowId1, context, chatHistory);
        await _progressManager.SaveProgressAsync(workflowId2, context, chatHistory);

        // Act
        var result = await _progressManager.GetAvailableConversationStatesAsync();

        // Assert
        var states = result.ToList();
        states.Should().Contain(workflowId1);
        states.Should().Contain(workflowId2);
    }

    [Fact]
    public async Task CleanupOldProgressAsync_ShouldRemoveOldRecords()
    {
        // Arrange
        var workflowId = "test-workflow-7";
        var context = new WorkflowExecutionContext();
        var chatHistory = new ChatHistory();
        
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        // Act
        var cleanupCount = await _progressManager.CleanupOldProgressAsync(DateTime.UtcNow.AddMinutes(1));

        // Assert
        cleanupCount.Should().Be(1);
        
        // Verify the record is gone
        var loadedProgress = await _progressManager.LoadProgressAsync(workflowId);
        loadedProgress.Should().BeNull();
    }

    [Fact]
    public async Task TrackStepFailureAsync_ShouldCreateFailedStepProgress()
    {
        // Arrange
        var workflowId = "test-workflow-8";
        var context = new WorkflowExecutionContext();
        var chatHistory = new ChatHistory();
        
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        var exception = new InvalidOperationException("Test failure");

        // Act
        await _progressManager.TrackStepFailureAsync(workflowId, "failing_step", exception);

        // Assert
        var loadedProgress = await _progressManager.LoadProgressAsync(workflowId);
        loadedProgress.Should().NotBeNull();
        loadedProgress.Value.Context.Should().NotBeNull();
        loadedProgress.Value.Context!.ExecutionHistory.Should().HaveCount(1);
        
        var stepHistory = loadedProgress.Value.Context.ExecutionHistory[0];
        stepHistory.StepName.Should().Be("failing_step");
        stepHistory.Success.Should().BeFalse();
        stepHistory.ErrorMessage.Should().Be("Test failure");
    }

    /// <summary>
    /// Helper method to compute hash for testing (mirrors the one in SkProgressManager)
    /// </summary>
    private static string ComputeTestHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}