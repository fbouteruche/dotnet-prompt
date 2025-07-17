using DotnetPrompt.Core.Models.RoslynAnalysis;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Primary interface for Roslyn-based .NET project analysis
/// </summary>
public interface IRoslynAnalysisService
{
    /// <summary>
    /// Performs comprehensive analysis of a .NET project or solution
    /// </summary>
    /// <param name="projectPath">Path to the project file (.csproj), solution file (.sln), or source directory</param>
    /// <param name="options">Analysis configuration options</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Comprehensive analysis results optimized for AI workflow consumption</returns>
    /// <exception cref="ArgumentException">Thrown when projectPath is null, empty, or invalid</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified project or solution file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when MSBuild cannot be initialized or located</exception>
    /// <exception cref="NotSupportedException">Thrown when the project type is not supported for analysis</exception>
    Task<RoslynAnalysisResult> AnalyzeAsync(
        string projectPath, 
        AnalysisOptions options, 
        CancellationToken cancellationToken = default);
}