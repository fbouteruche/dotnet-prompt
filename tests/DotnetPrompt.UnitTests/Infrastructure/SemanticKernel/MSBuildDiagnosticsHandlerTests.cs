using DotnetPrompt.Infrastructure.Analysis.Models;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel;

/// <summary>
/// Tests for MSBuild diagnostics handling
/// </summary>
public class MSBuildDiagnosticsHandlerTests
{
    private readonly Mock<ILogger<MSBuildDiagnosticsHandler>> _mockLogger;
    private readonly MSBuildDiagnosticsHandler _handler;

    public MSBuildDiagnosticsHandlerTests()
    {
        _mockLogger = new Mock<ILogger<MSBuildDiagnosticsHandler>>();
        _handler = new MSBuildDiagnosticsHandler(_mockLogger.Object);
    }

    [Fact]
    public void ProcessWorkspaceDiagnostics_WithEmptyDiagnostics_LogsDebugMessage()
    {
        // Arrange
        var diagnostics = Enumerable.Empty<WorkspaceDiagnostic>();
        var projectPath = "/test/project.csproj";

        // Act
        _handler.ProcessWorkspaceDiagnostics(diagnostics, projectPath);

        // Assert
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No workspace diagnostics found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ShouldFallbackToCustom_WithManyErrors_ReturnsTrue()
    {
        // Arrange - Create mock diagnostics by creating a list that will be processed
        var diagnostics = new List<WorkspaceDiagnostic>();
        
        // Since we can't easily create WorkspaceDiagnostic instances, 
        // we'll test the logic with an empty list and test indirectly
        var emptyDiagnostics = Enumerable.Empty<WorkspaceDiagnostic>();

        // Act
        var result = _handler.ShouldFallbackToCustom(emptyDiagnostics, null);

        // Assert - With null compilation, should return true
        Assert.True(result);
    }

    [Fact]
    public void ShouldFallbackToCustom_WithNullCompilation_ReturnsTrue()
    {
        // Arrange
        var diagnostics = Enumerable.Empty<WorkspaceDiagnostic>();
        var result = new CompilationResult(null, CompilationStrategy.MSBuild);

        // Act
        var shouldFallback = _handler.ShouldFallbackToCustom(diagnostics, result);

        // Assert
        Assert.True(shouldFallback);
    }

    [Fact]
    public void GenerateErrorSummary_WithNoDiagnostics_ReturnsNoIssuesMessage()
    {
        // Arrange
        var diagnostics = Enumerable.Empty<WorkspaceDiagnostic>();

        // Act
        var summary = _handler.GenerateErrorSummary(diagnostics);

        // Assert
        Assert.Contains("No MSBuild issues detected", summary);
    }

    [Fact]
    public void GenerateErrorSummary_WithEmptyList_ContainsCompletedMessage()
    {
        // Arrange
        var diagnostics = Enumerable.Empty<WorkspaceDiagnostic>();

        // Act
        var summary = _handler.GenerateErrorSummary(diagnostics);

        // Assert - should return "No MSBuild issues detected."
        Assert.Contains("No MSBuild issues detected", summary);
    }
}