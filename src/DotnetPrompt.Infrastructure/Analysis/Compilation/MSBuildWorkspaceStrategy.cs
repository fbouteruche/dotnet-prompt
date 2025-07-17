using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Primary compilation strategy using MSBuildWorkspace for full project system integration
/// </summary>
public class MSBuildWorkspaceStrategy : IRoslynCompilationStrategy
{
    private readonly ILogger<MSBuildWorkspaceStrategy> _logger;
    private readonly MSBuildDiagnosticsHandler _diagnosticsHandler;
    
    public MSBuildWorkspaceStrategy(
        ILogger<MSBuildWorkspaceStrategy> logger,
        MSBuildDiagnosticsHandler diagnosticsHandler)
    {
        _logger = logger;
        _diagnosticsHandler = diagnosticsHandler;
        
        // Critical: Ensure MSBuild is initialized before any workspace operations
        MSBuildSetup.EnsureInitialized();
    }
    
    public CompilationStrategy StrategyType => CompilationStrategy.MSBuild;
    
    public int Priority => 100; // High priority - MSBuild is preferred when available
    
    public string Description => "MSBuild Workspace strategy for full project system integration with comprehensive dependency resolution";
    
    public bool CanHandle(string projectPath, AnalysisOptions options)
    {
        // MSBuild can handle .csproj, .fsproj, .vbproj, and .sln files
        var extension = Path.GetExtension(projectPath).ToLowerInvariant();
        return extension == ".csproj" || extension == ".fsproj" || 
               extension == ".vbproj" || extension == ".sln";
    }
    
    public async Task<CompilationResult> CreateCompilationAsync(
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        MSBuildWorkspace? workspace = null;
        
        try
        {
            _logger.LogInformation("Starting MSBuild compilation for {ProjectPath}", projectPath);
            
            // Create MSBuild workspace with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.MSBuildTimeout));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                // Configure MSBuild properties for better compatibility
                ["DesignTimeBuild"] = "true",
                ["BuildProjectReferences"] = "false", 
                ["_ResolveReferenceDependencies"] = "true",
                ["SolutionDir"] = GetSolutionDirectory(projectPath),
                ["TargetFramework"] = options.TargetFramework ?? "",
                ["Configuration"] = "Debug", // Use Debug configuration for analysis
                ["Platform"] = "AnyCPU"
            });
            
