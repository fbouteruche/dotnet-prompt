using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Strategy pattern interface for different compilation approaches
/// </summary>
public interface ICompilationStrategy
{
    /// <summary>
    /// Creates a compilation for the specified project using this strategy
    /// </summary>
    /// <param name="projectPath">Path to the project file or solution</param>
    /// <param name="options">Compilation-specific options</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Compilation result containing either successful compilation or error information</returns>
    /// <exception cref="ArgumentException">Thrown when projectPath is null, empty, or invalid</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified project file is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when the strategy cannot create compilation</exception>
    Task<CompilationResult> CreateCompilationAsync(
        string projectPath, 
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether this strategy can handle the specified project and options
    /// </summary>
    /// <param name="projectPath">Path to the project file or solution</param>
    /// <param name="options">Analysis options that may affect strategy selection</param>
    /// <returns>True if this strategy can handle the project; otherwise, false</returns>
    /// <remarks>
    /// This method should perform lightweight checks (file existence, project type detection)
    /// without performing full project loading or compilation
    /// </remarks>
    bool CanHandle(string projectPath, AnalysisOptions options);
    
    /// <summary>
    /// Gets the strategy type that this implementation represents
    /// </summary>
    CompilationStrategy StrategyType { get; }
    
    /// <summary>
    /// Gets the priority of this strategy for automatic selection
    /// </summary>
    /// <remarks>
    /// Higher values indicate higher priority. Used by factory when multiple
    /// strategies can handle the same project. MSBuild typically has highest priority.
    /// </remarks>
    int Priority { get; }
    
    /// <summary>
    /// Gets a human-readable description of this compilation strategy
    /// </summary>
    string Description { get; }
}