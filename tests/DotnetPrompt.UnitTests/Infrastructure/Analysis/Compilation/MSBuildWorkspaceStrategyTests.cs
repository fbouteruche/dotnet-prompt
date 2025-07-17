using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Infrastructure.Analysis.Compilation;
using DotnetPrompt.Infrastructure.SemanticKernel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Analysis.Compilation;

/// <summary>
/// Unit tests for MSBuildWorkspaceStrategy
/// </summary>
public class MSBuildWorkspaceStrategyTests
{
    private readonly Mock<ILogger<MSBuildWorkspaceStrategy>> _mockLogger;
    private readonly Mock<MSBuildDiagnosticsHandler> _mockDiagnosticsHandler;
    private readonly MSBuildWorkspaceStrategy _strategy;

    public MSBuildWorkspaceStrategyTests()
    {
        _mockLogger = new Mock<ILogger<MSBuildWorkspaceStrategy>>();
        
        // Create a mock diagnostics handler
        var mockDiagnosticsLogger = new Mock<ILogger<MSBuildDiagnosticsHandler>>();
        _mockDiagnosticsHandler = new Mock<MSBuildDiagnosticsHandler>(mockDiagnosticsLogger.Object);
        
        _strategy = new MSBuildWorkspaceStrategy(_mockLogger.Object, _mockDiagnosticsHandler.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeMSBuild()
    {
        // Arrange & Act - Constructor is called in setup
        
        // Assert - MSBuild should be initialized
        MSBuildSetup.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void StrategyType_ShouldReturnMSBuild()
    {
        // Act
        var strategyType = _strategy.StrategyType;

        // Assert
        strategyType.Should().Be(CompilationStrategy.MSBuild);
    }

    [Fact]
    public void Priority_ShouldBeHigh()
    {
        // Act
        var priority = _strategy.Priority;

        // Assert
        priority.Should().Be(100); // High priority
    }

    [Theory]
    [InlineData(".csproj", true)]
    [InlineData(".fsproj", true)]
    [InlineData(".vbproj", true)]
    [InlineData(".sln", true)]
    [InlineData(".txt", false)]
    [InlineData(".cs", false)]
    [InlineData("", false)]
    public void CanHandle_ShouldReturnExpectedResult(string extension, bool expected)
    {
        // Arrange
        var projectPath = $"test{extension}";
        var options = new AnalysisOptions();

        // Act
        var result = _strategy.CanHandle(projectPath, options);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("PROJECT.CSPROJ", true)]  // Case insensitive
    [InlineData("solution.SLN", true)]
    [InlineData("test.FSPROJ", true)]
    public void CanHandle_ShouldBeCaseInsensitive(string projectPath, bool expected)
    {
        // Arrange
        var options = new AnalysisOptions();

        // Act
        var result = _strategy.CanHandle(projectPath, options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Description_ShouldProvideDescriptiveText()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("MSBuild");
        description.Should().Contain("Workspace");
    }

    [Fact]
    public async Task CreateCompilationAsync_WithInvalidPath_ShouldReturnFailure()
    {
        // Arrange
        var invalidPath = "nonexistent.csproj";
        var options = new AnalysisCompilationOptions
        {
            MSBuildTimeout = 5000
        };

        // Act
        var result = await _strategy.CreateCompilationAsync(invalidPath, options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StrategyUsed.Should().Be(CompilationStrategy.MSBuild);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCompilationAsync_WithTimeout_ShouldHandleGracefully()
    {
        // Arrange
        var projectPath = "test.csproj";
        var options = new AnalysisCompilationOptions
        {
            MSBuildTimeout = 1 // Very short timeout to force timeout
        };

        // Act
        var result = await _strategy.CreateCompilationAsync(projectPath, options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StrategyUsed.Should().Be(CompilationStrategy.MSBuild);
        // Should handle timeout gracefully without throwing
    }

    [Fact]
    public async Task CreateCompilationAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var projectPath = "test.csproj";
        var options = new AnalysisCompilationOptions();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _strategy.CreateCompilationAsync(projectPath, options, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task CreateCompilationWithRoslynAsync_WithInvalidPath_ShouldReturnFailure()
    {
        // Arrange
        var invalidPath = "nonexistent.csproj";
        var options = new AnalysisCompilationOptions
        {
            MSBuildTimeout = 5000
        };

        // Act
        var (result, compilation) = await _strategy.CreateCompilationWithRoslynAsync(invalidPath, options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StrategyUsed.Should().Be(CompilationStrategy.MSBuild);
        compilation.Should().BeNull();
    }

    [Theory]
    [InlineData("test.csproj")]
    [InlineData("solution.sln")]
    public async Task CreateCompilationAsync_ShouldLogAppropriateMessages(string projectPath)
    {
        // Arrange
        var options = new AnalysisCompilationOptions
        {
            MSBuildTimeout = 5000
        };

        // Act
        var result = await _strategy.CreateCompilationAsync(projectPath, options);

        // Assert - Should log information about starting compilation
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting MSBuild compilation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}