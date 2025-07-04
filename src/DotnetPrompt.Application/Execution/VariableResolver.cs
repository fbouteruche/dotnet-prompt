using System.Text.RegularExpressions;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;

namespace DotnetPrompt.Application.Execution;

/// <summary>
/// Implementation of variable resolution and template processing
/// </summary>
public class VariableResolver : IVariableResolver
{
    private static readonly Regex VariablePattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves variables in a template string
    /// </summary>
    public string ResolveVariables(string template, WorkflowExecutionContext context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return VariablePattern.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value.Trim();
            
            if (context.Variables.TryGetValue(variableName, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }

            // If variable is not found, leave the placeholder as-is
            // This allows for better error reporting
            return match.Value;
        });
    }

    /// <summary>
    /// Validates that all variables in a template can be resolved
    /// </summary>
    public VariableValidationResult ValidateTemplate(string template, WorkflowExecutionContext context)
    {
        if (string.IsNullOrEmpty(template))
            return new VariableValidationResult(true);

        var missingVariables = new HashSet<string>();
        var errors = new List<string>();

        var matches = VariablePattern.Matches(template);
        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value.Trim();
            
            if (string.IsNullOrEmpty(variableName))
            {
                errors.Add($"Empty variable name in template: {match.Value}");
                continue;
            }

            if (!context.Variables.ContainsKey(variableName))
            {
                missingVariables.Add(variableName);
            }
        }

        if (missingVariables.Count > 0)
        {
            errors.Add($"Missing variables: {string.Join(", ", missingVariables)}");
        }

        return new VariableValidationResult(
            IsValid: errors.Count == 0,
            MissingVariables: missingVariables.Count > 0 ? missingVariables : null,
            Errors: errors.Count > 0 ? errors.ToArray() : null
        );
    }

    /// <summary>
    /// Extracts variable references from a template
    /// </summary>
    public HashSet<string> ExtractVariableReferences(string template)
    {
        var variables = new HashSet<string>();
        
        if (string.IsNullOrEmpty(template))
            return variables;

        var matches = VariablePattern.Matches(template);
        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(variableName))
            {
                variables.Add(variableName);
            }
        }

        return variables;
    }
}