using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Core.Parsing;

public class DotpromptValidatorTests
{
    private readonly DotpromptValidator _validator;

    public DotpromptValidatorTests()
    {
        _validator = new DotpromptValidator();
    }

    [Fact]
    public void Validate_ValidWorkflowWithFrontmatter_ReturnsValid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Config = new DotpromptConfig
            {
                Temperature = 0.7,
                MaxOutputTokens = 4000
            },
            Tools = new List<string> { "project-analysis", "build-test" },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test workflow content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WorkflowWithoutFrontmatter_ReturnsValid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            HasFrontmatter = false,
            Content = new WorkflowContent
            {
                RawMarkdown = "# Simple workflow without frontmatter"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WorkflowWithMissingNameWhenFrontmatterPresent_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("name is required"));
    }

    [Fact]
    public void Validate_WorkflowWithInvalidTemperature_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Config = new DotpromptConfig
            {
                Temperature = 5.0 // Invalid: should be between 0.0 and 2.0
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("Temperature"));
    }

    [Fact]
    public void Validate_WorkflowWithInvalidMaxOutputTokens_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Config = new DotpromptConfig
            {
                MaxOutputTokens = -100 // Invalid: should be greater than 0
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("MaxOutputTokens"));
    }

    [Fact]
    public void Validate_WorkflowWithInvalidTopP_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Config = new DotpromptConfig
            {
                TopP = 1.5 // Invalid: should be between 0.0 and 1.0
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("TopP"));
    }

    [Fact]
    public void Validate_WorkflowWithParameterReferencesButNoSchema_ReturnsWarning()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test with {{parameter_name}}",
                ParameterReferences = new HashSet<string> { "parameter_name" }
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Warnings don't make it invalid
        result.Warnings.Should().Contain(w => w.Message.Contains("no input schema defined"));
    }

    [Fact]
    public void Validate_WorkflowWithUndefinedParameterReference_ReturnsWarning()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Input = new DotpromptInput
            {
                Schema = new Dictionary<string, DotpromptInputSchema>
                {
                    ["defined_param"] = new() { Type = "string" }
                }
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test with {{defined_param}} and {{undefined_param}}",
                ParameterReferences = new HashSet<string> { "defined_param", "undefined_param" }
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Message.Contains("undefined_param"));
        result.Warnings.Should().NotContain(w => w.Field == "defined_param");
    }

    [Fact]
    public void Validate_WorkflowWithUnknownTools_ReturnsWarning()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Tools = new List<string> { "project-analysis", "unknown-tool", "custom-tool" },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Message.Contains("unknown-tool"));
        result.Warnings.Should().Contain(w => w.Message.Contains("custom-tool"));
        result.Warnings.Should().NotContain(w => w.Message.Contains("project-analysis"));
    }

    [Fact]
    public void Validate_WorkflowWithInvalidMcpConfig_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new() { Version = "1.0.0" } // Missing server name
                }
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("server name"));
    }

    [Fact]
    public void Validate_WorkflowWithInvalidSubWorkflowConfig_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Extensions = new DotpromptExtensions
            {
                SubWorkflows = new List<SubWorkflowConfig>
                {
                    new() { Name = "analysis" } // Missing path
                }
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("specify a path"));
    }

    [Fact]
    public void Validate_WorkflowWithEmptyContent_ReturnsInvalid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Content = new WorkflowContent
            {
                RawMarkdown = "" // Empty content
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("markdown content"));
    }

    [Fact]
    public void Validate_ValidExtensionsConfiguration_ReturnsValid()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Name = "test-workflow",
            Model = "github/gpt-4o",
            HasFrontmatter = true,
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new() { Server = "filesystem-mcp", Version = "1.0.0" }
                },
                SubWorkflows = new List<SubWorkflowConfig>
                {
                    new() { Name = "analysis", Path = "./analysis.prompt.md" }
                },
                Resume = new ResumeConfig
                {
                    StorageLocation = "./.dotnet-prompt/resume",
                    RetentionDays = 7,
                    EnableAtomicWrites = true
                }
            },
            Content = new WorkflowContent
            {
                RawMarkdown = "# Test content"
            }
        };

        // Act
        var result = _validator.Validate(workflow);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}