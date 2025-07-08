using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetPrompt.IntegrationTests.SubWorkflow;

public class SubWorkflowPluginIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IServiceProvider _serviceProvider;
    private readonly SubWorkflowPlugin _plugin;

    public SubWorkflowPluginIntegrationTests()
    {
        // Create test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "subworkflow-integration-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Setup minimal service provider for integration test
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<SubWorkflowPlugin>>(new MockLogger<SubWorkflowPlugin>());
        services.AddSingleton<IDotpromptParser, DotpromptParser>();
        services.AddSingleton<IWorkflowOrchestrator, MockWorkflowOrchestrator>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        _plugin = new SubWorkflowPlugin(
            _serviceProvider.GetRequiredService<IDotpromptParser>(),
            _serviceProvider.GetRequiredService<IWorkflowOrchestrator>(),
            _serviceProvider.GetRequiredService<ILogger<SubWorkflowPlugin>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithRealWorkflowFile_ParsesAndExecutes()
    {
        // Arrange
        var subWorkflowPath = Path.Combine(_testDirectory, "test-sub.prompt.md");
        var subWorkflowContent = """
            ---
            name: "test-sub-workflow"
            model: "gpt-4o"
            tools: ["file-system"]
            input:
              schema:
                project_path: { type: string }
            ---
            
            # Sub-workflow Test
            
            Analyze the project at {{project_path}} and provide a summary.
            """;
        
        await File.WriteAllTextAsync(subWorkflowPath, subWorkflowContent);
        
        var parameters = """{"project_path": "/test/project"}""";

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(subWorkflowPath, parameters, true);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.Equal("Mock execution completed", result.Output);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task ValidateSubWorkflowAsync_WithRealWorkflowFile_ValidatesCorrectly()
    {
        // Arrange
        var subWorkflowPath = Path.Combine(_testDirectory, "validate-test.prompt.md");
        var subWorkflowContent = """
            ---
            name: "validation-test"
            model: "gpt-4o"
            tools: ["sub-workflow"]
            ---
            
            # Validation Test Workflow
            
            This is a simple workflow for validation testing.
            """;
        
        await File.WriteAllTextAsync(subWorkflowPath, subWorkflowContent);

        // Act
        var result = await _plugin.ValidateSubWorkflowAsync(subWorkflowPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithMissingFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.prompt.md");

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(nonExistentPath);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Contains("Sub-workflow execution failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteSubWorkflowAsync_WithParameterPassing_PassesParametersCorrectly()
    {
        // Arrange
        var subWorkflowPath = Path.Combine(_testDirectory, "param-test.prompt.md");
        var subWorkflowContent = """
            ---
            name: "parameter-test"
            model: "gpt-4o"
            input:
              schema:
                test_param: { type: string }
                number_param: { type: number }
            ---
            
            # Parameter Test
            
            Testing parameter: {{test_param}}
            Number: {{number_param}}
            """;
        
        await File.WriteAllTextAsync(subWorkflowPath, subWorkflowContent);
        
        var parameters = """{"test_param": "hello world", "number_param": 42}""";

        // Act
        var result = await _plugin.ExecuteSubWorkflowAsync(subWorkflowPath, parameters, true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Mock execution completed", result.Output);
    }

    /// <summary>
    /// Mock workflow orchestrator for integration testing
    /// </summary>
    private class MockWorkflowOrchestrator : IWorkflowOrchestrator
    {
        public Task<WorkflowExecutionResult> ExecuteWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
        {
            // Simulate successful execution
            return Task.FromResult(new WorkflowExecutionResult(
                Success: true,
                Output: "Mock execution completed",
                ExecutionTime: TimeSpan.FromMilliseconds(100)));
        }

        public Task<WorkflowValidationResult> ValidateWorkflowAsync(DotpromptWorkflow workflow, WorkflowExecutionContext context, CancellationToken cancellationToken = default)
        {
            // Simulate successful validation
            return Task.FromResult(new WorkflowValidationResult(
                IsValid: true,
                Errors: null,
                Warnings: null));
        }

        public Task<WorkflowExecutionResult> ResumeWorkflowAsync(string workflowId, DotpromptWorkflow workflow, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new WorkflowExecutionResult(Success: true, Output: "Mock resume"));
        }

        public Task<Microsoft.SemanticKernel.ChatCompletion.ChatHistory> GetChatHistoryAsync(string workflowId)
        {
            return Task.FromResult(new Microsoft.SemanticKernel.ChatCompletion.ChatHistory());
        }

        public Task SaveChatHistoryAsync(string workflowId, Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory)
        {
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Mock logger for integration testing
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}