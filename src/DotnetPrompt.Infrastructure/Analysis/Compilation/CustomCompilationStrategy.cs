using DotnetPrompt.Infrastructure.Analysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Custom compilation strategy for lightweight analysis scenarios
/// </summary>
public class CustomCompilationStrategy : ICompilationStrategy
{
    private readonly ILogger<CustomCompilationStrategy> _logger;

    public CustomCompilationStrategy(ILogger<CustomCompilationStrategy> logger)
    {
        _logger = logger;
    }

    public CompilationStrategy StrategyType => CompilationStrategy.Custom;

    public bool CanHandle(string projectPath, AnalysisOptions options)
    {
        // Custom strategy can handle any C# project as fallback
        return Path.GetExtension(projectPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
               Directory.Exists(projectPath);
    }

    public async Task<CompilationResult> CreateCompilationAsync(
        string projectPath,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting custom compilation for {ProjectPath}", projectPath);
            
            // Determine if this is a project file or directory
            var sourceDirectory = File.Exists(projectPath) ? Path.GetDirectoryName(projectPath)! : projectPath;
            var projectFile = File.Exists(projectPath) ? projectPath : FindProjectFile(sourceDirectory);
            
            // Load syntax trees
            var syntaxTrees = await LoadSyntaxTreesAsync(sourceDirectory, options, cancellationToken);
            
            if (!syntaxTrees.Any())
            {
                return new CompilationResult
                {
                    Success = false,
                    StrategyUsed = CompilationStrategy.Custom,
                    ErrorMessage = "No C# source files found for compilation",
                    CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
            
            // Get basic references
            var references = GetBasicReferences(projectFile);
            
            // Create compilation
            var assemblyName = projectFile != null 
                ? Path.GetFileNameWithoutExtension(projectFile)
                : Path.GetFileName(sourceDirectory.TrimEnd(Path.DirectorySeparatorChar));
                
            var compilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: syntaxTrees,
                references: references,
                options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
            var result = new CompilationResult(compilation, CompilationStrategy.Custom)
            {
                CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Diagnostics = compilation.GetDiagnostics()
            };
            
            _logger.LogInformation("Custom compilation completed for {ProjectPath} in {Duration}ms", 
                projectPath, result.CompilationTimeMs);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom compilation failed for {ProjectPath}", projectPath);
            
            return new CompilationResult
            {
                Success = false,
                StrategyUsed = CompilationStrategy.Custom,
                ErrorMessage = ex.Message,
                CompilationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private string? FindProjectFile(string directory)
    {
        if (!Directory.Exists(directory))
            return null;
            
        var projectFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        return projectFiles.FirstOrDefault();
    }

    private async Task<List<SyntaxTree>> LoadSyntaxTreesAsync(
        string sourceDirectory,
        AnalysisCompilationOptions options,
        CancellationToken cancellationToken)
    {
        var syntaxTrees = new List<SyntaxTree>();
        
        if (!Directory.Exists(sourceDirectory))
            return syntaxTrees;
        
        var searchOption = SearchOption.AllDirectories;
        var csFiles = Directory.GetFiles(sourceDirectory, "*.cs", searchOption);
        
        foreach (var file in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Skip generated files if requested
            if (options.ExcludeGenerated && IsGeneratedFile(file))
                continue;
                
            // Skip test files if not requested
            if (!options.IncludeTests && IsTestFile(file))
                continue;
            
            try
            {
                var source = await File.ReadAllTextAsync(file, cancellationToken);
                var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file, cancellationToken: cancellationToken);
                syntaxTrees.Add(syntaxTree);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse source file {File}", file);
            }
        }
        
        return syntaxTrees;
    }

    private static bool IsGeneratedFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        // Common patterns for generated files
        return fileName.Contains(".g.cs") ||
               fileName.Contains(".designer.cs") ||
               fileName.Contains(".Generated.cs") ||
               fileName.EndsWith(".g.i.cs") ||
               filePath.Contains("\\obj\\") ||
               filePath.Contains("/obj/");
    }

    private static bool IsTestFile(string filePath)
    {
        return filePath.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase) ||
               filePath.Contains("/Tests/", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<MetadataReference> GetBasicReferences(string? projectFile)
    {
        var references = new List<MetadataReference>();
        
        // Add basic .NET references
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Linq.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Console.dll")));
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        
        // Add basic framework references
        try
        {
            var frameworkPath = Path.GetDirectoryName(typeof(System.ComponentModel.Component).Assembly.Location);
            if (frameworkPath != null)
            {
                var systemDll = Path.Combine(frameworkPath, "System.dll");
                if (File.Exists(systemDll))
                {
                    references.Add(MetadataReference.CreateFromFile(systemDll));
                }
            }
        }
        catch
        {
            // Ignore errors adding framework references
        }
        
        return references;
    }
}