            // Handle solution vs project files
            CompilationResult result;
            if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                result = await HandleSolutionAnalysis(workspace, projectPath, options, combinedCts.Token);
            }
            else
            {
                result = await HandleProjectAnalysis(workspace, projectPath, options, combinedCts.Token);
            }
            
            // Process workspace diagnostics
            _diagnosticsHandler.ProcessWorkspaceDiagnostics(workspace.Diagnostics, projectPath);
            result.CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("MSBuild compilation was cancelled by user for {ProjectPath}", projectPath);
            throw; // Re-throw user cancellation
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MSBuild compilation timed out for {ProjectPath} after {Timeout}ms", 
                projectPath, options.MSBuildTimeout);
            
            return CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"MSBuild compilation timed out after {options.MSBuildTimeout}ms. " +
                "Consider increasing the timeout or using a lighter analysis approach.",
                (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSBuild compilation failed for {ProjectPath}", projectPath);
            
            return CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"MSBuild compilation failed: {ex.Message}",
                (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        finally
        {
            // Proper disposal is critical for MSBuildWorkspace
            workspace?.Dispose();
        }
    }
    
    private async Task<CompilationResult> HandleSolutionAnalysis(
        MSBuildWorkspace workspace,
        string solutionPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening solution {SolutionPath}", solutionPath);
        
        var solution = await workspace.OpenSolutionAsync(solutionPath, progress: null, cancellationToken);
        
        // Validate workspace diagnostics early
        var criticalDiagnostics = workspace.Diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToList();
            
        if (criticalDiagnostics.Any())
        {
            _logger.LogWarning("MSBuild workspace has {Count} critical diagnostics for solution {Path}",
                criticalDiagnostics.Count, solutionPath);
        }
        
        // For solutions, find the most appropriate project to analyze
        var targetProject = SelectTargetProject(solution, options);
        
        if (targetProject == null)
        {
            return CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                "No suitable C# project found in solution for analysis",
                0);
        }
        
        _logger.LogDebug("Selected project {ProjectName} from solution for analysis", targetProject.Name);
        
        var compilation = await targetProject.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"Failed to create compilation from MSBuild project '{targetProject.Name}'",
                0);
        }
        
        return CompilationResultMapper.CreateSuccessResult(
            compilation,
            CompilationStrategy.MSBuild,
            0, // Will be set by caller
            targetProject.AnalyzerConfigDocuments.FirstOrDefault()?.FilePath);
    }
    
    private async Task<CompilationResult> HandleProjectAnalysis(
        MSBuildWorkspace workspace,
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening project {ProjectPath}", projectPath);
        
        var project = await workspace.OpenProjectAsync(projectPath, progress: null, cancellationToken);
        
        // Handle multi-target framework projects
        if (!string.IsNullOrEmpty(options.TargetFramework))
        {
            _logger.LogDebug("Targeting specific framework {Framework} for project {Project}",
                options.TargetFramework, projectPath);
            
            // For multi-target projects, ensure we're using the correct target framework
            // This is a simplified approach - full implementation would require 
            // reloading the project with specific MSBuild properties
        }
        
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            return CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"Failed to create compilation from MSBuild project '{project.Name}'. " +
                "This may indicate missing dependencies or project configuration issues.",
                0);
        }
        
        _logger.LogDebug("Successfully created compilation for project {ProjectName} with {DocumentCount} documents",
            project.Name, project.Documents.Count());
        
        // Extract target framework from project properties
        var targetFramework = ExtractTargetFramework(project);
        
        return CompilationResultMapper.CreateSuccessResult(
            compilation,
            CompilationStrategy.MSBuild,
            0, // Will be set by caller
            targetFramework);
    }
    
    private Project? SelectTargetProject(Solution solution, AnalysisCompilationOptions options)
    {
        var csharpProjects = solution.Projects
            .Where(p => p.Language == LanguageNames.CSharp)
            .ToList();
        
        if (!csharpProjects.Any())
        {
            _logger.LogWarning("No C# projects found in solution");
            return null;
        }
        
        // If target framework is specified, prefer projects that match
        if (!string.IsNullOrEmpty(options.TargetFramework))
        {
            var matchingProjects = csharpProjects
                .Where(p => ProjectMatchesTargetFramework(p, options.TargetFramework))
                .ToList();
                
            if (matchingProjects.Any())
            {
                csharpProjects = matchingProjects;
            }
        }
        
        // Filter out test projects if requested
        if (!options.IncludeTests)
        {
            var nonTestProjects = csharpProjects
                .Where(p => !IsTestProject(p))
                .ToList();
                
            if (nonTestProjects.Any())
            {
                csharpProjects = nonTestProjects;
            }
        }
        
        // Prefer projects with more documents (likely main application projects)
        var targetProject = csharpProjects
            .OrderByDescending(p => p.Documents.Count())
            .ThenBy(p => p.Name) // Deterministic ordering
            .First();
            
        return targetProject;
    }
    
    private static string GetSolutionDirectory(string projectPath)
    {
        var directory = File.Exists(projectPath) ? 
            Path.GetDirectoryName(projectPath)! : 
            projectPath;
            
        // Ensure directory ends with separator for MSBuild
        return directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
    
    private static bool ProjectMatchesTargetFramework(Project project, string targetFramework)
    {
        // Simplified check - would need full implementation to parse project properties
        return project.CompilationOptions?.Platform.ToString()?.Contains(targetFramework, StringComparison.OrdinalIgnoreCase) ?? false;
    }
    
    private static bool IsTestProject(Project project)
    {
        // Check common patterns for test projects
        var projectName = project.Name.ToLowerInvariant();
        return projectName.Contains("test") || 
               projectName.Contains("spec") ||
               project.MetadataReferences.Any(r => 
                   r.Display?.Contains("xunit", StringComparison.OrdinalIgnoreCase) == true ||
                   r.Display?.Contains("nunit", StringComparison.OrdinalIgnoreCase) == true ||
                   r.Display?.Contains("mstest", StringComparison.OrdinalIgnoreCase) == true);
    }
    
    private static string? ExtractTargetFramework(Project project)
    {
        // Try to extract target framework from compilation options or project properties
        // This is a simplified implementation - full implementation would parse MSBuild properties
        var assemblyName = project.AssemblyName;
        if (!string.IsNullOrEmpty(assemblyName))
        {
            // Extract from assembly metadata or compilation options
            var compilationOptions = project.CompilationOptions;
            if (compilationOptions != null)
            {
                // For now, return a reasonable default based on available information
                return "net8.0"; // Could be improved to detect actual framework
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a compilation and provides access to the raw Roslyn compilation for semantic analysis
    /// </summary>
    public async Task<(CompilationResult Result, Microsoft.CodeAnalysis.Compilation? RoslynCompilation)> CreateCompilationWithRoslynAsync(
        string projectPath, 
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        MSBuildWorkspace? workspace = null;
        Microsoft.CodeAnalysis.Compilation? roslynCompilation = null;
        
        try
        {
            _logger.LogInformation("Starting MSBuild compilation for {ProjectPath}", projectPath);
            
            // Create MSBuild workspace with timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.MSBuildTimeout));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                // Configure MSBuild properties for better compatibility
                ["DesignTimeBuild"] = "true",
                ["BuildProjectReferences"] = "false", 
                ["_ResolveReferenceDependencies"] = "true",
                ["SolutionDir"] = GetSolutionDirectory(projectPath),
                ["TargetFramework"] = options.TargetFramework ?? "",
                ["Configuration"] = "Debug", // Use Debug configuration for analysis
                ["Platform"] = "AnyCPU"
            });
            
            // Handle solution vs project files
            CompilationResult result;
            if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                (result, roslynCompilation) = await HandleSolutionAnalysisWithRoslyn(workspace, projectPath, options, combinedCts.Token);
            }
            else
            {
                (result, roslynCompilation) = await HandleProjectAnalysisWithRoslyn(workspace, projectPath, options, combinedCts.Token);
            }
            
            // Process workspace diagnostics
            _diagnosticsHandler.ProcessWorkspaceDiagnostics(workspace.Diagnostics, projectPath);
            result.CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            return (result, roslynCompilation);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("MSBuild compilation was cancelled by user for {ProjectPath}", projectPath);
            throw; // Re-throw user cancellation
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MSBuild compilation timed out for {ProjectPath} after {Timeout}ms", 
                projectPath, options.MSBuildTimeout);
            
            var result = CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"MSBuild compilation timed out after {options.MSBuildTimeout}ms. " +
                "Consider increasing the timeout or using a lighter analysis approach.",
                (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
            
            return (result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSBuild compilation failed for {ProjectPath}", projectPath);
            
            var result = CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"MSBuild compilation failed: {ex.Message}",
                (long)(DateTime.UtcNow - startTime).TotalMilliseconds);
            
            return (result, null);
        }
        finally
        {
            // Proper disposal is critical for MSBuildWorkspace
            workspace?.Dispose();
        }
    }

    private async Task<(CompilationResult, Microsoft.CodeAnalysis.Compilation?)> HandleSolutionAnalysisWithRoslyn(
        MSBuildWorkspace workspace,
        string solutionPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening solution {SolutionPath}", solutionPath);
        
        var solution = await workspace.OpenSolutionAsync(solutionPath, progress: null, cancellationToken);
        
        // Validate workspace diagnostics early
        var criticalDiagnostics = workspace.Diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToList();
            
        if (criticalDiagnostics.Any())
        {
            _logger.LogWarning("MSBuild workspace has {Count} critical diagnostics for solution {Path}",
                criticalDiagnostics.Count, solutionPath);
        }
        
        // For solutions, find the most appropriate project to analyze
        var targetProject = SelectTargetProject(solution, options);
        
        if (targetProject == null)
        {
            var result = CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                "No suitable C# project found in solution for analysis",
                0);
            return (result, null);
        }
        
        _logger.LogDebug("Selected project {ProjectName} from solution for analysis", targetProject.Name);
        
        var compilation = await targetProject.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            var result = CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"Failed to create compilation from MSBuild project '{targetProject.Name}'",
                0);
            return (result, null);
        }
        
        var successResult = CompilationResultMapper.CreateSuccessResult(
            compilation,
            CompilationStrategy.MSBuild,
            0, // Will be set by caller
            targetProject.AnalyzerConfigDocuments.FirstOrDefault()?.FilePath);
        
        return (successResult, compilation);
    }

    private async Task<(CompilationResult, Microsoft.CodeAnalysis.Compilation?)> HandleProjectAnalysisWithRoslyn(
        MSBuildWorkspace workspace,
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening project {ProjectPath}", projectPath);
        
        var project = await workspace.OpenProjectAsync(projectPath, progress: null, cancellationToken);
        
        // Handle multi-target framework projects
        if (!string.IsNullOrEmpty(options.TargetFramework))
        {
            _logger.LogDebug("Targeting specific framework {Framework} for project {Project}",
                options.TargetFramework, projectPath);
            
            // For multi-target projects, ensure we're using the correct target framework
            // This is a simplified approach - full implementation would require 
            // reloading the project with specific MSBuild properties
        }
        
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            var result = CompilationResultMapper.CreateFailureResult(
                CompilationStrategy.MSBuild,
                $"Failed to create compilation from MSBuild project '{project.Name}'. " +
                "This may indicate missing dependencies or project configuration issues.",
                0);
            return (result, null);
        }
        
        _logger.LogDebug("Successfully created compilation for project {ProjectName} with {DocumentCount} documents",
            project.Name, project.Documents.Count());
        
        // Extract target framework from project properties
        var targetFramework = ExtractTargetFramework(project);
        
        var successResult = CompilationResultMapper.CreateSuccessResult(
            compilation,
            CompilationStrategy.MSBuild,
            0, // Will be set by caller
            targetFramework);
        
        return (successResult, compilation);
    }
}