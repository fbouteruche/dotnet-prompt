using DotnetPrompt.Application.Execution;
using DotnetPrompt.Core.Models;

namespace DotnetPrompt.UnitTests.Application.Execution;

public class VariableResolverTests
{
    private readonly VariableResolver _variableResolver;

    public VariableResolverTests()
    {
        _variableResolver = new VariableResolver();
    }

    [Fact]
    public void ResolveVariables_WithSimpleVariable_ReturnsResolvedString()
    {
        // Arrange
        var template = "Hello {{name}}!";
        var context = new WorkflowExecutionContext();
        context.SetVariable("name", "World");

        // Act
        var result = _variableResolver.ResolveVariables(template, context);

        // Assert
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void ResolveVariables_WithMultipleVariables_ReturnsResolvedString()
    {
        // Arrange
        var template = "{{greeting}} {{name}}, you have {{count}} messages.";
        var context = new WorkflowExecutionContext();
        context.SetVariable("greeting", "Hello");
        context.SetVariable("name", "John");
        context.SetVariable("count", "5");

        // Act
        var result = _variableResolver.ResolveVariables(template, context);

        // Assert
        Assert.Equal("Hello John, you have 5 messages.", result);
    }

    [Fact]
    public void ResolveVariables_WithMissingVariable_LeavesPlaceholder()
    {
        // Arrange
        var template = "Hello {{missing_variable}}!";
        var context = new WorkflowExecutionContext();

        // Act
        var result = _variableResolver.ResolveVariables(template, context);

        // Assert
        Assert.Equal("Hello {{missing_variable}}!", result);
    }

    [Fact]
    public void ResolveVariables_WithEmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";
        var context = new WorkflowExecutionContext();

        // Act
        var result = _variableResolver.ResolveVariables(template, context);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ValidateTemplate_WithAllVariablesPresent_ReturnsValid()
    {
        // Arrange
        var template = "Hello {{name}}, you are {{age}} years old.";
        var context = new WorkflowExecutionContext();
        context.SetVariable("name", "Alice");
        context.SetVariable("age", "30");

        // Act
        var result = _variableResolver.ValidateTemplate(template, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.MissingVariables);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void ValidateTemplate_WithMissingVariables_ReturnsInvalid()
    {
        // Arrange
        var template = "Hello {{name}}, you are {{age}} years old.";
        var context = new WorkflowExecutionContext();
        context.SetVariable("name", "Alice");
        // age is missing

        // Act
        var result = _variableResolver.ValidateTemplate(template, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.MissingVariables);
        Assert.Contains("age", result.MissingVariables);
        Assert.NotNull(result.Errors);
        Assert.Contains("Missing variables: age", result.Errors[0]);
    }

    [Fact]
    public void ExtractVariableReferences_WithVariables_ReturnsCorrectSet()
    {
        // Arrange
        var template = "{{var1}} and {{var2}} and {{var1}} again";

        // Act
        var result = _variableResolver.ExtractVariableReferences(template);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("var1", result);
        Assert.Contains("var2", result);
    }

    [Fact]
    public void ExtractVariableReferences_WithNoVariables_ReturnsEmptySet()
    {
        // Arrange
        var template = "No variables here";

        // Act
        var result = _variableResolver.ExtractVariableReferences(template);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractVariableReferences_WithEmptyVariableName_IgnoresEmpty()
    {
        // Arrange
        var template = "{{valid}} and {{}} and {{another}}";

        // Act
        var result = _variableResolver.ExtractVariableReferences(template);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("valid", result);
        Assert.Contains("another", result);
    }
}