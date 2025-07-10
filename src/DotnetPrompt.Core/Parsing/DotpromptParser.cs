using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DotnetPrompt.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DotnetPrompt.Core.Parsing;

/// <summary>
/// Parser for dotprompt workflow files following the dotprompt format specification
/// </summary>
public class DotpromptParser : IDotpromptParser
{
    private static readonly Regex FrontmatterRegex = new(@"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
    
    private readonly IDeserializer _yamlDeserializer;
    private readonly IDeserializer _extensionDeserializer;
    private readonly MarkdownProcessor _markdownProcessor;
    private readonly DotpromptValidator _validator;

    public DotpromptParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // Separate deserializer for extension fields that preserves exact field names
        _extensionDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        
        _markdownProcessor = new MarkdownProcessor();
        _validator = new DotpromptValidator();
    }

    /// <inheritdoc />
    public async Task<DotpromptWorkflow> ParseFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new DotpromptParseException($"Workflow file not found: {filePath}", filePath);
        }

        if (!filePath.EndsWith(".prompt.md", StringComparison.OrdinalIgnoreCase))
        {
            throw new DotpromptParseException("Workflow file must have .prompt.md extension", filePath);
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var workflow = ParseContent(content, filePath);
            workflow.FilePath = filePath;
            return workflow;
        }
        catch (DotpromptParseException)
        {
            throw; // Re-throw parsing exceptions as-is
        }
        catch (Exception ex)
        {
            throw new DotpromptParseException($"Failed to read workflow file: {ex.Message}", ex, filePath);
        }
    }

    /// <inheritdoc />
    public DotpromptWorkflow ParseContent(string content, string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DotpromptParseException("Workflow content cannot be empty", filePath);
        }

        var workflow = new DotpromptWorkflow
        {
            FilePath = filePath,
            ContentHash = ComputeContentHash(content)
        };

        try
        {
            // Check if content has YAML frontmatter
            var frontmatterMatch = FrontmatterRegex.Match(content);
            
            if (frontmatterMatch.Success)
            {
                // Parse YAML frontmatter
                workflow.HasFrontmatter = true;
                workflow.RawFrontmatter = frontmatterMatch.Groups[1].Value;
                var markdownContent = frontmatterMatch.Groups[2].Value;

                ParseFrontmatter(workflow, workflow.RawFrontmatter);
                
                // Process markdown content
                workflow.Content = _markdownProcessor.ProcessMarkdown(markdownContent);
            }
            else
            {
                // No frontmatter - pure markdown workflow
                workflow.HasFrontmatter = false;
                workflow.Content = _markdownProcessor.ProcessMarkdown(content);
            }

            return workflow;
        }
        catch (DotpromptParseException)
        {
            throw; // Re-throw parsing exceptions as-is
        }
        catch (Exception ex)
        {
            throw new DotpromptParseException($"Failed to parse workflow content: {ex.Message}", ex, filePath);
        }
    }

    /// <inheritdoc />
    public DotpromptValidationResult Validate(DotpromptWorkflow workflow)
    {
        return _validator.Validate(workflow);
    }

    /// <inheritdoc />
    public async Task<DotpromptValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await ParseFileAsync(filePath, cancellationToken);
            return Validate(workflow);
        }
        catch (DotpromptParseException ex)
        {
            return DotpromptValidationResult.Invalid(new DotpromptValidationError
            {
                Message = ex.Message,
                ErrorCode = ex.ErrorCode ?? "PARSE_ERROR",
                LineNumber = ex.LineNumber,
                ColumnNumber = ex.ColumnNumber,
                Severity = ValidationSeverity.Critical
            });
        }
        catch (Exception ex)
        {
            return DotpromptValidationResult.Invalid(new DotpromptValidationError
            {
                Message = $"Unexpected error during validation: {ex.Message}",
                ErrorCode = "UNEXPECTED_ERROR",
                Severity = ValidationSeverity.Critical
            });
        }
    }

    private void ParseFrontmatter(DotpromptWorkflow workflow, string yamlContent)
    {
        try
        {
            // First, deserialize to a dictionary to handle extension fields
            var yamlDict = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            
            if (yamlDict == null)
            {
                return; // Empty frontmatter is valid
            }

            // Extract standard dotprompt fields
            ParseStandardFields(workflow, yamlDict);

            // Extract extension fields
            ParseExtensionFields(workflow, yamlDict);
        }
        catch (YamlException ex)
        {
            throw new DotpromptParseException(
                $"Invalid YAML frontmatter: {ex.Message}", 
                workflow.FilePath, 
                (int?)ex.Start.Line, 
                (int?)ex.Start.Column,
                "YAML_PARSE_ERROR");
        }
        catch (Exception ex)
        {
            throw new DotpromptParseException($"Failed to parse frontmatter: {ex.Message}", ex, workflow.FilePath);
        }
    }

    private void ParseStandardFields(DotpromptWorkflow workflow, Dictionary<string, object> yamlDict)
    {
        // Parse standard dotprompt fields
        if (yamlDict.TryGetValue("name", out var name))
            workflow.Name = name?.ToString();

        if (yamlDict.TryGetValue("model", out var model))
            workflow.Model = model?.ToString();

        if (yamlDict.TryGetValue("tools", out var tools) && tools is IEnumerable<object> toolList)
            workflow.Tools = toolList.Select(t => t?.ToString()).Where(t => !string.IsNullOrEmpty(t)).ToList()!;

        // Parse complex objects by re-serializing and deserializing
        if (yamlDict.TryGetValue("config", out var config))
            workflow.Config = DeserializeObject<DotpromptConfig>(config);

        if (yamlDict.TryGetValue("input", out var input))
            workflow.Input = DeserializeObject<DotpromptInput>(input);

        if (yamlDict.TryGetValue("output", out var output))
            workflow.Output = DeserializeObject<DotpromptOutput>(output);

        if (yamlDict.TryGetValue("metadata", out var metadata))
            workflow.Metadata = DeserializeObject<DotpromptMetadata>(metadata);
    }

    private void ParseExtensionFields(DotpromptWorkflow workflow, Dictionary<string, object> yamlDict)
    {
        // Handle dotnet-prompt extension fields
        var extensionFields = yamlDict.Where(kvp => kvp.Key.StartsWith("dotnet-prompt.")).ToList();

        foreach (var field in extensionFields)
        {
            workflow.ExtensionFields[field.Key] = field.Value;

            // Parse known extension fields
            switch (field.Key)
            {
                case "dotnet-prompt.mcp":
                    workflow.Extensions.Mcp = DeserializeObject<List<McpServerConfig>>(field.Value);
                    break;
                case "dotnet-prompt.sub-workflows":
                    workflow.Extensions.SubWorkflows = DeserializeObject<List<SubWorkflowConfig>>(field.Value);
                    break;
                case "dotnet-prompt.resume":
                    workflow.Extensions.Resume = DeserializeObject<ResumeConfig>(field.Value);
                    break;
                case "dotnet-prompt.error-handling":
                    workflow.Extensions.ErrorHandling = DeserializeObject<ErrorHandlingConfig>(field.Value);
                    break;
            }
        }
    }

    private T? DeserializeObject<T>(object? obj) where T : class
    {
        if (obj == null) return null;

        try
        {
            // Convert the object back to YAML string and then deserialize to target type
            var serializer = new SerializerBuilder()
                .Build();
            
            var yamlString = serializer.Serialize(obj);
            return _extensionDeserializer.Deserialize<T>(yamlString);
        }
        catch (Exception ex)
        {
            throw new DotpromptParseException($"Failed to deserialize {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}