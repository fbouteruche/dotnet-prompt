using System.Text.Json;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Models.RoslynAnalysis;

public class RoslynAnalysisResultTests
{
    [Fact]
    public void RoslynAnalysisResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new RoslynAnalysisResult();

        // Assert
        result.ProjectPath.Should().Be(string.Empty);
        result.AnalysisTimestamp.Should().Be(string.Empty);
        result.Success.Should().BeFalse();
        result.ProjectMetadata.Should().NotBeNull();
        result.Structure.Should().NotBeNull();
        result.Dependencies.Should().NotBeNull();
        result.Recommendations.Should().NotBeNull().And.BeEmpty();
        result.Performance.Should().NotBeNull();
        result.Compilation.Should().BeNull();
        result.Semantics.Should().BeNull();
        result.Metrics.Should().BeNull();
        result.Patterns.Should().BeNull();
        result.Vulnerabilities.Should().BeNull();
    }

    [Fact]
    public void RoslynAnalysisResult_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var result = new RoslynAnalysisResult
        {
            ProjectPath = "/path/to/project.csproj",
            AnalysisTimestamp = "2024-01-15T10:30:00Z",
            AnalysisDepth = AnalysisDepth.Deep,
            SemanticDepth = SemanticAnalysisDepth.Standard,
            CompilationStrategy = CompilationStrategy.MSBuild,
            Success = true,
            ProjectMetadata = new ProjectMetadata
            {
                Name = "TestProject",
                ProjectType = "Console Application",
                TargetFrameworks = new List<string> { "net8.0" },
                LanguageVersion = "12.0",
                NullableEnabled = true,
                AssemblyVersion = "1.0.0.0"
            },
            Compilation = new CompilationInfo
            {
                AssemblyName = "TestProject",
                CompilationSuccessful = true,
                SyntaxTreeCount = 5,
                ReferencedAssemblyCount = 10,
                DiagnosticCounts = new DiagnosticCount
                {
                    Errors = 0,
                    Warnings = 2,
                    Info = 5
                }
            },
            Performance = new PerformanceMetrics
            {
                TotalTimeMs = 5000,
                CompilationTimeMs = 2000,
                AnalysisTimeMs = 3000,
                PeakMemoryBytes = 100000000,
                FilesProcessed = 5
            }
        };

        // Act
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<RoslynAnalysisResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ProjectPath.Should().Be("/path/to/project.csproj");
        deserialized.AnalysisTimestamp.Should().Be("2024-01-15T10:30:00Z");
        deserialized.AnalysisDepth.Should().Be(AnalysisDepth.Deep);
        deserialized.SemanticDepth.Should().Be(SemanticAnalysisDepth.Standard);
        deserialized.CompilationStrategy.Should().Be(CompilationStrategy.MSBuild);
        deserialized.Success.Should().BeTrue();
        
        deserialized.ProjectMetadata.Name.Should().Be("TestProject");
        deserialized.ProjectMetadata.ProjectType.Should().Be("Console Application");
        deserialized.ProjectMetadata.TargetFrameworks.Should().Contain("net8.0");
        deserialized.ProjectMetadata.LanguageVersion.Should().Be("12.0");
        deserialized.ProjectMetadata.NullableEnabled.Should().BeTrue();
        
        deserialized.Compilation.Should().NotBeNull();
        deserialized.Compilation!.AssemblyName.Should().Be("TestProject");
        deserialized.Compilation.CompilationSuccessful.Should().BeTrue();
        deserialized.Compilation.SyntaxTreeCount.Should().Be(5);
        deserialized.Compilation.DiagnosticCounts.Errors.Should().Be(0);
        deserialized.Compilation.DiagnosticCounts.Warnings.Should().Be(2);
        
        deserialized.Performance.TotalTimeMs.Should().Be(5000);
        deserialized.Performance.FilesProcessed.Should().Be(5);
    }
}

public class ProjectMetadataTests
{
    [Fact]
    public void ProjectMetadata_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var metadata = new ProjectMetadata();

