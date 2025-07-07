using DotnetPrompt.Infrastructure.Models;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Models;

public class WorkflowExecutionContextTests
{
    [Fact]
    public void Constructor_WithoutCorrelationId_GeneratesOne()
    {
        // Act
        var context = new WorkflowExecutionContext();

        // Assert
        Assert.NotNull(context.CorrelationId);
        Assert.NotEmpty(context.CorrelationId);
        Assert.True(context.StartTime <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithCorrelationId_UsesProvidedId()
    {
        // Arrange
        var correlationId = "custom-correlation-id";

        // Act
        var context = new WorkflowExecutionContext(correlationId);

        // Assert
        Assert.Equal(correlationId, context.CorrelationId);
    }

    [Fact]
    public void Duration_ReturnsTimeElapsedSinceStart()
    {
        // Arrange
        var context = new WorkflowExecutionContext();
        
        // Act
        Thread.Sleep(10); // Small delay to ensure duration > 0
        var duration = context.Duration;

        // Assert
        Assert.True(duration.TotalMilliseconds >= 0);
    }

    [Fact]
    public void SetProperty_AndGetProperty_WorksCorrectly()
    {
        // Arrange
        var context = new WorkflowExecutionContext();
        var key = "test-key";
        var value = "test-value";

        // Act
        context.SetProperty(key, value);
        var retrievedValue = context.GetProperty<string>(key);

        // Assert
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public void GetProperty_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var context = new WorkflowExecutionContext();

        // Act
        var retrievedValue = context.GetProperty<string>("missing-key");

        // Assert
        Assert.Null(retrievedValue);
    }

    [Fact]
    public void GetProperty_WithWrongType_ReturnsDefault()
    {
        // Arrange
        var context = new WorkflowExecutionContext();
        var key = "test-key";
        context.SetProperty(key, "string-value");

        // Act
        var retrievedValue = context.GetProperty<int>(key);

        // Assert
        Assert.Equal(0, retrievedValue);
    }

    [Fact]
    public void RecordMetric_StoresMetricCorrectly()
    {
        // Arrange
        var context = new WorkflowExecutionContext();
        var metricName = "test-metric";
        var metricValue = 123.45;

        // Act
        context.RecordMetric(metricName, metricValue);

        // Assert
        Assert.True(context.Metrics.ContainsKey(metricName));
        Assert.Equal(metricValue, context.Metrics[metricName]);
    }

    [Fact]
    public void RecordMetric_WithSameName_OverwritesPreviousValue()
    {
        // Arrange
        var context = new WorkflowExecutionContext();
        var metricName = "test-metric";
        var firstValue = 100.0;
        var secondValue = 200.0;

        // Act
        context.RecordMetric(metricName, firstValue);
        context.RecordMetric(metricName, secondValue);

        // Assert
        Assert.Equal(secondValue, context.Metrics[metricName]);
    }

    [Fact]
    public void Properties_InitializedAsEmpty()
    {
        // Act
        var context = new WorkflowExecutionContext();

        // Assert
        Assert.NotNull(context.Properties);
        Assert.Empty(context.Properties);
    }

    [Fact]
    public void Metrics_InitializedAsEmpty()
    {
        // Act
        var context = new WorkflowExecutionContext();

        // Assert
        Assert.NotNull(context.Metrics);
        Assert.Empty(context.Metrics);
    }
}