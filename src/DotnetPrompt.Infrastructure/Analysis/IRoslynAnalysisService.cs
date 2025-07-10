using DotnetPrompt.Infrastructure.Analysis.Models;

namespace DotnetPrompt.Infrastructure.Analysis;

/// <summary>
/// Main interface for Roslyn-based project analysis
/// </summary>
public interface IRoslynAnalysisService
{
    /// <summary>
    /// Analyzes a .NET project using Roslyn
    /// </summary>
    /// <param name="projectPath">Path to project file, solution, or directory</param>
    /// <param name="options">Analysis configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive analysis result as JSON string</returns>
    Task<string> AnalyzeAsync(
        string projectPath,
        AnalysisOptions options,
        CancellationToken cancellationToken = default);
}