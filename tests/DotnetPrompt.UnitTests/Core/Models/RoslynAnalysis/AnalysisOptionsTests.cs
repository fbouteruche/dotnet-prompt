using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Models.RoslynAnalysis;

public class AnalysisOptionsTests
{
    [Fact]
    public void AnalysisOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new AnalysisOptions();

        // Assert
        options.AnalysisDepth.Should().Be(AnalysisDepth.Standard);
        options.SemanticDepth.Should().Be(SemanticAnalysisDepth.None);
        options.CompilationStrategy.Should().Be(CompilationStrategy.Auto);
        options.IncludeDependencies.Should().BeTrue();
        options.IncludeMetrics.Should().BeTrue();
        options.IncludePatterns.Should().BeFalse();
        options.IncludeVulnerabilities.Should().BeFalse();
        options.MaxDepth.Should().Be(5);
        options.ExcludeGenerated.Should().BeTrue();
        options.IncludeTests.Should().BeTrue();
        options.MSBuildTimeout.Should().Be(30000);
        options.FallbackToCustom.Should().BeTrue();
        options.LightweightMode.Should().BeFalse();
    }

    [Fact]
    public void AnalysisOptions_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var options = new AnalysisOptions
        {
            AnalysisDepth = AnalysisDepth.Deep,
            SemanticDepth = SemanticAnalysisDepth.Standard,
            CompilationStrategy = CompilationStrategy.MSBuild,
            IncludeDependencies = false,
            TargetFramework = "net8.0",
            MaxDepth = 10
        };

        // Act
        var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<AnalysisOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AnalysisDepth.Should().Be(AnalysisDepth.Deep);
        deserialized.SemanticDepth.Should().Be(SemanticAnalysisDepth.Standard);
        deserialized.CompilationStrategy.Should().Be(CompilationStrategy.MSBuild);
        deserialized.IncludeDependencies.Should().BeFalse();
        deserialized.TargetFramework.Should().Be("net8.0");
        deserialized.MaxDepth.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void AnalysisOptions_MaxDepthValidation_FailsForInvalidValues(int invalidDepth)
    {
        // Arrange
        var options = new AnalysisOptions { MaxDepth = invalidDepth };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(AnalysisOptions.MaxDepth)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public void AnalysisOptions_MaxDepthValidation_PassesForValidValues(int validDepth)
    {
        // Arrange
        var options = new AnalysisOptions { MaxDepth = validDepth };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().NotContain(r => r.MemberNames.Contains(nameof(AnalysisOptions.MaxDepth)));
    }
}

public class AnalysisCompilationOptionsTests
{
    [Fact]
    public void AnalysisCompilationOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new AnalysisCompilationOptions();

        // Assert
        options.MSBuildProperties.Should().BeEmpty();
        options.ReferenceAssemblies.Should().BeEmpty();
        options.PreprocessorSymbols.Should().BeEmpty();
        options.CompilationTimeout.Should().Be(60000);
        options.ExcludeGenerated.Should().BeTrue();
        options.NullableContext.Should().BeTrue();
        options.TargetFramework.Should().BeNull();
        options.LanguageVersion.Should().BeNull();
    }

    [Fact]
    public void AnalysisCompilationOptions_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var options = new AnalysisCompilationOptions
        {
            TargetFramework = "net8.0",
            LanguageVersion = "12.0",
            MSBuildProperties = new Dictionary<string, string> { { "Configuration", "Release" } },
            ReferenceAssemblies = new List<string> { "System.Data.dll" },
            PreprocessorSymbols = new List<string> { "DEBUG", "TRACE" },
            CompilationTimeout = 120000,
            ExcludeGenerated = false,
            NullableContext = false
        };

        // Act
        var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<AnalysisCompilationOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TargetFramework.Should().Be("net8.0");
        deserialized.LanguageVersion.Should().Be("12.0");
        deserialized.MSBuildProperties.Should().ContainKey("Configuration").WhoseValue.Should().Be("Release");
        deserialized.ReferenceAssemblies.Should().Contain("System.Data.dll");
        deserialized.PreprocessorSymbols.Should().Contain("DEBUG").And.Contain("TRACE");
        deserialized.CompilationTimeout.Should().Be(120000);
        deserialized.ExcludeGenerated.Should().BeFalse();
        deserialized.NullableContext.Should().BeFalse();
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(700000)]
    public void AnalysisCompilationOptions_CompilationTimeoutValidation_FailsForInvalidValues(int invalidTimeout)
    {
        // Arrange
        var options = new AnalysisCompilationOptions { CompilationTimeout = invalidTimeout };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(AnalysisCompilationOptions.CompilationTimeout)));
    }
}