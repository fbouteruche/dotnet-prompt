using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Parsing;

public class DotpromptParserTests : IDisposable
{
    private readonly DotpromptParser _parser;
    private readonly string _tempDirectory;

    public DotpromptParserTests()
    {
        _parser = new DotpromptParser();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    #region Content Parsing Tests

    [Fact]
    public void ParseContent_ValidWorkflowWithFrontmatter_ParsesSuccessfully()
    {
        // Arrange
        var content = """
            ---
            name: "test-workflow"
            model: "github/gpt-4o"
            tools:
              - "project-analysis"
              - "build-test"
            config:
              temperature: 0.7
              maxOutputTokens: 4000
            input:
              default:
                include_tests: true
              schema:
                project_path:
                  type: string
                  description: "Path to project file"
            ---

            # Test Workflow

            Analyze the project at {{project_path}} and generate documentation.
            Include tests: {{include_tests}}
            """;

        // Act
        var workflow = _parser.ParseContent(content);

        // Assert
        workflow.Should().NotBeNull();
        workflow.HasFrontmatter.Should().BeTrue();
        workflow.Name.Should().Be("test-workflow");
        workflow.Model.Should().Be("github/gpt-4o");
        workflow.Tools.Should().ContainInOrder("project-analysis", "build-test");
        workflow.Config.Should().NotBeNull();
        workflow.Config!.Temperature.Should().Be(0.7);
        workflow.Config.MaxOutputTokens.Should().Be(4000);
        workflow.Input.Should().NotBeNull();
        workflow.Input!.Default.Should().ContainKey("include_tests");
        workflow.Input.Schema.Should().ContainKey("project_path");
        workflow.Content.ParameterReferences.Should().Contain("project_path");
        workflow.Content.ParameterReferences.Should().Contain("include_tests");
        workflow.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ParseContent_WorkflowWithoutFrontmatter_ParsesAsMarkdownOnly()
    {
        // Arrange
        var content = """
            # Simple Workflow

            This is a simple workflow without any frontmatter.
            It should still be parsed successfully.
            """;

        // Act
        var workflow = _parser.ParseContent(content);

        // Assert
        workflow.Should().NotBeNull();
        workflow.HasFrontmatter.Should().BeFalse();
        workflow.Name.Should().BeNull();
        workflow.Model.Should().BeNull();
        workflow.Content.RawMarkdown.Should().Be(content);
        workflow.Content.ParsedDocument.Should().NotBeNull();
    }

    [Fact]
    public void ParseContent_WorkflowWithExtensionFields_ParsesExtensions()
    {
        // Arrange
        var content = """
            ---
            name: "advanced-workflow"
            model: "github/gpt-4o"
            dotnet-prompt.mcp:
              - server: "filesystem-mcp"
                version: "1.0.0"
                config:
                  root_path: "./project"
            dotnet-prompt.progress:
              enabled: true
              checkpoint_frequency: "after_each_tool"
            ---

            # Advanced Workflow
            
            This workflow uses extension fields.
            """;

        // Act
        var workflow = _parser.ParseContent(content);

        // Assert
        workflow.Should().NotBeNull();
        workflow.Extensions.Mcp.Should().NotBeNull();
        workflow.Extensions.Mcp.Should().HaveCount(1);
        workflow.Extensions.Mcp![0].Server.Should().Be("filesystem-mcp");
        workflow.Extensions.Mcp[0].Version.Should().Be("1.0.0");
        // Resume configuration tests would go here
        workflow.ExtensionFields.Should().ContainKey("dotnet-prompt.mcp");
        // Note: Progress extension field changed to resume extension field
    }

    [Fact]
    public void ParseContent_EmptyContent_ThrowsParseException()
    {
        // Arrange
        var content = "";

        // Act & Assert
        var exception = Assert.Throws<DotpromptParseException>(() => _parser.ParseContent(content));
        exception.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public void ParseContent_InvalidYamlFrontmatter_ThrowsParseException()
    {
        // Arrange
        var content = """
            ---
            name: "test
            invalid yaml here
            ---

            # Test Workflow
            """;

        // Act & Assert
        var exception = Assert.Throws<DotpromptParseException>(() => _parser.ParseContent(content));
        exception.Message.Should().Contain("Invalid YAML frontmatter");
        exception.ErrorCode.Should().Be("YAML_PARSE_ERROR");
    }

    #endregion

    #region File Parsing Tests

    [Fact]
    public async Task ParseFileAsync_ValidWorkflowFile_ParsesSuccessfully()
    {
        // Arrange
        var content = """
            ---
            name: "file-test-workflow"
            model: "github/gpt-4o"
            ---

            # File Test Workflow

            This workflow is loaded from a file.
            """;

        var filePath = Path.Combine(_tempDirectory, "test.prompt.md");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var workflow = await _parser.ParseFileAsync(filePath);

        // Assert
        workflow.Should().NotBeNull();
        workflow.Name.Should().Be("file-test-workflow");
        workflow.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task ParseFileAsync_NonExistentFile_ThrowsParseException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexistent.prompt.md");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DotpromptParseException>(() => _parser.ParseFileAsync(filePath));
        exception.Message.Should().Contain("not found");
        exception.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task ParseFileAsync_InvalidExtension_ThrowsParseException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "invalid.txt");
        await File.WriteAllTextAsync(filePath, "test content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DotpromptParseException>(() => _parser.ParseFileAsync(filePath));
        exception.Message.Should().Contain(".prompt.md extension");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_ValidWorkflow_ReturnsValid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Content = new WorkflowContent { RawMarkdown = "# Test content" }
        };

        // Act
        var result = _parser.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WorkflowWithInvalidConfig_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Config = new DotpromptConfig { Temperature = 5.0 }, // Invalid temperature
            Content = new WorkflowContent { RawMarkdown = "# Test content" }
        };

        // Act
        var result = _parser.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Message.Contains("Temperature"));
    }

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsValid()
    {
        // Arrange
        var content = """
            ---
            name: "validation-test"
            model: "github/gpt-4o"
            ---

            # Validation Test

            This is a valid workflow.
            """;

        var filePath = Path.Combine(_tempDirectory, "validation.prompt.md");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ValidateFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidFile_ReturnsInvalid()
    {
        // Arrange
        var content = """
            ---
            name: "invalid-workflow"
            config:
              temperature: 10.0
            ---

            # Invalid Workflow
            """;

        var filePath = Path.Combine(_tempDirectory, "invalid.prompt.md");
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _parser.ValidateFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region Parameter and Content Analysis Tests

    [Fact]
    public void ParseContent_WorkflowWithParameters_ExtractsParameterReferences()
    {
        // Arrange
        var content = """
            ---
            name: "parameter-test"
            model: "github/gpt-4o"
            input:
              schema:
                project_path:
                  type: string
                output_dir:
                  type: string
            ---

            # Parameter Test

            Analyze project at {{project_path}} and save to {{output_dir}}.
            Also use {{undeclared_param}} for testing warnings.
            """;

        // Act
        var workflow = _parser.ParseContent(content);

        // Assert
        workflow.Content.ParameterReferences.Should().Contain("project_path");
        workflow.Content.ParameterReferences.Should().Contain("output_dir");
        workflow.Content.ParameterReferences.Should().Contain("undeclared_param");
    }

    [Fact]
    public void ParseContent_WorkflowWithSubWorkflowReferences_ExtractsReferences()
    {
        // Arrange
        var content = """
            # Sub-workflow Test

            First, run detailed analysis:

            > Execute: ./analysis/detailed.prompt.md
            > Parameters:
            > - project_path: "{{project_path}}"
            > - depth: "comprehensive"

            Then generate docs:

            > Execute: ./docs/generate.prompt.md
            """;

        // Act
        var workflow = _parser.ParseContent(content);

        // Assert
        workflow.Content.SubWorkflowReferences.Should().HaveCount(2);
        workflow.Content.SubWorkflowReferences[0].Path.Should().Be("./analysis/detailed.prompt.md");
        workflow.Content.SubWorkflowReferences[0].Parameters.Should().ContainKey("project_path");
        workflow.Content.SubWorkflowReferences[0].Parameters.Should().ContainKey("depth");
        workflow.Content.SubWorkflowReferences[1].Path.Should().Be("./docs/generate.prompt.md");
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}