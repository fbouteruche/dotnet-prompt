using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Infrastructure.Analysis.Compilation;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotnetPrompt.Infrastructure.Analysis;

/// <summary>
/// Main implementation of Roslyn-based project analysis service
/// </summary>
public class RoslynAnalysisService : IRoslynAnalysisService
{
    private readonly ILogger<RoslynAnalysisService> _logger;
    private readonly ICompilationStrategyFactory _compilationStrategyFactory;

    public RoslynAnalysisService(
        ILogger<RoslynAnalysisService> logger,
        ICompilationStrategyFactory compilationStrategyFactory)
    {
        _logger = logger;
        _compilationStrategyFactory = compilationStrategyFactory;
    }

    public async Task<RoslynAnalysisResult> AnalyzeAsync(
        string projectPath,
        AnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        var result = new RoslynAnalysisResult
        {
            ProjectPath = projectPath,
            AnalysisTimestamp = startTime.ToString("O"),
            AnalysisDepth = options.AnalysisDepth,
            SemanticDepth = options.SemanticDepth,
            CompilationStrategy = options.CompilationStrategy
        };

        try
        {
            _logger.LogInformation("Starting Roslyn analysis for {ProjectPath} with depth {SemanticDepth}", 
                projectPath, options.SemanticDepth);

            // Phase 1: Always perform structural analysis (fast)
            await PerformStructuralAnalysis(result, projectPath, options, cancellationToken);

            // Phase 2: Conditional compilation and semantic analysis
            if (RequiresCompilation(options))
            {
                await PerformCompilationAnalysis(result, projectPath, options, cancellationToken);
            }

            // Phase 3: Generate AI-friendly recommendations
            result.Recommendations = GenerateRecommendations(result, options);

            // Phase 4: Calculate performance metrics
            stopwatch.Stop();
            result.Performance = CalculatePerformanceMetrics(stopwatch.ElapsedMilliseconds);
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Success = true;

            _logger.LogInformation("Roslyn analysis completed for {ProjectPath} in {Duration}ms", 
                projectPath, result.ExecutionTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Roslyn analysis failed for {ProjectPath}", projectPath);

            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }
    }

    private async Task PerformStructuralAnalysis(
        RoslynAnalysisResult result,
        string projectPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        // Extract project metadata
        result.ProjectMetadata = await ExtractProjectMetadata(projectPath, cancellationToken);

        // Analyze project structure
        result.Structure = await AnalyzeProjectStructure(projectPath, cancellationToken);

        // Analyze dependencies
        if (options.IncludeDependencies)
        {
            result.Dependencies = await AnalyzeDependencies(projectPath, cancellationToken);
        }
    }

    private async Task PerformCompilationAnalysis(
        RoslynAnalysisResult result,
        string projectPath,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var compilationOptions = new AnalysisCompilationOptions
            {
                MSBuildTimeout = options.MSBuildTimeout,
                FallbackToCustom = options.FallbackToCustom,
                LightweightMode = options.LightweightMode,
                TargetFramework = options.TargetFramework,
                IncludeTests = options.IncludeTests,
                ExcludeGenerated = options.ExcludeGenerated
            };

            // Get the appropriate compilation strategy
            var strategy = _compilationStrategyFactory.GetStrategy(projectPath, options);
            
            // Check if strategy supports Roslyn bridge interface
            if (strategy is IRoslynCompilationStrategy roslynStrategy)
            {
                var (compilationResult, roslynCompilation) = await roslynStrategy.CreateCompilationWithRoslynAsync(
                    projectPath, compilationOptions, cancellationToken);

                result.Compilation = MapCompilationInfo(compilationResult);

                // Perform semantic analysis if compilation succeeded and requested
                if (compilationResult.Success && 
                    roslynCompilation != null && 
                    options.SemanticDepth != SemanticAnalysisDepth.None)
                {
                    result.Semantics = await PerformSemanticAnalysis(
                        roslynCompilation, options, cancellationToken);
                }

                // Perform metrics analysis if requested
                if (compilationResult.Success && 
                    roslynCompilation != null && 
                    options.IncludeMetrics)
                {
                    result.Metrics = await CalculateMetrics(
                        roslynCompilation, options, cancellationToken);
                }
            }
            else
            {
                // Fallback for strategies that don't support Roslyn bridge
                var compilationResult = await strategy.CreateCompilationAsync(
                    projectPath, compilationOptions, cancellationToken);
                
                result.Compilation = MapCompilationInfo(compilationResult);
                
                _logger.LogWarning("Strategy {StrategyType} doesn't support semantic analysis", strategy.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Compilation analysis failed for {ProjectPath}, continuing with structural analysis only", projectPath);
            // Don't fail the entire analysis - just log and continue
        }
    }

    private async Task<ProjectMetadata> ExtractProjectMetadata(string projectPath, CancellationToken cancellationToken)
    {
        var metadata = new ProjectMetadata();
        
        try
        {
            if (File.Exists(projectPath) && projectPath.EndsWith(".csproj"))
            {
                var content = await File.ReadAllTextAsync(projectPath, cancellationToken);
                
                metadata.Name = Path.GetFileNameWithoutExtension(projectPath);
                metadata.ProjectType = DetermineProjectType(content);
                // Note: SdkStyle and OutputType properties have been removed from ProjectMetadata
                
                var targetFramework = ExtractTargetFramework(content);
                if (!string.IsNullOrEmpty(targetFramework))
                {
                    metadata.TargetFrameworks.Add(targetFramework);
                }
                
                var targetFrameworks = ExtractTargetFrameworks(content);
                if (targetFrameworks.Any())
                {
                    metadata.TargetFrameworks.AddRange(targetFrameworks);
                }
            }
            else if (Directory.Exists(projectPath))
            {
                metadata.Name = Path.GetFileName(projectPath.TrimEnd(Path.DirectorySeparatorChar));
                metadata.ProjectType = "Directory";
            }
            
            // Note: File and line counting logic has been moved to ProjectStructure population
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract complete project metadata for {ProjectPath}", projectPath);
        }
        
        return metadata;
    }

    private async Task<ProjectStructure> AnalyzeProjectStructure(string projectPath, CancellationToken cancellationToken)
    {
        var structure = new ProjectStructure();
        
        try
        {
            var directory = File.Exists(projectPath) ? Path.GetDirectoryName(projectPath)! : projectPath;
            
            if (Directory.Exists(directory))
            {
                // Analyze directories
                var subdirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
                foreach (var subdir in subdirectories)
                {
                    var dirName = Path.GetFileName(subdir);
                    if (!IsIgnoredDirectory(dirName))
                    {
                        var fileCount = Directory.GetFiles(subdir, "*.cs", SearchOption.AllDirectories).Length;
                        structure.Directories.Add(new DirectoryStructureInfo { Name = dirName, RelativePath = dirName, FileCount = fileCount });
                    }
                }
                
                // Analyze source files
                var sourceFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                foreach (var file in sourceFiles.Take(50)) // Limit for performance
                {
                    try
                    {
                        var lines = await File.ReadAllLinesAsync(file, cancellationToken);
                        var relativePath = Path.GetRelativePath(directory, file);
                        var fileType = DetermineFileType(file, lines);
                        
                        structure.SourceFiles.Add(new SourceFileInfo 
                        { 
                            Name = Path.GetFileName(file),
                            RelativePath = relativePath, 
                            LineCount = lines.Length,
                            SizeBytes = new FileInfo(file).Length
                        });
                    }
                    catch
                    {
                        // Ignore file read errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze project structure for {ProjectPath}", projectPath);
        }
        
        return structure;
    }

    private async Task<DependencyAnalysis> AnalyzeDependencies(string projectPath, CancellationToken cancellationToken)
    {
        var dependencies = new DependencyAnalysis();
        
        try
        {
            if (File.Exists(projectPath) && projectPath.EndsWith(".csproj"))
            {
                var content = await File.ReadAllTextAsync(projectPath, cancellationToken);
                
                // Extract package references
                var packageReferences = ExtractPackageReferences(content);
                dependencies.PackageReferences.AddRange(packageReferences);
                
                // Extract project references
                var projectReferences = ExtractProjectReferences(content);
                dependencies.ProjectReferences.AddRange(projectReferences);
                
                dependencies.DependencyCounts.DirectPackages = packageReferences.Count;
                dependencies.DependencyCounts.ProjectReferences = projectReferences.Count;
                // Note: Transitive dependencies would require more complex analysis
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze dependencies for {ProjectPath}", projectPath);
        }
        
        return dependencies;
    }

    private async Task<SemanticAnalysis> PerformSemanticAnalysis(
        Microsoft.CodeAnalysis.Compilation compilation,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var analysis = new SemanticAnalysis();
        
        try
        {
            // Basic type counting
            var typeCount = new TypeCount();
            var namespaces = new List<NamespaceInfo>();
            
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(cancellationToken);
                
                // This is a simplified implementation - would need full Roslyn tree walking
                // for comprehensive semantic analysis
                
                await Task.CompletedTask; // Placeholder for async compliance
            }
            
            analysis.TypeCounts = typeCount;
            analysis.Namespaces = namespaces;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic analysis failed, returning basic structure");
        }
        
        return analysis;
    }

    private async Task<CodeMetrics> CalculateMetrics(
        Microsoft.CodeAnalysis.Compilation compilation,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        var metrics = new CodeMetrics();
        
        try
        {
            // Basic metrics calculation - would need full implementation for comprehensive metrics
            var complexity = new ComplexityMetrics();
            var maintainability = new MaintainabilityMetrics();
            var size = new SizeMetrics();
            
            // Count total lines across all syntax trees
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var text = await syntaxTree.GetTextAsync(cancellationToken);
                size.LinesOfCode += text.Lines.Count;
            }
            
            metrics.Complexity = complexity;
            metrics.Maintainability = maintainability;
            metrics.Size = size;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metrics calculation failed");
        }
        
        return metrics;
    }

    private bool RequiresCompilation(AnalysisOptions options)
    {
        return options.SemanticDepth != SemanticAnalysisDepth.None ||
               options.IncludeMetrics ||
               options.IncludePatterns ||
               options.IncludeVulnerabilities;
    }

    private CompilationInfo MapCompilationInfo(CompilationResult compilation)
    {
        return new CompilationInfo
        {
            CompilationSuccessful = compilation.Success,
            AssemblyName = compilation.AssemblyName ?? "Unknown",
            DiagnosticCounts = new DiagnosticCount
            {
                Errors = compilation.Diagnostics.ErrorCount,
                Warnings = compilation.Diagnostics.WarningCount,
                Info = compilation.Diagnostics.InfoCount
            }
        };
    }

    private List<Recommendation> GenerateRecommendations(RoslynAnalysisResult result, AnalysisOptions options)
    {
        var recommendations = new List<Recommendation>();
        
        // Basic recommendations based on analysis results
        if (result.Compilation?.DiagnosticCounts.Errors > 0)
        {
            recommendations.Add(new Recommendation
            {
                Category = "Compilation",
                Priority = "High",
                Message = $"Project has {result.Compilation.DiagnosticCounts.Errors} compilation errors that should be addressed",
                Actionable = true
            });
        }
        
        if (result.Compilation?.DiagnosticCounts.Warnings > 10)
        {
            recommendations.Add(new Recommendation
            {
                Category = "Quality",
                Priority = "Medium",
                Message = $"Project has {result.Compilation.DiagnosticCounts.Warnings} warnings - consider addressing them",
                Actionable = true
            });
        }
        
        return recommendations;
    }

    private PerformanceMetrics CalculatePerformanceMetrics(long totalTimeMs)
    {
        return new PerformanceMetrics
        {
            TotalAnalysisTimeMs = totalTimeMs,
            CompilationTimeMs = 0, // Would be populated from actual compilation timing
            SemanticAnalysisTimeMs = 0,
            MetricsCalculationTimeMs = 0,
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
        };
    }

    private string SerializeResult(RoslynAnalysisResult result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    // Helper methods for project file parsing
    private static string DetermineProjectType(string projectContent)
    {
        if (projectContent.Contains("<OutputType>Exe</OutputType>") || 
            projectContent.Contains("<OutputType>exe</OutputType>"))
            return "Console";
        if (projectContent.Contains("Microsoft.AspNetCore") || 
            projectContent.Contains("Microsoft.NET.Sdk.Web"))
            return "Web";
        if (projectContent.Contains("Microsoft.NET.Test.Sdk"))
            return "Test";
        return "Library";
    }

    private static string? ExtractTargetFramework(string content)
    {
        var match = Regex.Match(content, @"<TargetFramework[^>]*>([^<]+)</TargetFramework>");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<string> ExtractTargetFrameworks(string content)
    {
        var match = Regex.Match(content, @"<TargetFrameworks[^>]*>([^<]+)</TargetFrameworks>");
        if (match.Success)
        {
            return match.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        return new List<string>();
    }

    private static string? ExtractOutputType(string content)
    {
        var match = Regex.Match(content, @"<OutputType[^>]*>([^<]+)</OutputType>");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<PackageReference> ExtractPackageReferences(string content)
    {
        var packages = new List<PackageReference>();
        var matches = Regex.Matches(content, @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""[^>]*>");
        
        foreach (Match match in matches)
        {
            packages.Add(new PackageReference
            {
                Name = match.Groups[1].Value,
                Version = match.Groups[2].Value,
                Type = "PackageReference"
            });
        }
        
        return packages;
    }

    private static List<ProjectReference> ExtractProjectReferences(string content)
    {
        var projects = new List<ProjectReference>();
        var matches = Regex.Matches(content, @"<ProjectReference\s+Include=""([^""]+)""[^>]*>");
        
        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value;
            projects.Add(new ProjectReference
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            });
        }
        
        return projects;
    }

    private static bool IsIgnoredDirectory(string dirName)
    {
        return dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
               dirName.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
               dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineFileType(string filePath, string[] lines)
    {
        var fileName = Path.GetFileName(filePath);
        
        if (fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
            return "EntryPoint";
        if (fileName.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase))
            return "Controller";
        if (fileName.EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase))
            return "Service";
        if (fileName.EndsWith("Repository.cs", StringComparison.OrdinalIgnoreCase))
            return "Repository";
        if (fileName.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) || 
            fileName.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
            return "Test";
        
        // Check content for interface
        if (lines.Any(line => line.Trim().StartsWith("interface ", StringComparison.OrdinalIgnoreCase)))
            return "Interface";
        
        return "Class";
    }
}