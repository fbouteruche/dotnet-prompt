using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using DotnetPrompt.Infrastructure.Analysis.Models;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Handles MSBuild workspace diagnostics and provides fallback recommendations
/// </summary>
public class MSBuildDiagnosticsHandler
{
    private readonly ILogger<MSBuildDiagnosticsHandler> _logger;
    
    public MSBuildDiagnosticsHandler(ILogger<MSBuildDiagnosticsHandler> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Processes workspace diagnostics and logs appropriate messages
    /// </summary>
    /// <param name="diagnostics">Collection of workspace diagnostics</param>
    /// <param name="projectPath">Path to the project being analyzed</param>
    public void ProcessWorkspaceDiagnostics(IEnumerable<WorkspaceDiagnostic> diagnostics, string projectPath)
    {
        var diagnosticsList = diagnostics.ToList();
        if (!diagnosticsList.Any()) 
        {
            _logger.LogDebug("No workspace diagnostics found for {ProjectPath}", projectPath);
            return;
        }
        
        var errors = diagnosticsList.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure).ToList();
        var warnings = diagnosticsList.Where(d => d.Kind == WorkspaceDiagnosticKind.Warning).ToList();
        
        if (errors.Any())
        {
            _logger.LogWarning("MSBuild workspace has {ErrorCount} errors for {ProjectPath}:", 
                errors.Count, projectPath);
            
            foreach (var error in errors.Take(5)) // Log first 5 errors to avoid spam
            {
                _logger.LogWarning("  Error: {Message}", error.Message);
            }
            
            if (errors.Count > 5)
            {
                _logger.LogWarning("  ... and {AdditionalCount} more errors", errors.Count - 5);
            }
        }
        
        if (warnings.Any())
        {
            _logger.LogDebug("MSBuild workspace has {WarningCount} warnings for {ProjectPath}", 
                warnings.Count, projectPath);
                
            // Log first few warnings at debug level
            foreach (var warning in warnings.Take(3))
            {
                _logger.LogDebug("  Warning: {Message}", warning.Message);
            }
        }
        
        // Check for common issues and provide specific guidance
        CheckForCommonIssues(diagnosticsList, projectPath);
    }
    
