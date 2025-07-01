using System.Text.RegularExpressions;
using DotnetPrompt.Core.Models;
using Markdig;
using Markdig.Syntax;

namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Processes markdown content and extracts workflow-specific information
/// </summary>
public class MarkdownProcessor
{
    private static readonly Regex ParameterReferenceRegex = new(@"\{\{([a-zA-Z_][a-zA-Z0-9_.-]*)\}\}", RegexOptions.Compiled);
    private static readonly Regex SubWorkflowReferenceRegex = new(@">\s*Execute:\s*([^\r\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ParameterLineRegex = new(@">\s*Parameters?:\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly MarkdownPipeline _pipeline;

    public MarkdownProcessor()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <summary>
    /// Processes markdown content and extracts workflow information
    /// </summary>
    /// <param name="markdownContent">Raw markdown content</param>
    /// <returns>Processed workflow content</returns>
    public WorkflowContent ProcessMarkdown(string markdownContent)
    {
        var content = new WorkflowContent
        {
            RawMarkdown = markdownContent
        };

        try
        {
            // Parse markdown document
            content.ParsedDocument = Markdown.Parse(markdownContent, _pipeline);

            // Extract parameter references
            content.ParameterReferences = ExtractParameterReferences(markdownContent);

            // Extract sub-workflow references
            content.SubWorkflowReferences = ExtractSubWorkflowReferences(markdownContent);

            // Extract tool references (basic implementation)
            content.ToolReferences = ExtractToolReferences(markdownContent);
        }
        catch (Exception ex)
        {
            throw new DotpromptParseException("Failed to process markdown content", ex);
        }

        return content;
    }

    /// <summary>
    /// Extracts parameter references in the format {{parameter_name}}
    /// </summary>
    private HashSet<string> ExtractParameterReferences(string content)
    {
        var parameters = new HashSet<string>();
        var matches = ParameterReferenceRegex.Matches(content);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                parameters.Add(match.Groups[1].Value);
            }
        }

        return parameters;
    }

    /// <summary>
    /// Extracts sub-workflow references from markdown content
    /// </summary>
    private List<SubWorkflowReference> ExtractSubWorkflowReferences(string content)
    {
        var references = new List<SubWorkflowReference>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var executeMatch = SubWorkflowReferenceRegex.Match(line);

            if (executeMatch.Success)
            {
                var reference = new SubWorkflowReference
                {
                    Path = executeMatch.Groups[1].Value.Trim(),
                    LineNumber = i + 1
                };

                // Look for parameters in subsequent lines
                for (int j = i + 1; j < lines.Length; j++)
                {
                    var paramLine = lines[j];
                    
                    // Check if this is the start of parameters section
                    if (ParameterLineRegex.IsMatch(paramLine))
                    {
                        // Parse parameters from following lines
                        for (int k = j + 1; k < lines.Length; k++)
                        {
                            var nextLine = lines[k].Trim();
                            if (string.IsNullOrEmpty(nextLine) || !nextLine.StartsWith(">"))
                                break;

                            // Parse parameter line: > - key: "value"
                            var paramMatch = Regex.Match(nextLine, @">\s*-\s*([^:]+):\s*[""']?([^""'\r\n]+)[""']?");
                            if (paramMatch.Success)
                            {
                                reference.Parameters[paramMatch.Groups[1].Value.Trim()] = paramMatch.Groups[2].Value.Trim();
                            }
                        }
                        break;
                    }
                    
                    // If we hit a non-quote line that's not empty, stop looking for parameters
                    if (!string.IsNullOrWhiteSpace(paramLine) && !paramLine.TrimStart().StartsWith(">"))
                        break;
                }

                references.Add(reference);
            }
        }

        return references;
    }

    /// <summary>
    /// Extracts tool references from markdown content
    /// Basic implementation that looks for common tool patterns
    /// </summary>
    private HashSet<string> ExtractToolReferences(string content)
    {
        var tools = new HashSet<string>();

        // Look for explicit tool mentions in various patterns
        var toolPatterns = new[]
        {
            @"project[_-]?analysis",
            @"build[_-]?test",
            @"file[_-]?system",
            @"git[_-]?operations",
            @"document[_-]?generation"
        };

        foreach (var pattern in toolPatterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                tools.Add(match.Value.ToLowerInvariant().Replace("_", "-"));
            }
        }

        return tools;
    }
}