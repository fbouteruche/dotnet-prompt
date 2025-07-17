using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using Microsoft.CodeAnalysis;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Internal interface that extends ICompilationStrategy with access to raw Roslyn artifacts
/// This is a bridge pattern to allow Infrastructure semantic analysis while maintaining Clean Architecture
/// </summary>
internal interface IRoslynCompilationStrategy : ICompilationStrategy
{
    /// <summary>
    /// Creates a compilation and provides access to the raw Roslyn compilation for semantic analysis
    /// </summary>
    /// <param name="projectPath">Path to the project file or solution</param>
    /// <param name="options">Compilation-specific options</param>
    /// <param name="cancellationToken">Cancellation token for operation cancellation</param>
    /// <returns>Tuple containing both the domain result and raw Roslyn compilation</returns>
    Task<(CompilationResult Result, Microsoft.CodeAnalysis.Compilation? RoslynCompilation)> CreateCompilationWithRoslynAsync(
        string projectPath, 
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default);
}