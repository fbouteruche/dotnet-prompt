using System.ComponentModel;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Semantic Kernel plugin for comprehensive .NET project analysis using Roslyn
/// Replaces basic XML parsing with full Roslyn Compiler Platform integration
/// </summary>
public class ProjectAnalysisPlugin
{
    private readonly ILogger<ProjectAnalysisPlugin> _logger;
    private readonly IRoslynAnalysisService _roslynAnalysisService;

    public ProjectAnalysisPlugin(
        ILogger<ProjectAnalysisPlugin> logger,
        IRoslynAnalysisService roslynAnalysisService)
    {
        _logger = logger;
        _roslynAnalysisService = roslynAnalysisService;
    }

    [KernelFunction("analyze_with_roslyn")]
    [Description("Comprehensive .NET project analysis using Roslyn for AI workflows")]
    [return: Description("JSON object containing comprehensive project analysis results optimized for AI consumption")]
    public async Task<string> AnalyzeProjectAsync(
        [Description("Path to .csproj, .sln, or source directory")] string project_path,
        [Description("Analysis depth: Surface, Standard, Deep, Comprehensive")] string analysis_depth = "Standard",
        [Description("Semantic analysis depth: None, Basic, Standard, Deep, Comprehensive")] string semantic_depth = "None",
        [Description("Compilation strategy: Auto, MSBuild, Custom, Hybrid")] string compilation_strategy = "Auto",
        [Description("Include dependency analysis")] bool include_dependencies = true,
        [Description("Include code quality metrics")] bool include_metrics = true,
        [Description("Include architectural pattern detection")] bool include_patterns = false,
        [Description("Include security vulnerability scanning")] bool include_vulnerabilities = false,
        [Description("Specific target framework to analyze")] string? target_framework = null,
        [Description("Maximum recursion depth for references")] int max_depth = 5,
        [Description("Exclude auto-generated code from analysis")] bool exclude_generated = true,
        [Description("Include test projects in analysis")] bool include_tests = true,
        [Description("MSBuild workspace timeout in milliseconds")] int msbuild_timeout = 30000,
        [Description("Fallback to custom compilation if MSBuild fails")] bool fallback_to_custom = true,
        [Description("Use lightweight custom compilation mode")] bool lightweight_mode = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Roslyn project analysis via SK function: {ProjectPath}", project_path);
            
            // Validate project path
            var validatedPath = ValidateProjectPath(project_path);
            
            // Parse analysis options from parameters
            var options = new AnalysisOptions
            {
                AnalysisDepth = ParseAnalysisDepth(analysis_depth),
                SemanticDepth = ParseSemanticDepth(semantic_depth),
                CompilationStrategy = ParseCompilationStrategy(compilation_strategy),
                IncludeDependencies = include_dependencies,
                IncludeMetrics = include_metrics,
                IncludePatterns = include_patterns,
                IncludeVulnerabilities = include_vulnerabilities,
                TargetFramework = target_framework,
                MaxDepth = max_depth,
                ExcludeGenerated = exclude_generated,
                IncludeTests = include_tests,
                MSBuildTimeout = msbuild_timeout,
                FallbackToCustom = fallback_to_custom,
                LightweightMode = lightweight_mode
            };
            
            // Perform Roslyn analysis
            var result = await _roslynAnalysisService.AnalyzeAsync(validatedPath, options, cancellationToken);
            
            _logger.LogInformation("Successfully completed Roslyn analysis for {ProjectPath}", validatedPath);
            
            // Serialize result for AI consumption
            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Roslyn analysis for {ProjectPath} via SK function", project_path);
            throw new KernelException($"Roslyn project analysis failed: {ex.Message}", ex);
        }
    }

    private string ValidateProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));
        }

        var resolvedPath = Path.IsPathRooted(projectPath) ? projectPath : Path.GetFullPath(projectPath);
        
        // Check if it's a valid project file, solution file, or directory
        if (File.Exists(resolvedPath))
        {
            var extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
            if (!new[] { ".csproj", ".fsproj", ".vbproj", ".sln" }.Contains(extension))
            {
                throw new ArgumentException($"Invalid project file extension: {extension}. Expected .csproj, .fsproj, .vbproj, or .sln");
            }
        }
        else if (!Directory.Exists(resolvedPath))
        {
            throw new ArgumentException($"Project path does not exist: {resolvedPath}");
        }

        return resolvedPath;
    }

    private static AnalysisDepth ParseAnalysisDepth(string depth)
    {
        return depth.ToLowerInvariant() switch
        {
            "surface" => AnalysisDepth.Surface,
            "standard" => AnalysisDepth.Standard,
            "deep" => AnalysisDepth.Deep,
            "comprehensive" => AnalysisDepth.Comprehensive,
            _ => AnalysisDepth.Standard
        };
    }

    private static SemanticAnalysisDepth ParseSemanticDepth(string depth)
    {
        return depth.ToLowerInvariant() switch
        {
            "none" => SemanticAnalysisDepth.None,
            "basic" => SemanticAnalysisDepth.Basic,
            "standard" => SemanticAnalysisDepth.Standard,
            "deep" => SemanticAnalysisDepth.Deep,
            "comprehensive" => SemanticAnalysisDepth.Comprehensive,
            _ => SemanticAnalysisDepth.None
        };
    }

    private static CompilationStrategy ParseCompilationStrategy(string strategy)
    {
        return strategy.ToLowerInvariant() switch
        {
            "auto" => CompilationStrategy.Auto,
            "msbuild" => CompilationStrategy.MSBuild,
            "custom" => CompilationStrategy.Custom,
            "hybrid" => CompilationStrategy.Hybrid,
            _ => CompilationStrategy.Auto
        };
    }
}