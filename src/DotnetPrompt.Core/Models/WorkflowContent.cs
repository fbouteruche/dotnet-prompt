using Markdig.Syntax;

namespace DotnetPrompt.Core.Models;

/// <summary>
/// Represents the markdown content of a workflow file
/// </summary>
public class WorkflowContent
{
    /// <summary>
    /// Raw markdown content as string
    /// </summary>
    public string RawMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Parsed markdown document from Markdig
    /// </summary>
    public MarkdownDocument? ParsedDocument { get; set; }

    /// <summary>
    /// Extracted parameter references from the content (e.g., {{parameter_name}})
    /// </summary>
    public HashSet<string> ParameterReferences { get; set; } = new();

    /// <summary>
    /// Sub-workflow references found in the content
    /// </summary>
    public List<SubWorkflowReference> SubWorkflowReferences { get; set; } = new();

    /// <summary>
    /// Tool calls explicitly referenced in the content
    /// </summary>
    public HashSet<string> ToolReferences { get; set; } = new();
}

/// <summary>
/// Represents a sub-workflow reference found in markdown content
/// </summary>
public class SubWorkflowReference
{
    /// <summary>
    /// Path to the sub-workflow file
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Parameters to pass to the sub-workflow
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Line number where the reference was found
    /// </summary>
    public int LineNumber { get; set; }
}