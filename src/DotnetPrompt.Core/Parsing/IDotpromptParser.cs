using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Interface for parsing dotprompt workflow files
/// </summary>
public interface IDotpromptParser
{
    /// <summary>
    /// Parses a dotprompt workflow from a file path
    /// </summary>
    /// <param name="filePath">Path to the .prompt.md file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed workflow model</returns>
    Task<DotpromptWorkflow> ParseFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a dotprompt workflow from content string
    /// </summary>
    /// <param name="content">File content</param>
    /// <param name="filePath">Optional file path for context</param>
    /// <returns>Parsed workflow model</returns>
    DotpromptWorkflow ParseContent(string content, string? filePath = null);

    /// <summary>
    /// Validates a dotprompt workflow
    /// </summary>
    /// <param name="workflow">Workflow to validate</param>
    /// <returns>Validation result</returns>
    DotpromptValidationResult Validate(DotpromptWorkflow workflow);

    /// <summary>
    /// Validates a dotprompt workflow file
    /// </summary>
    /// <param name="filePath">Path to the .prompt.md file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<DotpromptValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);
}