        // Assert
        metadata.Name.Should().Be(string.Empty);
        metadata.ProjectType.Should().Be(string.Empty);
        metadata.TargetFrameworks.Should().NotBeNull().And.BeEmpty();
        metadata.LanguageVersion.Should().BeNull();
        metadata.SdkVersion.Should().BeNull();
        metadata.NullableEnabled.Should().BeFalse();
        metadata.AssemblyVersion.Should().BeNull();
        metadata.FileVersion.Should().BeNull();
    }

    [Fact]
    public void ProjectMetadata_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var metadata = new ProjectMetadata
        {
            Name = "MyLibrary",
            ProjectType = "Class Library",
            TargetFrameworks = new List<string> { "net8.0", "netstandard2.0" },
            LanguageVersion = "12.0",
            SdkVersion = "8.0.100",
            NullableEnabled = true,
            AssemblyVersion = "2.1.0.0",
            FileVersion = "2.1.0.0"
        };

        // Act
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<ProjectMetadata>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("MyLibrary");
        deserialized.ProjectType.Should().Be("Class Library");
        deserialized.TargetFrameworks.Should().HaveCount(2)
            .And.Contain("net8.0")
            .And.Contain("netstandard2.0");
        deserialized.LanguageVersion.Should().Be("12.0");
        deserialized.SdkVersion.Should().Be("8.0.100");
        deserialized.NullableEnabled.Should().BeTrue();
        deserialized.AssemblyVersion.Should().Be("2.1.0.0");
        deserialized.FileVersion.Should().Be("2.1.0.0");
    }
}

public class ProjectStructureTests
{
    [Fact]
    public void ProjectStructure_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var structure = new ProjectStructure();

        // Assert
        structure.TotalFiles.Should().Be(0);
        structure.TotalLines.Should().Be(0);
        structure.Directories.Should().NotBeNull().And.BeEmpty();
        structure.SourceFiles.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProjectStructure_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var structure = new ProjectStructure
        {
            TotalFiles = 10,
            TotalLines = 1500,
            Directories = new List<DirectoryStructureInfo>
            {
                new() { Name = "Controllers", RelativePath = "Controllers", FileCount = 3, LineCount = 450 },
                new() { Name = "Models", RelativePath = "Models", FileCount = 5, LineCount = 750 }
            },
            SourceFiles = new List<SourceFileInfo>
            {
                new() 
                { 
                    Name = "Program.cs", 
                    RelativePath = "Program.cs", 
                    LineCount = 50, 
                    SizeBytes = 1024, 
                    IsGenerated = false 
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(structure, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<ProjectStructure>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TotalFiles.Should().Be(10);
        deserialized.TotalLines.Should().Be(1500);
        deserialized.Directories.Should().HaveCount(2);
        deserialized.Directories[0].Name.Should().Be("Controllers");
        deserialized.Directories[0].FileCount.Should().Be(3);
        deserialized.SourceFiles.Should().HaveCount(1);
        deserialized.SourceFiles[0].Name.Should().Be("Program.cs");
        deserialized.SourceFiles[0].IsGenerated.Should().BeFalse();
    }
}

public class RecommendationTests
{
    [Fact]
    public void Recommendation_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var recommendation = new Recommendation();

        // Assert
        recommendation.Category.Should().Be(string.Empty);
        recommendation.Priority.Should().Be(string.Empty);
        recommendation.Message.Should().Be(string.Empty);
        recommendation.File.Should().BeNull();
        recommendation.Line.Should().BeNull();
        recommendation.Actionable.Should().BeFalse();
    }

    [Fact]
    public void Recommendation_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var recommendation = new Recommendation
        {
            Category = "Performance",
            Priority = "High",
            Message = "Consider using StringBuilder for string concatenation in loops",
            File = "DataProcessor.cs",
            Line = 42,
            Actionable = true
        };

        // Act
        var json = JsonSerializer.Serialize(recommendation, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<Recommendation>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Category.Should().Be("Performance");
        deserialized.Priority.Should().Be("High");
        deserialized.Message.Should().Be("Consider using StringBuilder for string concatenation in loops");
        deserialized.File.Should().Be("DataProcessor.cs");
        deserialized.Line.Should().Be(42);
        deserialized.Actionable.Should().BeTrue();
    }
}