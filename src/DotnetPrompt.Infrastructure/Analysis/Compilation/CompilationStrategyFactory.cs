using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Factory for creating appropriate compilation strategies based on project characteristics
/// </summary>
public class CompilationStrategyFactory : ICompilationStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompilationStrategyFactory> _logger;
    private readonly Dictionary<CompilationStrategy, Type> _strategies;

    public CompilationStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<CompilationStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Register available strategies
        _strategies = new Dictionary<CompilationStrategy, Type>
        {
            { CompilationStrategy.MSBuild, typeof(MSBuildWorkspaceStrategy) },
            { CompilationStrategy.Custom, typeof(CustomCompilationStrategy) }
        };
    }

    /// <summary>
    /// Gets the most appropriate compilation strategy for the specified project and options
    /// </summary>
    public ICompilationStrategy GetStrategy(string projectPath, AnalysisOptions options)
    {
        var preferredStrategy = DeterminePreferredStrategy(projectPath, options);
        
        return preferredStrategy switch
        {
            CompilationStrategy.MSBuild => _serviceProvider.GetRequiredService<MSBuildWorkspaceStrategy>(),
            CompilationStrategy.Custom => _serviceProvider.GetRequiredService<CustomCompilationStrategy>(),
            CompilationStrategy.Auto or CompilationStrategy.Hybrid => SelectAutoStrategy(projectPath, options),
            _ => _serviceProvider.GetRequiredService<MSBuildWorkspaceStrategy>() // Default fallback
        };
    }

    /// <summary>
    /// Gets a specific compilation strategy by type
    /// </summary>
    public ICompilationStrategy GetStrategy(CompilationStrategy strategyType)
    {
        if (!_strategies.TryGetValue(strategyType, out var strategyTypeInfo))
        {
            throw new NotSupportedException($"Compilation strategy '{strategyType}' is not available");
        }

        return (ICompilationStrategy)_serviceProvider.GetRequiredService(strategyTypeInfo);
    }

    /// <summary>
    /// Gets all available compilation strategies
    /// </summary>
    public IEnumerable<ICompilationStrategy> GetAllStrategies()
    {
        return _strategies.Values.Select(type => (ICompilationStrategy)_serviceProvider.GetRequiredService(type));
    }

    /// <summary>
    /// Determines if a specific strategy type is available
    /// </summary>
    public bool IsStrategyAvailable(CompilationStrategy strategyType)
    {
        return _strategies.ContainsKey(strategyType);
    }

    /// <summary>
    /// Registers a new compilation strategy with the factory
    /// </summary>
    public void RegisterStrategy(ICompilationStrategy strategy)
    {
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));
            
        if (_strategies.ContainsKey(strategy.StrategyType))
        {
            throw new InvalidOperationException($"Strategy '{strategy.StrategyType}' is already registered");
        }

        _strategies[strategy.StrategyType] = strategy.GetType();
    }

    /// <summary>
    /// Gets the strategies that can handle the specified project, ordered by priority
    /// </summary>
    public IEnumerable<ICompilationStrategy> GetCompatibleStrategies(string projectPath, AnalysisOptions options)
    {
        var allStrategies = GetAllStrategies().ToList();
        
        return allStrategies
            .Where(s => s.CanHandle(projectPath, options))
            .OrderByDescending(s => s.Priority);
    }

    private CompilationStrategy DeterminePreferredStrategy(string projectPath, AnalysisOptions options)
    {
        // If explicitly set, respect the user's choice
        if (options.CompilationStrategy != CompilationStrategy.Auto)
        {
            return options.CompilationStrategy;
        }

        // Auto selection logic
        return CompilationStrategy.Auto;
    }

    private ICompilationStrategy SelectAutoStrategy(string projectPath, AnalysisOptions options)
    {
        _logger.LogDebug("Auto-selecting compilation strategy for {ProjectPath}", projectPath);

        // Strategy selection heuristics
        var useMSBuild = ShouldUseMSBuildStrategy(projectPath, options);
        
        if (useMSBuild)
        {
            _logger.LogDebug("Selected MSBuild strategy for {ProjectPath}", projectPath);
            return _serviceProvider.GetRequiredService<MSBuildWorkspaceStrategy>();
        }
        else
        {
            _logger.LogDebug("Selected Custom strategy for {ProjectPath}", projectPath);
            return _serviceProvider.GetRequiredService<CustomCompilationStrategy>();
        }
    }

    private static bool ShouldUseMSBuildStrategy(string projectPath, AnalysisOptions options)
    {
        // Prefer MSBuild for:
        // - Solution files (.sln)
        // - Project files (.csproj)
        // - When semantic analysis is requested
        // - When dependency analysis is needed
        
        if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Solutions require MSBuild
        }

        if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Project files work best with MSBuild
        }

        if (options.SemanticDepth != SemanticAnalysisDepth.None)
        {
            return true; // Semantic analysis benefits from MSBuild compilation
        }

        if (options.IncludeDependencies)
        {
            return true; // Dependency analysis requires MSBuild project loading
        }

        if (options.IncludeMetrics && options.SemanticDepth != SemanticAnalysisDepth.None)
        {
            return true; // Complex metrics need semantic compilation
        }

        // Use Custom for lightweight scenarios
        return false;
    }
}