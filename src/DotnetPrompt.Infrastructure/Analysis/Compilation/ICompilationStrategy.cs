using DotnetPrompt.Infrastructure.Analysis.Models;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Strategy interface for different compilation approaches
/// </summary>
public interface ICompilationStrategy
{
    /// <summary>
    /// Creates a compilation for the given project
    /// </summary>
    /// <param name="projectPath">Path to project file</param>
    /// <param name="options">Compilation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compilation result</returns>
    Task<CompilationResult> CreateCompilationAsync(
        string projectPath, 
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines if this strategy can handle the given project
    /// </summary>
    /// <param name="projectPath">Path to project file</param>
    /// <param name="options">Analysis options</param>
    /// <returns>True if strategy can handle the project</returns>
    bool CanHandle(string projectPath, AnalysisOptions options);
    
    /// <summary>
    /// Strategy type identifier
    /// </summary>
    CompilationStrategy StrategyType { get; }
}