using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;

namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Factory interface for creating and selecting compilation strategies
/// </summary>
public interface ICompilationStrategyFactory
{
    /// <summary>
    /// Gets the most appropriate compilation strategy for the specified project and options
    /// </summary>
    /// <param name="projectPath">Path to the project file or solution</param>
    /// <param name="options">Analysis options that may influence strategy selection</param>
    /// <returns>The best compilation strategy for the given project and options</returns>
    /// <exception cref="ArgumentException">Thrown when projectPath is null, empty, or invalid</exception>
    /// <exception cref="NotSupportedException">Thrown when no strategy can handle the specified project</exception>
    /// <remarks>
    /// Selection logic:
    /// - CompilationStrategy.Auto: Selects best strategy based on project characteristics and available tools
    /// - CompilationStrategy.MSBuild: Returns MSBuild strategy if available
    /// - CompilationStrategy.Custom: Returns custom compilation strategy
    /// - CompilationStrategy.Hybrid: Returns hybrid strategy that combines MSBuild and custom approaches
    /// </remarks>
    ICompilationStrategy GetStrategy(string projectPath, AnalysisOptions options);
    
    /// <summary>
    /// Gets a specific compilation strategy by type
    /// </summary>
    /// <param name="strategyType">The type of strategy to retrieve</param>
    /// <returns>The requested compilation strategy</returns>
    /// <exception cref="NotSupportedException">Thrown when the requested strategy type is not available</exception>
    ICompilationStrategy GetStrategy(CompilationStrategy strategyType);
    
    /// <summary>
    /// Gets all available compilation strategies
    /// </summary>
    /// <returns>Collection of all registered compilation strategies</returns>
    IEnumerable<ICompilationStrategy> GetAllStrategies();
    
    /// <summary>
    /// Determines if a specific strategy type is available
    /// </summary>
    /// <param name="strategyType">The strategy type to check</param>
    /// <returns>True if the strategy is available; otherwise, false</returns>
    bool IsStrategyAvailable(CompilationStrategy strategyType);
    
    /// <summary>
    /// Registers a new compilation strategy with the factory
    /// </summary>
    /// <param name="strategy">The strategy to register</param>
    /// <exception cref="ArgumentNullException">Thrown when strategy is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when a strategy of the same type is already registered</exception>
    void RegisterStrategy(ICompilationStrategy strategy);
    
    /// <summary>
    /// Gets the strategies that can handle the specified project, ordered by priority
    /// </summary>
    /// <param name="projectPath">Path to the project file or solution</param>
    /// <param name="options">Analysis options that may influence strategy selection</param>
    /// <returns>Collection of compatible strategies ordered by priority (highest first)</returns>
    IEnumerable<ICompilationStrategy> GetCompatibleStrategies(string projectPath, AnalysisOptions options);
}