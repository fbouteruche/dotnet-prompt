using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotnetPrompt.Infrastructure.Analysis.Compilation;

/// <summary>
/// Maps between Roslyn compilation artifacts and Core domain models
/// </summary>
public static class CompilationResultMapper
{
    /// <summary>
    /// Creates a successful Core CompilationResult from a Roslyn Compilation
    /// </summary>
    public static CompilationResult CreateSuccessResult(
        Microsoft.CodeAnalysis.Compilation compilation,
        CompilationStrategy strategy,
        long compilationTimeMs,
        string? targetFramework = null,
        bool fallbackUsed = false,
        string? fallbackReason = null)
    {
        var diagnostics = compilation.GetDiagnostics();
        
        return new CompilationResult(strategy, compilation.AssemblyName)
        {
            Success = true,
            StrategyUsed = strategy,
            FallbackUsed = fallbackUsed,
            FallbackReason = fallbackReason,
            CompilationTimeMs = compilationTimeMs,
            TargetFramework = targetFramework,
            Diagnostics = CreateDiagnosticsSummary(diagnostics)
        };
    }

    /// <summary>
    /// Creates a failed Core CompilationResult
    /// </summary>
    public static CompilationResult CreateFailureResult(
        CompilationStrategy strategy,
        string errorMessage,
        long compilationTimeMs,
        bool fallbackUsed = false,
        string? fallbackReason = null,
        IEnumerable<Diagnostic>? diagnostics = null)
    {
        var result = new CompilationResult
        {
            Success = false,
            StrategyUsed = strategy,
            FallbackUsed = fallbackUsed,
            FallbackReason = fallbackReason,
            ErrorMessage = errorMessage,
            CompilationTimeMs = compilationTimeMs
        };

        if (diagnostics != null)
        {
            result.Diagnostics = CreateDiagnosticsSummary(diagnostics);
        }

        return result;
    }

    /// <summary>
    /// Creates a Core CompilationResult from workspace diagnostics
    /// </summary>
    public static CompilationResult CreateFromWorkspaceDiagnostics(
        CompilationStrategy strategy,
        IEnumerable<WorkspaceDiagnostic> workspaceDiagnostics,
        long compilationTimeMs,
        string? errorMessage = null)
    {
        var diagnostics = workspaceDiagnostics.ToList();
        var hasFailures = diagnostics.Any(d => d.Kind == WorkspaceDiagnosticKind.Failure);

        var result = new CompilationResult
        {
            Success = !hasFailures,
            StrategyUsed = strategy,
            CompilationTimeMs = compilationTimeMs,
            ErrorMessage = errorMessage
        };

        // Convert workspace diagnostics to our format
        result.WorkspaceDiagnostics = diagnostics.Select(d => new WorkspaceDiagnosticInfo
        {
            Kind = d.Kind.ToString(),
            Message = d.Message,
            ProjectPath = d.ProjectId?.ToString()
        }).ToList();

        return result;
    }

    /// <summary>
    /// Creates a DiagnosticsSummary from Roslyn diagnostics
    /// </summary>
    private static DiagnosticsSummary CreateDiagnosticsSummary(IEnumerable<Diagnostic> diagnostics)
    {
        var diagnosticsList = diagnostics.ToList();
        var summary = new DiagnosticsSummary();

        foreach (var diagnostic in diagnosticsList)
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    summary.ErrorCount++;
                    if (summary.CriticalErrors.Count < 10)
                    {
                        summary.CriticalErrors.Add(CreateDiagnosticInfo(diagnostic));
                    }
                    break;
                case DiagnosticSeverity.Warning:
                    summary.WarningCount++;
                    if (summary.SignificantWarnings.Count < 10)
                    {
                        summary.SignificantWarnings.Add(CreateDiagnosticInfo(diagnostic));
                    }
                    break;
                case DiagnosticSeverity.Info:
                    summary.InfoCount++;
                    break;
            }
        }

        return summary;
    }

    /// <summary>
    /// Creates a DiagnosticInfo from a Roslyn Diagnostic
    /// </summary>
    private static DiagnosticInfo CreateDiagnosticInfo(Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        var lineSpan = location.GetLineSpan();

        return new DiagnosticInfo
        {
            Id = diagnostic.Id,
            Message = diagnostic.GetMessage(),
            Severity = diagnostic.Severity.ToString(),
            FilePath = location.IsInSource ? lineSpan.Path : null,
            Line = location.IsInSource ? lineSpan.StartLinePosition.Line + 1 : null,
            Column = location.IsInSource ? lineSpan.StartLinePosition.Character + 1 : null
        };
    }
}