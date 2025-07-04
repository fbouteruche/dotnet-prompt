using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.SemanticKernel.Plugins;

/// <summary>
/// Semantic Kernel plugin for .NET project analysis and metadata extraction
/// </summary>
public class ProjectAnalysisPlugin
{
    private readonly ILogger<ProjectAnalysisPlugin> _logger;

    public ProjectAnalysisPlugin(ILogger<ProjectAnalysisPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("analyze_project")]
    [Description("Analyzes a .NET project and returns comprehensive information about its structure, dependencies, and configuration")]
    [return: Description("JSON object containing detailed project analysis results")]
    public async Task<string> AnalyzeProjectAsync(
        [Description("The absolute path to the project file (.csproj/.fsproj/.vbproj)")] string projectPath,
        [Description("Include dependency analysis (default: true)")] bool includeDependencies = true,
        [Description("Include source file analysis (default: false)")] bool includeSourceFiles = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Analyzing .NET project via SK function: {ProjectPath}", projectPath);
            
            // Validate project file
            var validatedPath = ValidateProjectPath(projectPath);
            
            if (!File.Exists(validatedPath))
            {
                throw new FileNotFoundException($"Project file not found: {validatedPath}");
            }

            // Parse project file
            var projectContent = await File.ReadAllTextAsync(validatedPath, cancellationToken);
            var analysis = await AnalyzeProjectContent(validatedPath, projectContent, includeDependencies, includeSourceFiles, cancellationToken);
            
            var result = JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("Successfully analyzed project {ProjectPath} in {Duration}ms", 
                validatedPath, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project {ProjectPath} via SK function", projectPath);
            throw new KernelException($"Project analysis failed: {ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    [KernelFunction("find_project_files")]
    [Description("Finds all .NET project files in a directory and its subdirectories")]
    [return: Description("JSON array of project file paths with basic information")]
    public async Task<string> FindProjectFilesAsync(
        [Description("The directory path to search for project files")] string searchPath,
        [Description("Whether to search subdirectories recursively (default: true)")] bool recursive = true,
        [Description("Maximum depth to search (default: 10)")] int maxDepth = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Finding project files via SK function in: {SearchPath}", searchPath);
            
            var validatedPath = ValidateDirectoryPath(searchPath);
            
            if (!Directory.Exists(validatedPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {validatedPath}");
            }

            var projectFiles = new List<object>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var patterns = new[] { "*.csproj", "*.fsproj", "*.vbproj" };

            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(validatedPath, pattern, searchOption);
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    projectFiles.Add(new
                    {
                        Path = file,
                        Name = fileInfo.Name,
                        Directory = fileInfo.DirectoryName,
                        Size = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime,
                        ProjectType = DetermineProjectType(fileInfo.Extension)
                    });
                }
            }

            var result = JsonSerializer.Serialize(projectFiles, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("Found {ProjectCount} project files in {SearchPath}", projectFiles.Count, validatedPath);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding project files in {SearchPath} via SK function", searchPath);
            throw new KernelException($"Project file search failed: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }

    [KernelFunction("get_project_dependencies")]
    [Description("Extracts and analyzes package dependencies from a .NET project file")]
    [return: Description("JSON object containing dependency information")]
    public async Task<string> GetProjectDependenciesAsync(
        [Description("The absolute path to the project file")] string projectPath,
        [Description("Include transitive dependencies (requires dotnet CLI)")] bool includeTransitive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting project dependencies via SK function: {ProjectPath}", projectPath);
            
            var validatedPath = ValidateProjectPath(projectPath);
            var projectContent = await File.ReadAllTextAsync(validatedPath, cancellationToken);
            
            var dependencies = ExtractDependenciesFromProjectFile(projectContent);
            
            var result = new
            {
                ProjectPath = validatedPath,
                DirectDependencies = dependencies,
                DependencyCount = dependencies.Count,
                AnalysisTime = DateTimeOffset.UtcNow,
                IncludesTransitive = includeTransitive
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("Extracted {DependencyCount} dependencies from {ProjectPath}", 
                dependencies.Count, validatedPath);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project dependencies {ProjectPath} via SK function", projectPath);
            throw new KernelException($"Dependency analysis failed: {ex.Message}", ex);
        }
    }

    private async Task<object> AnalyzeProjectContent(string projectPath, string content, bool includeDependencies, bool includeSourceFiles, CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        
        var analysis = new
        {
            ProjectPath = projectPath,
            ProjectName = projectName,
            ProjectType = DetermineProjectType(Path.GetExtension(projectPath)),
            Directory = projectDirectory,
            TargetFramework = ExtractTargetFramework(content),
            OutputType = ExtractOutputType(content),
            Dependencies = includeDependencies ? ExtractDependenciesFromProjectFile(content) : new List<object>(),
            Properties = ExtractProjectProperties(content),
            SourceFiles = includeSourceFiles ? await GetSourceFiles(projectDirectory, cancellationToken) : new List<string>(),
            AnalysisTime = DateTimeOffset.UtcNow,
            FileSize = new FileInfo(projectPath).Length
        };

        return analysis;
    }

    private string ValidateProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));
        }

        var resolvedPath = Path.IsPathRooted(projectPath) ? projectPath : Path.GetFullPath(projectPath);
        var extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
        
        if (!new[] { ".csproj", ".fsproj", ".vbproj" }.Contains(extension))
        {
            throw new ArgumentException($"Invalid project file extension: {extension}. Expected .csproj, .fsproj, or .vbproj");
        }

        return resolvedPath;
    }

    private string ValidateDirectoryPath(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
        }

        return Path.IsPathRooted(directoryPath) ? directoryPath : Path.GetFullPath(directoryPath);
    }

