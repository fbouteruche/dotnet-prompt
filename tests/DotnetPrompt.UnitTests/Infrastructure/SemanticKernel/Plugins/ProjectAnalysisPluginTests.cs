using DotnetPrompt.Infrastructure.Analysis;
using DotnetPrompt.Infrastructure.Analysis.Compilation;
using DotnetPrompt.Infrastructure.Analysis.Models;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Tests for the new Roslyn-based ProjectAnalysisPlugin
/// </summary>
public class ProjectAnalysisPluginTests
{
    private readonly Mock<ILogger<ProjectAnalysisPlugin>> _mockLogger;
    private readonly Mock<IRoslynAnalysisService> _mockRoslynService;
    private readonly ProjectAnalysisPlugin _plugin;

    public ProjectAnalysisPluginTests()
    {
        _mockLogger = new Mock<ILogger<ProjectAnalysisPlugin>>();
        _mockRoslynService = new Mock<IRoslynAnalysisService>();
        _plugin = new ProjectAnalysisPlugin(_mockLogger.Object, _mockRoslynService.Object);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithValidProjectPath_CallsRoslynService()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var projectPath = Path.Combine(currentDir, "test.csproj");
        
        // Create a temporary project file for testing
        await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        
        try
        {
            var expectedJson = """
            {
              "success": true,
              "projectPath": "/test/project.csproj",
              "analysisTimestamp": "2024-01-01T00:00:00.000Z",
              "metadata": {
                "projectName": "TestProject",
                "projectType": "Library"
              }
            }
            """;

            _mockRoslynService
                .Setup(x => x.AnalyzeAsync(
                    It.IsAny<string>(),
                    It.IsAny<AnalysisOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJson);

            // Act
            var result = await _plugin.AnalyzeProjectAsync(projectPath);

            // Assert
            result.Should().Be(expectedJson);
            
            _mockRoslynService.Verify(
                x => x.AnalyzeAsync(
                    It.IsAny<string>(),
                    It.Is<AnalysisOptions>(opts => 
                        opts.AnalysisDepth == AnalysisDepth.Standard &&
                        opts.SemanticDepth == SemanticAnalysisDepth.None &&
                        opts.CompilationStrategy == CompilationStrategy.Auto),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            // Clean up
            if (File.Exists(projectPath))
                File.Delete(projectPath);
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithCustomParameters_PassesCorrectOptions()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var projectPath = Path.Combine(currentDir, "test2.csproj");
        
        // Create a temporary project file for testing
        await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        
        try
        {
            var expectedJson = """{"success": true}""";

            _mockRoslynService
                .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<AnalysisOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJson);

            // Act
            await _plugin.AnalyzeProjectAsync(
                project_path: projectPath,
                analysis_depth: "Deep",
                semantic_depth: "Standard",
                compilation_strategy: "Hybrid",
                include_dependencies: false,
                include_metrics: false,
                include_patterns: true,
                include_vulnerabilities: true,
                target_framework: "net8.0",
                max_depth: 10,
                exclude_generated: false,
                include_tests: false,
                msbuild_timeout: 60000,
                fallback_to_custom: false,
                lightweight_mode: true);

            // Assert
            _mockRoslynService.Verify(
                x => x.AnalyzeAsync(
                    It.IsAny<string>(),
                    It.Is<AnalysisOptions>(opts =>
                        opts.AnalysisDepth == AnalysisDepth.Deep &&
                        opts.SemanticDepth == SemanticAnalysisDepth.Standard &&
                        opts.CompilationStrategy == CompilationStrategy.Hybrid &&
                        opts.IncludeDependencies == false &&
                        opts.IncludeMetrics == false &&
                        opts.IncludePatterns == true &&
                        opts.IncludeVulnerabilities == true &&
                        opts.TargetFramework == "net8.0" &&
                        opts.MaxDepth == 10 &&
                        opts.ExcludeGenerated == false &&
                        opts.IncludeTests == false &&
                        opts.MSBuildTimeout == 60000 &&
                        opts.FallbackToCustom == false &&
                        opts.LightweightMode == true),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            // Clean up
            if (File.Exists(projectPath))
                File.Delete(projectPath);
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithInvalidProjectPath_ThrowsKernelException()
    {
        // Act & Assert - Empty path
        var ex1 = await Assert.ThrowsAsync<Microsoft.SemanticKernel.KernelException>(
            () => _plugin.AnalyzeProjectAsync(""));
        ex1.Message.Should().Contain("Project path cannot be null or empty");
        
        // Act & Assert - Invalid extension (create the file first so it exists)
        var currentDir = Directory.GetCurrentDirectory();
        var invalidFile = Path.Combine(currentDir, "invalid.txt");
        await File.WriteAllTextAsync(invalidFile, "test content");
        
        try
        {
            var ex2 = await Assert.ThrowsAsync<Microsoft.SemanticKernel.KernelException>(
                () => _plugin.AnalyzeProjectAsync(invalidFile));
            ex2.Message.Should().Contain("Invalid project file extension");
        }
        finally
        {
            if (File.Exists(invalidFile))
                File.Delete(invalidFile);
        }
    }

    [Fact]
    public async Task ParseAnalysisDepth_WithValidValues_ReturnsCorrectEnum()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var testCases = new Dictionary<string, AnalysisDepth>
        {
            { "surface", AnalysisDepth.Surface },
            { "standard", AnalysisDepth.Standard },
            { "deep", AnalysisDepth.Deep },
            { "comprehensive", AnalysisDepth.Comprehensive },
            { "invalid", AnalysisDepth.Standard } // Should default to Standard
        };

        foreach (var testCase in testCases)
        {
            var projectPath = Path.Combine(currentDir, $"test_{testCase.Key}.csproj");
            await File.WriteAllTextAsync(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            
            try
            {
                _mockRoslynService
                    .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<AnalysisOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("{}");

                // Act
                await _plugin.AnalyzeProjectAsync(projectPath, analysis_depth: testCase.Key);

                // Assert
                _mockRoslynService.Verify(
                    x => x.AnalyzeAsync(
                        It.IsAny<string>(),
                        It.Is<AnalysisOptions>(opts => opts.AnalysisDepth == testCase.Value),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
            finally
            {
                if (File.Exists(projectPath))
                    File.Delete(projectPath);
            }

            _mockRoslynService.Reset();
        }
    }
}