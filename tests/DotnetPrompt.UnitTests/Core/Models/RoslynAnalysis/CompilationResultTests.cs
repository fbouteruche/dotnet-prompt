using System.Text.Json;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Models.RoslynAnalysis;

public class CompilationResultTests
{
    [Fact]
    public void CompilationResult_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var result = new CompilationResult();

        // Assert
        result.Success.Should().BeFalse();
        result.StrategyUsed.Should().Be(default(CompilationStrategy));
        result.FallbackUsed.Should().BeFalse();
        result.CompilationTimeMs.Should().Be(0);
        result.Diagnostics.Should().NotBeNull();
        result.WorkspaceDiagnostics.Should().NotBeNull().And.BeEmpty();
        result.ProjectMetadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CompilationResult_SuccessConstructor_SetsCorrectValues()
    {
        // Arrange & Act
        var result = new CompilationResult(CompilationStrategy.MSBuild, "TestAssembly");

        // Assert
        result.Success.Should().BeTrue();
        result.StrategyUsed.Should().Be(CompilationStrategy.MSBuild);
        result.AssemblyName.Should().Be("TestAssembly");
    }

    [Fact]
    public void CompilationResult_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var result = new CompilationResult(CompilationStrategy.Hybrid, "MyAssembly")
        {
            FallbackUsed = true,
            FallbackReason = "MSBuild failed",
            CompilationTimeMs = 5000,
            TargetFramework = "net8.0",
            ErrorMessage = null,
            Diagnostics = new DiagnosticsSummary
            {
                ErrorCount = 2,
                WarningCount = 5,
                InfoCount = 10
            },
            ProjectMetadata = new Dictionary<string, object>
            {
                { "ProjectName", "TestProject" },
                { "Language", "C#" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<CompilationResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Success.Should().BeTrue();
        deserialized.StrategyUsed.Should().Be(CompilationStrategy.Hybrid);
        deserialized.AssemblyName.Should().Be("MyAssembly");
        deserialized.FallbackUsed.Should().BeTrue();
        deserialized.FallbackReason.Should().Be("MSBuild failed");
        deserialized.CompilationTimeMs.Should().Be(5000);
        deserialized.TargetFramework.Should().Be("net8.0");
        deserialized.Diagnostics.ErrorCount.Should().Be(2);
        deserialized.Diagnostics.WarningCount.Should().Be(5);
        deserialized.Diagnostics.InfoCount.Should().Be(10);
        deserialized.ProjectMetadata.Should().ContainKey("ProjectName");
    }
}

public class DiagnosticsSummaryTests
{
    [Fact]
    public void DiagnosticsSummary_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var summary = new DiagnosticsSummary();

        // Assert
        summary.ErrorCount.Should().Be(0);
        summary.WarningCount.Should().Be(0);
        summary.InfoCount.Should().Be(0);
        summary.CriticalErrors.Should().NotBeNull().And.BeEmpty();
        summary.SignificantWarnings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void DiagnosticsSummary_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var summary = new DiagnosticsSummary
        {
            ErrorCount = 3,
            WarningCount = 7,
            InfoCount = 12,
            CriticalErrors = new List<DiagnosticInfo>
            {
                new() { Id = "CS0001", Message = "Test error", Severity = "Error", FilePath = "test.cs", Line = 10 }
            },
            SignificantWarnings = new List<DiagnosticInfo>
            {
                new() { Id = "CS0162", Message = "Unreachable code", Severity = "Warning", FilePath = "test.cs", Line = 20 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<DiagnosticsSummary>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ErrorCount.Should().Be(3);
        deserialized.WarningCount.Should().Be(7);
        deserialized.InfoCount.Should().Be(12);
        deserialized.CriticalErrors.Should().HaveCount(1);
        deserialized.CriticalErrors[0].Id.Should().Be("CS0001");
        deserialized.CriticalErrors[0].Message.Should().Be("Test error");
        deserialized.CriticalErrors[0].FilePath.Should().Be("test.cs");
        deserialized.CriticalErrors[0].Line.Should().Be(10);
        deserialized.SignificantWarnings.Should().HaveCount(1);
        deserialized.SignificantWarnings[0].Id.Should().Be("CS0162");
    }
}

public class DiagnosticInfoTests
{
    [Fact]
    public void DiagnosticInfo_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var diagnostic = new DiagnosticInfo();

        // Assert
        diagnostic.Id.Should().Be(string.Empty);
        diagnostic.Message.Should().Be(string.Empty);
        diagnostic.Severity.Should().Be(string.Empty);
        diagnostic.FilePath.Should().BeNull();
        diagnostic.Line.Should().BeNull();
        diagnostic.Column.Should().BeNull();
    }

    [Fact]
    public void DiagnosticInfo_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var diagnostic = new DiagnosticInfo
        {
            Id = "CS0103",
            Message = "The name 'undefinedVariable' does not exist in the current context",
            Severity = "Error",
            FilePath = "/src/Program.cs",
            Line = 15,
            Column = 25
        };

        // Act
        var json = JsonSerializer.Serialize(diagnostic, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<DiagnosticInfo>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("CS0103");
        deserialized.Message.Should().Be("The name 'undefinedVariable' does not exist in the current context");
        deserialized.Severity.Should().Be("Error");
        deserialized.FilePath.Should().Be("/src/Program.cs");
        deserialized.Line.Should().Be(15);
        deserialized.Column.Should().Be(25);
    }
}