    private static string DetermineProjectType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".csproj" => "C#",
            ".fsproj" => "F#",
            ".vbproj" => "VB.NET",
            _ => "Unknown"
        };
    }

    private static string? ExtractTargetFramework(string projectContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(projectContent, @"<TargetFramework[^>]*>([^<]+)</TargetFramework>");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractOutputType(string projectContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(projectContent, @"<OutputType[^>]*>([^<]+)</OutputType>");
        return match.Success ? match.Groups[1].Value : "Library"; // Default for .NET projects
    }

    private static List<object> ExtractDependenciesFromProjectFile(string projectContent)
    {
        var dependencies = new List<object>();
        var packageReferences = System.Text.RegularExpressions.Regex.Matches(
            projectContent, 
            @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""[^>]*>");

        foreach (System.Text.RegularExpressions.Match match in packageReferences)
        {
            dependencies.Add(new
            {
                Name = match.Groups[1].Value,
                Version = match.Groups[2].Value,
                Type = "PackageReference"
            });
        }

        return dependencies;
    }

    private static Dictionary<string, string> ExtractProjectProperties(string projectContent)
    {
        var properties = new Dictionary<string, string>();
        
        // Extract common project properties
        var propertyMatches = System.Text.RegularExpressions.Regex.Matches(
            projectContent,
            @"<(TargetFramework|OutputType|AssemblyName|RootNamespace|Nullable|ImplicitUsings)[^>]*>([^<]+)</\1>");

        foreach (System.Text.RegularExpressions.Match match in propertyMatches)
        {
            properties[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return properties;
    }

    private static async Task<List<string>> GetSourceFiles(string projectDirectory, CancellationToken cancellationToken)
    {
        var sourceFiles = new List<string>();
        var extensions = new[] { "*.cs", "*.fs", "*.vb" };

        foreach (var extension in extensions)
        {
            try
            {
                var files = Directory.GetFiles(projectDirectory, extension, SearchOption.AllDirectories);
                sourceFiles.AddRange(files);
            }
            catch
            {
                // Ignore errors when accessing directories
            }
        }

        return sourceFiles;
    }
}