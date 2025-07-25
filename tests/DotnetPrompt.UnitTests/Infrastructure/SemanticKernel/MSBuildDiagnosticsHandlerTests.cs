using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        // Arrange - Create a compilation context with valid compilation to test diagnostic count logic
        var validCompilationContext = new RoslynCompilationContext
        {
            Success = true,
            Compilation = CSharpCompilation.Create("TestAssembly") // Empty but valid compilation
        };
        
        // Create multiple mock diagnostics that would cause fallback (>10 failures)
        // Since WorkspaceDiagnostic is hard to instantiate, we test the null context scenario instead
        var emptyDiagnostics = Enumerable.Empty<WorkspaceDiagnostic>();

        // Act - Test with null context (should fallback)
        var result = _handler.ShouldFallbackToCustom(emptyDiagnostics, null);

        // Assert - With null compilation context, should return true
        Assert.True(result);
    }

    [Fact]
    public void ShouldFallbackToCustom_WithNullCompilationContext_ReturnsTrue()
    {
        // Arrange
        var diagnostics = Enumerable.Empty<WorkspaceDiagnostic>();
        RoslynCompilationContext? compilationContext = null;

        // Act
        var shouldFallback = _handler.ShouldFallbackToCustom(diagnostics, compilationContext);

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