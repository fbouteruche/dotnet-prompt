using System.Diagnostics;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.SemanticKernel;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Models;

public class WorkflowExecutionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageCorrectly()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new WorkflowExecutionException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.Function);
        Assert.Null(exception.Context);
        Assert.NotNull(exception.CorrelationId); // Should auto-generate
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new ArgumentException("Inner error");
        var correlationId = "test-correlation-id";

        // Act
        var exception = new WorkflowExecutionException(message, innerException, correlationId: correlationId);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(correlationId, exception.CorrelationId);
    }

    [Fact]
    public void Constructor_WithoutCorrelationId_GeneratesOne()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new WorkflowExecutionException(message);

        // Assert
        Assert.NotNull(exception.CorrelationId);
        Assert.NotEmpty(exception.CorrelationId);
    }

    [Fact]
    public void Constructor_WithCorrelationId_UsesProvidedId()
    {
        // Arrange
        var message = "Test error message";
        var correlationId = "custom-correlation-id";

        // Act
        var exception = new WorkflowExecutionException(message, correlationId: correlationId);

        // Assert
        Assert.Equal(correlationId, exception.CorrelationId);
    }

    [Fact]
    public void Constructor_WithExecutionDuration_SetsProperty()
    {
        // Arrange
        var message = "Test error message";
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var exception = new WorkflowExecutionException(message, executionDuration: duration);

        // Assert
        Assert.Equal(duration, exception.ExecutionDuration);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new ArgumentException("Inner error");
        var correlationId = "test-correlation-id";
        var duration = TimeSpan.FromSeconds(10);

        // Act
        var exception = new WorkflowExecutionException(
            message, 
            innerException, 
            null, 
            null, 
            correlationId, 
            duration);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(correlationId, exception.CorrelationId);
        Assert.Equal(duration, exception.ExecutionDuration);
    }
}