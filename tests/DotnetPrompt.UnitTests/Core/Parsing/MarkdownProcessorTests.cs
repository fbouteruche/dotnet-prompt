using DotnetPrompt.Core.Parsing;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Parsing;

public class MarkdownProcessorTests
{
    private readonly MarkdownProcessor _processor;

    public MarkdownProcessorTests()
    {
        _processor = new MarkdownProcessor();
    }

    [Fact]
    public void ProcessMarkdown_BasicMarkdown_ParsesSuccessfully()
    {
        // Arrange
        var markdown = """
            # Test Workflow

            This is a basic markdown content with **bold** and *italic* text.

            ## Section 2

            Some more content here.
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.Should().NotBeNull();
        result.RawMarkdown.Should().Be(markdown);
        result.ParsedDocument.Should().NotBeNull();
        result.ParameterReferences.Should().BeEmpty();
        result.SubWorkflowReferences.Should().BeEmpty();
    }

    [Fact]
    public void ProcessMarkdown_MarkdownWithParameters_ExtractsParameters()
    {
        // Arrange
        var markdown = """
            # Project Analysis

            Analyze the project at {{project_path}} with the following settings:
            - Include tests: {{include_tests}}
            - Output directory: {{output_dir}}
            - Analysis depth: {{analysis.depth}}
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.ParameterReferences.Should().HaveCount(4);
        result.ParameterReferences.Should().Contain("project_path");
        result.ParameterReferences.Should().Contain("include_tests");
        result.ParameterReferences.Should().Contain("output_dir");
        result.ParameterReferences.Should().Contain("analysis.depth");
    }

    [Fact]
    public void ProcessMarkdown_MarkdownWithSubWorkflows_ExtractsSubWorkflows()
    {
        // Arrange
        var markdown = """
            # Main Workflow

            First, perform analysis:

            > Execute: ./analysis/project-analysis.prompt.md
            > Parameters:
            > - project_path: "{{project_path}}"
            > - include_dependencies: true

            Then generate documentation:

            > Execute: ./docs/api-docs.prompt.md
            > Parameters:
            > - source_path: "{{project_path}}"
            > - output_format: "markdown"

            Finally, create a summary report:

            > Execute: ./reports/summary.prompt.md
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.SubWorkflowReferences.Should().HaveCount(3);
        
        // First sub-workflow
        var first = result.SubWorkflowReferences[0];
        first.Path.Should().Be("./analysis/project-analysis.prompt.md");
        first.Parameters.Should().HaveCount(2);
        first.Parameters.Should().ContainKey("project_path");
        first.Parameters.Should().ContainKey("include_dependencies");
        first.Parameters["project_path"].Should().Be("{{project_path}}");
        first.Parameters["include_dependencies"].Should().Be("true");

        // Second sub-workflow
        var second = result.SubWorkflowReferences[1];
        second.Path.Should().Be("./docs/api-docs.prompt.md");
        second.Parameters.Should().HaveCount(2);
        second.Parameters.Should().ContainKey("source_path");
        second.Parameters.Should().ContainKey("output_format");

        // Third sub-workflow (no parameters)
        var third = result.SubWorkflowReferences[2];
        third.Path.Should().Be("./reports/summary.prompt.md");
        third.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void ProcessMarkdown_MarkdownWithToolReferences_ExtractsTools()
    {
        // Arrange
        var markdown = """
            # Tool Usage Workflow

            Use project-analysis to examine the codebase.
            Then run build-test to validate the project.
            Finally, use file-system operations to organize outputs.
            
            Additional git-operations might be needed.
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.ToolReferences.Should().NotBeEmpty();
        result.ToolReferences.Should().Contain("project-analysis");
        result.ToolReferences.Should().Contain("build-test");
        result.ToolReferences.Should().Contain("file-system");
        result.ToolReferences.Should().Contain("git-operations");
    }

    [Fact]
    public void ProcessMarkdown_ComplexMarkdown_ExtractsAllElements()
    {
        // Arrange
        var markdown = """
            # Complex Workflow Example

            This workflow demonstrates parameter usage with {{project_path}} and {{config.environment}}.

            ## Phase 1: Analysis

            > Execute: ./phases/analysis.prompt.md
            > Parameters:
            > - target: "{{project_path}}"
            > - mode: "detailed"

            Use project-analysis tool for comprehensive examination.

            ## Phase 2: Build and Test

            Run build-test operations with the following parameters:
            - Source: {{project_path}}
            - Environment: {{config.environment}}

            > Execute: ./phases/build.prompt.md
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.ParameterReferences.Should().Contain("project_path");
        result.ParameterReferences.Should().Contain("config.environment");
        result.SubWorkflowReferences.Should().HaveCount(2);
        result.SubWorkflowReferences[0].Path.Should().Be("./phases/analysis.prompt.md");
        result.SubWorkflowReferences[1].Path.Should().Be("./phases/build.prompt.md");
        result.ToolReferences.Should().Contain("project-analysis");
        result.ToolReferences.Should().Contain("build-test");
    }

    [Fact]
    public void ProcessMarkdown_EmptyMarkdown_ReturnsEmptyResult()
    {
        // Arrange
        var markdown = "";

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.Should().NotBeNull();
        result.RawMarkdown.Should().Be(markdown);
        result.ParameterReferences.Should().BeEmpty();
        result.SubWorkflowReferences.Should().BeEmpty();
        result.ToolReferences.Should().BeEmpty();
        result.ParsedDocument.Should().NotBeNull();
    }

    [Fact]
    public void ProcessMarkdown_MarkdownWithMalformedParameters_ExtractsValidParametersOnly()
    {
        // Arrange
        var markdown = """
            # Parameter Edge Cases

            Valid parameter: {{valid_param}}
            Malformed parameters: {invalid} {{}} {{123invalid}}
            Another valid one: {{another.valid_param}}
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.ParameterReferences.Should().HaveCount(2);
        result.ParameterReferences.Should().Contain("valid_param");
        result.ParameterReferences.Should().Contain("another.valid_param");
        result.ParameterReferences.Should().NotContain("invalid");
        result.ParameterReferences.Should().NotContain("");
        result.ParameterReferences.Should().NotContain("123invalid");
    }

    [Fact]
    public void ProcessMarkdown_SubWorkflowWithoutParameters_ExtractsPathOnly()
    {
        // Arrange
        var markdown = """
            # Simple Sub-workflow

            > Execute: ./simple/workflow.prompt.md

            No parameters for this one.
            """;

        // Act
        var result = _processor.ProcessMarkdown(markdown);

        // Assert
        result.SubWorkflowReferences.Should().HaveCount(1);
        result.SubWorkflowReferences[0].Path.Should().Be("./simple/workflow.prompt.md");
        result.SubWorkflowReferences[0].Parameters.Should().BeEmpty();
    }
}