    /// <summary>
    /// Analyzes diagnostics to determine if fallback to custom compilation is recommended
    /// </summary>
    /// <param name="diagnostics">Collection of workspace diagnostics</param>
    /// <param name="result">Compilation result to analyze</param>
    /// <returns>True if fallback is recommended</returns>
    public bool ShouldFallbackToCustom(IEnumerable<WorkspaceDiagnostic> diagnostics, CompilationResult? result)
    {
        var diagnosticsList = diagnostics.ToList();
        var criticalErrors = diagnosticsList.Count(d => d.Kind == WorkspaceDiagnosticKind.Failure);
        
        // Fallback if too many critical errors
        if (criticalErrors > 10)
        {
            _logger.LogWarning("Too many MSBuild errors ({Count}), recommending fallback to custom compilation", criticalErrors);
            return true;
        }
        
        // Fallback if compilation completely failed
        if (result?.Compilation == null)
        {
            _logger.LogWarning("MSBuild compilation produced no result, recommending fallback to custom compilation");
            return true;
        }
        
        // Fallback if too many compilation errors
        var compilationErrors = result.Compilation.GetDiagnostics()
            .Count(d => d.Severity == DiagnosticSeverity.Error);
            
        if (compilationErrors > 50)
        {
            _logger.LogWarning("Too many compilation errors ({Count}), recommending fallback to custom compilation", compilationErrors);
            return true;
        }
        
        // Check for specific error patterns that indicate MSBuild issues
        var hasSDKErrors = diagnosticsList.Any(d => 
            d.Message.Contains("SDK", StringComparison.OrdinalIgnoreCase) && 
            d.Kind == WorkspaceDiagnosticKind.Failure);
            
        if (hasSDKErrors && criticalErrors > 2)
        {
            _logger.LogWarning("Multiple SDK-related errors detected, recommending fallback to custom compilation");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks for common MSBuild issues and provides specific guidance
    /// </summary>
    /// <param name="diagnostics">Collection of workspace diagnostics</param>
    /// <param name="projectPath">Path to the project being analyzed</param>
    private void CheckForCommonIssues(List<WorkspaceDiagnostic> diagnostics, string projectPath)
    {
        var messages = diagnostics.Select(d => d.Message).ToList();
        
        // Check for SDK not found issues
        if (messages.Any(m => ContainsIgnoreCase(m, "SDK") && ContainsIgnoreCase(m, "not found")))
        {
            _logger.LogError("MSBuild SDK issues detected for {ProjectPath}. " +
                           "Ensure proper .NET SDK is installed and accessible. " +
                           "Try running 'dotnet --list-sdks' to verify SDK installation.", projectPath);
        }
        
        // Check for target framework issues
        if (messages.Any(m => ContainsIgnoreCase(m, "TargetFramework") || ContainsIgnoreCase(m, "target framework")))
        {
            _logger.LogWarning("Target framework issues detected for {ProjectPath}. " +
                             "Project may use unsupported or missing target framework. " +
                             "Consider using a supported .NET version or installing the required SDK.", projectPath);
        }
        
        // Check for package reference issues
        if (messages.Any(m => ContainsIgnoreCase(m, "PackageReference") || ContainsIgnoreCase(m, "package")))
        {
            _logger.LogWarning("Package reference issues detected for {ProjectPath}. " +
                             "Some NuGet packages may be missing or incompatible. " +
                             "Try running 'dotnet restore' to resolve package dependencies.", projectPath);
        }
        
        // Check for MSBuild version issues
        if (messages.Any(m => ContainsIgnoreCase(m, "MSBuild") && ContainsIgnoreCase(m, "version")))
        {
            _logger.LogWarning("MSBuild version compatibility issues detected for {ProjectPath}. " +
                             "The project may require a different version of MSBuild tools.", projectPath);
        }
        
        // Check for missing files or directories
        if (messages.Any(m => ContainsIgnoreCase(m, "not found") || ContainsIgnoreCase(m, "does not exist")))
        {
            _logger.LogWarning("Missing files or directories detected for {ProjectPath}. " +
                             "Ensure all project dependencies and referenced files are available.", projectPath);
        }
        
        // Check for permission issues
        if (messages.Any(m => ContainsIgnoreCase(m, "access") && ContainsIgnoreCase(m, "denied")))
        {
            _logger.LogError("File access permission issues detected for {ProjectPath}. " +
                           "Ensure the process has sufficient permissions to access project files and directories.", projectPath);
        }
    }
    
    /// <summary>
    /// Generates user-friendly error summary for MSBuild issues
    /// </summary>
    /// <param name="diagnostics">Collection of workspace diagnostics</param>
    /// <returns>Human-readable error summary</returns>
    public string GenerateErrorSummary(IEnumerable<WorkspaceDiagnostic> diagnostics)
    {
        var diagnosticsList = diagnostics.ToList();
        if (!diagnosticsList.Any())
            return "No MSBuild issues detected.";
        
        var errors = diagnosticsList.Count(d => d.Kind == WorkspaceDiagnosticKind.Failure);
        var warnings = diagnosticsList.Count(d => d.Kind == WorkspaceDiagnosticKind.Warning);
        
        var summary = $"MSBuild analysis completed with {errors} errors and {warnings} warnings.";
        
        if (errors > 0)
        {
            summary += " Critical issues may prevent full semantic analysis.";
            
            if (ShouldFallbackToCustom(diagnosticsList, null))
            {
                summary += " Consider using custom compilation fallback for better results.";
            }
        }
        
        return summary;
    }
    
    /// <summary>
    /// Case-insensitive string contains check
    /// </summary>
    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}