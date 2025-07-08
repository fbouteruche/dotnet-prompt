using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Progress;
using DotnetPrompt.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using Xunit;

namespace DotnetPrompt.IntegrationTests.Workflows;

/// <summary>
/// Tests file-based progress management with SK ChatHistory serialization
/// Validates progress file creation, restoration, and cleanup functionality
/// </summary>
public class FileBasedProgressTests : IDisposable
{
    private readonly FileProgressManager _progressManager;
    private readonly string _testDirectory;

    public FileBasedProgressTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "progress-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var logger = new MockLogger<FileProgressManager>();
        _progressManager = new FileProgressManager(logger, _testDirectory);
    }

    [Fact]
    public async Task FileProgressManager_SaveProgress_CreatesValidJsonFile()
    {
        // Arrange
        var workflowId = "test-workflow-123";
        var context = new WorkflowExecutionContext
        {
            CurrentStep = 2,
            Variables = new Dictionary<string, object>
            {
                ["project_name"] = "TestProject",
                ["include_tests"] = true,
                ["analysis_depth"] = 3
            },
            ExecutionHistory = new List<StepExecutionHistory>
            {
                new() { StepName = "Initialize", Timestamp = DateTime.UtcNow.AddMinutes(-2), Success = true },
                new() { StepName = "Analyze", Timestamp = DateTime.UtcNow.AddMinutes(-1), Success = true }
            },
            StartTime = DateTime.UtcNow.AddMinutes(-5)
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Analyze the project structure");
        chatHistory.AddAssistantMessage("I'll analyze the project structure for you. Starting with dependencies...");
        chatHistory.AddUserMessage("Focus on the test coverage");

        // Act
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);

        // Assert
        var progressFiles = Directory.GetFiles(_testDirectory, "*.json");
        progressFiles.Should().HaveCount(1, "Should create exactly one progress file");

        var progressFile = progressFiles[0];
        File.Exists(progressFile).Should().BeTrue("Progress file should exist");

        // Verify file content structure
        var jsonContent = await File.ReadAllTextAsync(progressFile);
        jsonContent.Should().NotBeNullOrEmpty("Progress file should not be empty");

        // Parse and validate JSON structure
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var root = jsonDocument.RootElement;

        root.TryGetProperty("workflowMetadata", out var metadata).Should().BeTrue();
        root.TryGetProperty("executionContext", out var executionContext).Should().BeTrue();
        root.TryGetProperty("chatHistory", out var chatHistoryArray).Should().BeTrue();
        
        chatHistoryArray.GetArrayLength().Should().Be(3, "Should have 3 chat messages");
    }

    [Fact]
    public async Task SkChatHistory_Serialization_PreservesConversationState()
    {
        // Arrange
        var workflowId = "chat-history-test";
        var context = new WorkflowExecutionContext
        {
            CurrentStep = 1,
            Variables = new Dictionary<string, object> { ["test"] = "value" },
            ExecutionHistory = new List<StepExecutionHistory>
            {
                new() { StepName = "Started", Timestamp = DateTime.UtcNow, Success = true }
            }
        };

        var originalChatHistory = new ChatHistory();
        originalChatHistory.AddSystemMessage("You are a helpful assistant for .NET development.");
        originalChatHistory.AddUserMessage("Please analyze this C# project for potential improvements.");
        originalChatHistory.AddAssistantMessage("I'll analyze your C# project. Let me examine the structure and dependencies first.");
        originalChatHistory.AddUserMessage("Focus on security vulnerabilities and performance issues.");
        originalChatHistory.AddAssistantMessage("I'll prioritize security and performance in my analysis. Here's what I found...");

        // Act - Save and restore
        await _progressManager.SaveProgressAsync(workflowId, context, originalChatHistory);
        var result = await _progressManager.LoadProgressAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        var (restoredContext, restoredChatHistory) = result.Value;

        restoredContext.Should().NotBeNull();
        restoredChatHistory.Should().NotBeNull();

        restoredChatHistory!.Count.Should().Be(originalChatHistory.Count, "Should restore all chat messages");

        // Verify message content and roles are preserved
        for (int i = 0; i < originalChatHistory.Count; i++)
        {
            var original = originalChatHistory[i];
            var restored = restoredChatHistory[i];

            restored.Role.Should().Be(original.Role, $"Message {i} role should be preserved");
            restored.Content.Should().Be(original.Content, $"Message {i} content should be preserved");
        }

        // Verify conversation flow preservation
        restoredChatHistory.Where(m => m.Role.Label == "user").Should().HaveCount(2);
        restoredChatHistory.Where(m => m.Role.Label == "assistant").Should().HaveCount(2);
    }

    [Fact]
    public async Task ProgressFile_InterruptionAndResume_MaintainsState()
    {
        // Arrange - Simulate workflow interruption scenario
        var workflowId = "interruption-test";
        
        // First execution - save progress at step 3
        var initialContext = new WorkflowExecutionContext
        {
            CurrentStep = 3,
            Variables = new Dictionary<string, object>
            {
                ["project_path"] = "/path/to/project",
                ["analysis_complete"] = false,
                ["partial_results"] = "dependencies: 42, files: 156"
            },
            ExecutionHistory = new List<StepExecutionHistory> 
            { 
                new() { StepName = "Project discovery", Timestamp = DateTime.UtcNow.AddMinutes(-3), Success = true },
                new() { StepName = "Dependency analysis", Timestamp = DateTime.UtcNow.AddMinutes(-2), Success = true },
                new() { StepName = "File analysis - IN PROGRESS", Timestamp = DateTime.UtcNow.AddMinutes(-1), Success = false }
            },
            StartTime = DateTime.UtcNow.AddMinutes(-10)
        };

        var initialChatHistory = new ChatHistory();
        initialChatHistory.AddUserMessage("Analyze the project and provide a comprehensive report.");
        initialChatHistory.AddAssistantMessage("I'll analyze your project comprehensively. Starting with project structure...");
        initialChatHistory.AddAssistantMessage("Found 42 dependencies and 156 source files. Analyzing code quality...");

        // Act - Save initial progress
        await _progressManager.SaveProgressAsync(workflowId, initialContext, initialChatHistory);

        // Simulate resume - load progress and continue
        var resumeResult = await _progressManager.LoadProgressAsync(workflowId);
        resumeResult.Should().NotBeNull();
        
        var (resumedContext, resumedChatHistory) = resumeResult.Value;
        resumedContext.Should().NotBeNull();
        resumedChatHistory.Should().NotBeNull();

        // Simulate continuation - update progress
        resumedContext!.CurrentStep = 4;
        resumedContext.Variables["analysis_complete"] = true;
        resumedContext.ExecutionHistory.Add(new StepExecutionHistory 
        { 
            StepName = "Code quality analysis - COMPLETED", 
            Timestamp = DateTime.UtcNow, 
            Success = true 
        });

        resumedChatHistory!.AddAssistantMessage("Code quality analysis complete. Overall score: 85/100. Here are the recommendations...");

        // Save updated progress
        await _progressManager.SaveProgressAsync(workflowId, resumedContext, resumedChatHistory);

        // Final load to verify state
        var finalResult = await _progressManager.LoadProgressAsync(workflowId);
        finalResult.Should().NotBeNull();
        
        var (finalContext, finalChatHistory) = finalResult.Value;

        // Assert
        finalContext.Should().NotBeNull();
        finalContext!.CurrentStep.Should().Be(4, "Should resume from the correct step");
        finalContext.Variables["analysis_complete"].Should().Be(true);
        finalContext.ExecutionHistory.Should().HaveCount(4, "Should preserve all execution history");

        finalChatHistory.Should().NotBeNull();
        finalChatHistory!.Count.Should().Be(4, "Should preserve complete conversation");
        finalChatHistory.Last().Content.Should().Contain("Overall score: 85/100");
    }

    [Fact]
    public async Task ProgressFileCleanup_OldFiles_RemovedCorrectly()
    {
        // Arrange - Create multiple progress files with different ages
        var workflows = new[]
        {
            new { Id = "recent-workflow", Age = TimeSpan.FromHours(1) },
            new { Id = "old-workflow-1", Age = TimeSpan.FromDays(8) },
            new { Id = "old-workflow-2", Age = TimeSpan.FromDays(15) },
            new { Id = "medium-workflow", Age = TimeSpan.FromDays(3) }
        };

        var context = new WorkflowExecutionContext
        {
            CurrentStep = 1,
            Variables = new Dictionary<string, object> { ["test"] = "cleanup" },
            ExecutionHistory = new List<StepExecutionHistory>
            {
                new() { StepName = "Test step", Timestamp = DateTime.UtcNow, Success = true }
            }
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Test cleanup");

        foreach (var workflow in workflows)
        {
            // Save progress file
            await _progressManager.SaveProgressAsync(workflow.Id, context, chatHistory);
            
            // Manually adjust file timestamp to simulate age
            var progressFiles = Directory.GetFiles(_testDirectory, "*.json");
            var workflowFile = progressFiles.FirstOrDefault(f => f.Contains(workflow.Id));
            if (workflowFile != null)
            {
                var targetTime = DateTime.Now.Subtract(workflow.Age);
                File.SetLastWriteTime(workflowFile, targetTime);
            }
        }

        // Verify all files were created
        var allFiles = Directory.GetFiles(_testDirectory, "*.json");
        allFiles.Should().HaveCount(4, "Should create all progress files");

        // Act - Cleanup files older than 7 days
        var cleanupDate = DateTime.Now.AddDays(-7);
        var deletedCount = await _progressManager.CleanupOldProgressAsync(cleanupDate);

        // Assert
        deletedCount.Should().Be(2, "Should delete 2 old files (8 days and 15 days old)");

        var remainingFiles = Directory.GetFiles(_testDirectory, "*.json");
        remainingFiles.Should().HaveCount(2, "Should keep 2 recent files (1 hour and 3 days old)");
    }

    [Fact]
    public async Task ProgressFile_CrossPlatformCompatibility_WorksCorrectly()
    {
        // Arrange - Test with various path and content scenarios
        var workflowId = "cross-platform-test";
        var context = new WorkflowExecutionContext
        {
            CurrentStep = 1,
            Variables = new Dictionary<string, object>
            {
                ["file_path"] = "/unix/style/path/file.txt",
                ["windows_path"] = "C:\\Windows\\Style\\Path\\file.txt",
                ["special_chars"] = "Special chars: äöü, 中文, 🚀",
                ["unicode_content"] = "Unicode test: αβγδε"
            },
            ExecutionHistory = new List<StepExecutionHistory>
            { 
                new() { StepName = "Cross-platform path test", Timestamp = DateTime.UtcNow.AddMinutes(-1), Success = true },
                new() { StepName = "Unicode content handling", Timestamp = DateTime.UtcNow, Success = true }
            }
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("Test with special characters: äöü, 中文, emoji 🚀");
        chatHistory.AddAssistantMessage("I can handle Unicode content: αβγδε and various paths.");

        // Act
        await _progressManager.SaveProgressAsync(workflowId, context, chatHistory);
        var result = await _progressManager.LoadProgressAsync(workflowId);

        // Assert - All content should be preserved regardless of platform
        result.Should().NotBeNull();
        var (restoredContext, restoredChatHistory) = result.Value;
        
        restoredContext.Should().NotBeNull();
        restoredContext!.Variables["special_chars"].Should().Be("Special chars: äöü, 中文, 🚀");
        restoredContext.Variables["unicode_content"].Should().Be("Unicode test: αβγδε");

        restoredChatHistory.Should().NotBeNull();
        restoredChatHistory![0].Content.Should().Contain("äöü, 中文, emoji 🚀");
        restoredChatHistory[1].Content.Should().Contain("αβγδε");

        // Verify file can be read as valid JSON
        var progressFiles = Directory.GetFiles(_testDirectory, "*.json");
        var jsonContent = await File.ReadAllTextAsync(progressFiles[0]);
        var isValidJson = IsValidJson(jsonContent);
        isValidJson.Should().BeTrue("Progress file should be valid JSON regardless of platform");
    }

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}