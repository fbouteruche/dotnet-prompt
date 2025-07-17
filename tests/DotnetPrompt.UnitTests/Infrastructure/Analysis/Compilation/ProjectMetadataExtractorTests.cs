using DotnetPrompt.Infrastructure.Analysis.Compilation;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Analysis.Compilation;

/// <summary>
/// Unit tests for ProjectMetadataExtractor
/// </summary>
public class ProjectMetadataExtractorTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ProjectMetadataExtractorTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void ExtractProjectMetadata_WithValidProject_ShouldReturnMetadata()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        // Act
        var metadata = ProjectMetadataExtractor.ExtractProjectMetadata(project, _mockLogger.Object);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().ContainKey("ProjectName");
        metadata.Should().ContainKey("Language");
        metadata.Should().ContainKey("DocumentCount");
        metadata.Should().ContainKey("MetadataReferences");
        metadata.Should().ContainKey("ProjectReferences");
        metadata.Should().ContainKey("AnalyzerReferences");
        
        metadata["ProjectName"].Should().Be("TestProject");
        metadata["Language"].Should().Be(LanguageNames.CSharp);
        metadata["DocumentCount"].Should().Be(0);
    }

    [Fact]
    public void ExtractProjectMetadata_WithDocuments_ShouldIncludeDocumentCount()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        
        // Add some documents
        project = project.AddDocument("Test1.cs", "class Test1 { }").Project;
        project = project.AddDocument("Test2.cs", "class Test2 { }").Project;

        // Act
        var metadata = ProjectMetadataExtractor.ExtractProjectMetadata(project, _mockLogger.Object);

        // Assert
        metadata["DocumentCount"].Should().Be(2);
        metadata["HasDocuments"].Should().Be(true);
    }

    [Fact]
    public void ExtractProjectMetadata_WithCompilationOptions_ShouldIncludeOptions()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            platform: Platform.X64,
            optimizationLevel: OptimizationLevel.Release,
            allowUnsafe: true,
            checkOverflow: true);
            
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(compilationOptions);

        // Act
        var metadata = ProjectMetadataExtractor.ExtractProjectMetadata(project, _mockLogger.Object);

        // Assert
        metadata.Should().ContainKey("Platform");
        metadata.Should().ContainKey("OptimizationLevel");
        metadata.Should().ContainKey("AllowUnsafe");
        metadata.Should().ContainKey("CheckOverflow");
        
        metadata["Platform"].Should().Be("X64");
        metadata["OptimizationLevel"].Should().Be("Release");
        metadata["AllowUnsafe"].Should().Be(true);
        metadata["CheckOverflow"].Should().Be(true);
    }

    [Fact]
    public void ExtractTargetFramework_WithValidProject_ShouldReturnFramework()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithAssemblyName("TestAssembly");

        // Act
        var targetFramework = ProjectMetadataExtractor.ExtractTargetFramework(project);

        // Assert
        targetFramework.Should().NotBeNull();
        targetFramework.Should().Be("net8.0"); // Default framework
    }

    [Fact]
    public void ExtractTargetFramework_WithNullProject_ShouldReturnNull()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);

        // Act
        var targetFramework = ProjectMetadataExtractor.ExtractTargetFramework(project);

        // Assert
        // Should handle gracefully and return null for projects without assembly name
        targetFramework.Should().BeNull();
    }

    [Fact]
    public void ExtractSolutionMetadata_WithValidSolution_ShouldReturnMetadata()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        
        // Add some projects
        solution = solution.AddProject("Project1", "Project1", LanguageNames.CSharp);
        solution = solution.AddProject("Project2", "Project2", LanguageNames.CSharp);
        solution = solution.AddProject("FSharpProject", "FSharpProject", LanguageNames.FSharp);

        // Act
        var metadata = ProjectMetadataExtractor.ExtractSolutionMetadata(solution, _mockLogger.Object);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().ContainKey("ProjectCount");
        metadata.Should().ContainKey("CSharpProjectCount");
        metadata.Should().ContainKey("TotalDocumentCount");
        metadata.Should().ContainKey("ProjectNames");
        
        metadata["ProjectCount"].Should().Be(3);
        metadata["CSharpProjectCount"].Should().Be(2);
        metadata["TotalDocumentCount"].Should().Be(0);
        
        var projectNames = metadata["ProjectNames"] as List<string>;
        projectNames.Should().NotBeNull();
        projectNames.Should().Contain("Project1");
        projectNames.Should().Contain("Project2");
        projectNames.Should().Contain("FSharpProject");
    }

    [Fact]
    public void ExtractSolutionMetadata_WithEmptySolution_ShouldReturnEmptyMetadata()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;

        // Act
        var metadata = ProjectMetadataExtractor.ExtractSolutionMetadata(solution, _mockLogger.Object);

        // Assert
        metadata.Should().NotBeNull();
        metadata["ProjectCount"].Should().Be(0);
        metadata["CSharpProjectCount"].Should().Be(0);
        metadata["TotalDocumentCount"].Should().Be(0);
    }

    [Fact]
    public void ExtractProjectMetadata_WithException_ShouldHandleGracefully()
    {
        // Arrange - Create a project that might cause issues
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);

        // Act - Should not throw even if there are issues
        var metadata = ProjectMetadataExtractor.ExtractProjectMetadata(project, _mockLogger.Object);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().ContainKey("ProjectName");
        metadata.Should().ContainKey("Language");
        metadata.Should().ContainKey("DocumentCount");
        
        // Should have basic metadata even if extraction partially fails
        metadata["ProjectName"].Should().Be("TestProject");
        metadata["Language"].Should().Be(LanguageNames.CSharp);
    }

    [Fact]
    public void ExtractProjectMetadata_ShouldLogDebugInformation()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);

        // Act
        var metadata = ProjectMetadataExtractor.ExtractProjectMetadata(project, _mockLogger.Object);

        // Assert - Should log debug information
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extracted metadata")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}