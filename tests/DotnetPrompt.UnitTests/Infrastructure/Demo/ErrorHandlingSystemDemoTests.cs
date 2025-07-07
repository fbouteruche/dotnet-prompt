using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Core;
using System.Security;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Demo;

/// <summary>
/// Demonstration tests showing the comprehensive error handling system in action
/// </summary>
public class ErrorHandlingSystemDemoTests
{
    [Fact]
    public void ErrorHandlingSystem_ExceptionMapping_WorksCorrectly()
    {
        // Arrange & Act & Assert various exception mappings
        Assert.Equal(ExitCodes.AuthenticationError, new UnauthorizedAccessException().MapToExitCode());
        Assert.Equal(ExitCodes.AuthenticationError, new SecurityException().MapToExitCode());
        Assert.Equal(ExitCodes.ExecutionTimeout, new TimeoutException().MapToExitCode());
        Assert.Equal(ExitCodes.ExecutionTimeout, new TaskCanceledException().MapToExitCode());
        Assert.Equal(ExitCodes.InvalidArguments, new ArgumentNullException("param").MapToExitCode());
        Assert.Equal(ExitCodes.NetworkError, new HttpRequestException("Network error").MapToExitCode());
        Assert.Equal(ExitCodes.ValidationError, new FileNotFoundException("file.txt").MapToExitCode());
        Assert.Equal(ExitCodes.GeneralError, new InvalidOperationException("Generic error").MapToExitCode());
    }

    [Fact]
    public void ErrorHandlingSystem_WorkflowExecutionException_CapturesContext()
    {
        // Arrange
        var innerException = new ArgumentException("Invalid argument");
        var executionDuration = TimeSpan.FromSeconds(5);

        // Act
        var workflowException = new WorkflowExecutionException(
            "Workflow failed during execution",
            innerException,
            executionDuration: executionDuration);

        // Assert
        Assert.Equal("Workflow failed during execution", workflowException.Message);
        Assert.Equal(innerException, workflowException.InnerException);
        Assert.Equal(executionDuration, workflowException.ExecutionDuration);
        Assert.NotNull(workflowException.CorrelationId);
        Assert.NotEmpty(workflowException.CorrelationId);
    }

    [Fact]
    public void ErrorHandlingSystem_WorkflowExecutionContext_TracksMetrics()
    {
        // Arrange
        var context = new WorkflowExecutionContext("demo-correlation-id");

        // Act
        context.SetProperty("operation", "demo-operation");
        context.RecordMetric("execution_time_ms", 1234.5);
        context.RecordMetric("memory_usage_mb", 56.7);

        // Assert
        Assert.Equal("demo-correlation-id", context.CorrelationId);
        Assert.Equal("demo-operation", context.GetProperty<string>("operation"));
        Assert.Equal(1234.5, context.Metrics["execution_time_ms"]);
        Assert.Equal(56.7, context.Metrics["memory_usage_mb"]);
        Assert.True(context.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public void ErrorHandlingSystem_EndToEndFlow_DemonstratesIntegration()
    {
        // This test demonstrates how the error handling system works end-to-end
        
        // 1. Create an execution context (normally done by filters)
        var context = new WorkflowExecutionContext();
        context.SetProperty("function_name", "demo_function");
        context.RecordMetric("start_time", DateTime.UtcNow.Ticks);

        // 2. Simulate an error occurring during workflow execution
        var originalException = new UnauthorizedAccessException("API key invalid");

        // 3. Create a workflow execution exception with full context
        var workflowException = new WorkflowExecutionException(
            "Demo function failed due to authentication error",
            originalException,
            correlationId: context.CorrelationId,
            executionDuration: context.Duration);

        // 4. Map the exception to appropriate exit code
        var exitCode = originalException.MapToExitCode(); // Map the inner exception directly

        // 5. Verify the complete error handling flow
        Assert.Equal(ExitCodes.AuthenticationError, exitCode);
        Assert.Equal(context.CorrelationId, workflowException.CorrelationId);
        Assert.Equal(originalException, workflowException.InnerException);
        Assert.Contains("Demo function failed", workflowException.Message);
        Assert.NotNull(workflowException.ExecutionDuration);
    }
}