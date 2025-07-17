using DotnetPrompt.Core.Models.RoslynAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Extracts project metadata from MSBuild projects and solutions
/// </summary>
public static class ProjectMetadataExtractor
{
    /// <summary>
    /// Extracts metadata from a Roslyn Project
    /// </summary>
    /// <param name="project">The Roslyn project to extract metadata from</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <returns>Dictionary containing project metadata</returns>
    public static Dictionary<string, object> ExtractProjectMetadata(Project project, ILogger? logger = null)
    {
        var metadata = new Dictionary<string, object>();
        
        try
        {
            // Basic project information
            metadata["ProjectName"] = project.Name ?? string.Empty;
            metadata["Language"] = project.Language ?? string.Empty;
            metadata["FilePath"] = project.FilePath ?? string.Empty;
            metadata["OutputFilePath"] = project.OutputFilePath ?? string.Empty;
            metadata["AssemblyName"] = project.AssemblyName ?? string.Empty;
            
            // Compilation information
            metadata["HasDocuments"] = project.Documents.Any();
            metadata["DocumentCount"] = project.Documents.Count();
            metadata["MetadataReferences"] = project.MetadataReferences.Count;
            metadata["ProjectReferences"] = project.ProjectReferences.Count();
            metadata["AnalyzerReferences"] = project.AnalyzerReferences.Count;
            
            // Additional metadata from project properties
            if (project.CompilationOptions != null)
            {
                metadata["Platform"] = project.CompilationOptions.Platform.ToString();
                metadata["OptimizationLevel"] = project.CompilationOptions.OptimizationLevel.ToString();
                
                // Check for specific compilation options that may be available
                if (project.CompilationOptions is Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions csharpOptions)
                {
                    metadata["AllowUnsafe"] = csharpOptions.AllowUnsafe;
                    metadata["CheckOverflow"] = csharpOptions.CheckOverflow;
                }
                else
                {
                    metadata["AllowUnsafe"] = false;
                    metadata["CheckOverflow"] = false;
                }
                
                metadata["NullableContextOptions"] = project.CompilationOptions.NullableContextOptions.ToString();
            }
            
            // Parse configuration and target framework from project properties
            metadata["TargetFramework"] = ExtractTargetFramework(project);
            metadata["Configuration"] = ExtractConfiguration(project);
            metadata["OutputType"] = ExtractOutputType(project);
            
            logger?.LogDebug("Extracted metadata for project {ProjectName} with {DocumentCount} documents",
                project.Name, project.Documents.Count());
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract complete metadata for project {ProjectName}", project.Name);
            
            // Ensure basic metadata is available even if extraction fails
            metadata["ProjectName"] = project.Name ?? string.Empty;
            metadata["Language"] = project.Language ?? string.Empty;
            metadata["DocumentCount"] = project.Documents.Count();
        }
        
        return metadata;
    }
    
    /// <summary>
    /// Extracts target framework from project
    /// </summary>
    /// <param name="project">The project to analyze</param>
    /// <returns>Target framework string or null if not determinable</returns>
    public static string? ExtractTargetFramework(Project project)
    {
        try
        {
            // Try to extract from compilation options or project properties
            // This is a simplified implementation - full implementation would parse MSBuild properties
            var assemblyName = project.AssemblyName;
            if (!string.IsNullOrEmpty(assemblyName))
            {
                // Extract from assembly metadata or compilation options
                var compilationOptions = project.CompilationOptions;
                if (compilationOptions != null)
                {
                    // For now, return a reasonable default based on available information
                    // In a full implementation, this would parse the actual project file
                    return "net8.0"; // Could be improved to detect actual framework
                }
            }
        }
        catch
        {
            // Ignore errors in target framework extraction
        }
        
        return null;
    }
    
    /// <summary>
    /// Extracts configuration from project
    /// </summary>
    /// <param name="project">The project to analyze</param>
    /// <returns>Configuration string (Debug/Release) or default</returns>
    private static string ExtractConfiguration(Project project)
    {
        try
        {
            // Check compilation options for optimization level
            if (project.CompilationOptions?.OptimizationLevel == OptimizationLevel.Release)
            {
                return "Release";
            }
            
            return "Debug"; // Default to Debug
        }
        catch
        {
            return "Debug";
        }
    }
    
    /// <summary>
    /// Extracts output type from project
    /// </summary>
    /// <param name="project">The project to analyze</param>
    /// <returns>Output type string or default</returns>
    private static string ExtractOutputType(Project project)
    {
        try
        {
            // Try to determine output type based on project characteristics
            var hasMainMethod = project.Documents.Any(d => 
                d.Name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && 
                d.Name.Contains("Program", StringComparison.OrdinalIgnoreCase));
                
            if (hasMainMethod)
            {
                return "Exe"; // Console application
            }
            
            return "Library"; // Default to library
        }
        catch
        {
            return "Library";
        }
    }
    
    /// <summary>
    /// Extracts metadata from a solution
    /// </summary>
    /// <param name="solution">The Roslyn solution to extract metadata from</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <returns>Dictionary containing solution metadata</returns>
    public static Dictionary<string, object> ExtractSolutionMetadata(Solution solution, ILogger? logger = null)
    {
        var metadata = new Dictionary<string, object>();
        
        try
        {
            metadata["SolutionPath"] = solution.FilePath ?? string.Empty;
            metadata["ProjectCount"] = solution.Projects.Count();
            metadata["CSharpProjectCount"] = solution.Projects.Count(p => p.Language == LanguageNames.CSharp);
            metadata["TotalDocumentCount"] = solution.Projects.Sum(p => p.Documents.Count());
            
            // Get project names
            metadata["ProjectNames"] = solution.Projects.Select(p => p.Name).ToList();
            
            logger?.LogDebug("Extracted metadata for solution with {ProjectCount} projects",
                solution.Projects.Count());
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract complete solution metadata");
            
            // Ensure basic metadata is available
            metadata["ProjectCount"] = solution.Projects.Count();
        }
        
        return metadata;
    }
}