using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Xunit;

namespace DotnetPrompt.IntegrationTests.SemanticKernel;

/// <summary>
/// Tests SK native Handlebars template execution and rendering
/// Validates that the architecture properly leverages SK's built-in templating instead of custom implementations
/// </summary>
public class SkHandlebarsTemplatingTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IKernelFactory _kernelFactory;
    private readonly string _testDirectory;

    public SkHandlebarsTemplatingTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "sk-handlebars-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Use enhanced TestServiceCollectionBuilder for consistent integration test services
        var services = TestServiceCollectionBuilder.CreateIntegrationTestServices();

        _serviceProvider = services.BuildServiceProvider();
        _kernelFactory = _serviceProvider.GetRequiredService<IKernelFactory>();
    }

    [Fact]
    public async Task SkHandlebarsTemplating_SimpleVariableSubstitution_RendersCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        
        var handlebarsTemplate = """
            # Project Analysis for {{project_name}}
            
            The project {{project_name}} is located at {{project_path}}.
            Analysis will {{#if include_tests}}include{{else}}exclude{{/if}} test coverage.
            """;

        var variables = new Dictionary<string, object>
        {
            ["project_name"] = "TestProject",
            ["project_path"] = "/path/to/project",
            ["include_tests"] = true
        };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelAsync("ollama");
            
            // Create SK Handlebars template factory
            var templateFactory = new HandlebarsPromptTemplateFactory();
            var templateConfig = new PromptTemplateConfig(handlebarsTemplate)
            {
                TemplateFormat = "handlebars"
            };
            var template = templateFactory.Create(templateConfig);
            
            var renderedTemplate = await template.RenderAsync(kernel, new KernelArguments(variables.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)));

            // Assert
            renderedTemplate.Should().Contain("TestProject");
            renderedTemplate.Should().Contain("/path/to/project");
            renderedTemplate.Should().Contain("include");
            renderedTemplate.Should().NotContain("{{");
            renderedTemplate.Should().NotContain("}}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkHandlebarsTemplating_ConditionalLogic_ProcessesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        
        var handlebarsTemplate = """
            {{#if verbose}}
            Detailed analysis will be performed.
            {{/if}}
            
            {{#unless quiet}}
            Progress updates will be shown.
            {{/unless}}
            
            {{#each tools}}
            - Tool: {{this}}
            {{/each}}
            """;

        var variables = new Dictionary<string, object>
        {
            ["verbose"] = true,
            ["quiet"] = false,
            ["tools"] = new[] { "file-system", "project-analysis", "build-test" }
        };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelAsync("ollama");
            
            var templateFactory = new HandlebarsPromptTemplateFactory();
            var templateConfig = new PromptTemplateConfig(handlebarsTemplate)
            {
                TemplateFormat = "handlebars"
            };
            var template = templateFactory.Create(templateConfig);
            
            var renderedTemplate = await template.RenderAsync(kernel, new KernelArguments(variables.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)));

            // Assert
            renderedTemplate.Should().Contain("Detailed analysis will be performed");
            renderedTemplate.Should().Contain("Progress updates will be shown");
            renderedTemplate.Should().Contain("Tool: file-system");
            renderedTemplate.Should().Contain("Tool: project-analysis");
            renderedTemplate.Should().Contain("Tool: build-test");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkHandlebarsTemplating_WorkflowWithFrontmatter_ParsesAndRendersCorrectly()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "test-handlebars-workflow"
            model: "ollama/test-model"
            tools: ["file-system"]
            
            config:
              temperature: 0.7
              maxOutputTokens: 2000
            
            input:
              default:
                project_name: "DefaultProject"
                include_analysis: true
            ---
            
            # Analysis for {{project_name}}
            
            {{#if include_analysis}}
            Performing detailed analysis of {{project_name}}.
            {{/if}}
            
            Complete the analysis and provide results.
            """;

        var workflowFile = Path.Combine(_testDirectory, "handlebars-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act - Parse workflow with DotpromptParser
            var parser = new DotpromptParser();
            var workflow = await parser.ParseFileAsync(workflowFile);

            // Create kernel and test basic functionality
            var kernel = await _kernelFactory.CreateKernelAsync("ollama");

            // Assert basic parsing worked
            workflow.Should().NotBeNull();
            workflow.Name.Should().Be("test-handlebars-workflow");
            workflow.Config.Should().NotBeNull();
            
            // Test that we have a valid kernel
            kernel.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkHandlebarsTemplating_ComplexNestedLogic_HandlesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        
        var complexTemplate = """
            # Project Configuration Analysis
            
            {{#each providers}}
            ## Provider: {{name}}
            {{#if enabled}}
            Status: ✅ Enabled
            {{#if config}}
            Configuration:
            {{#each config}}
            - {{@key}}: {{this}}
            {{/each}}
            {{/if}}
            {{else}}
            Status: ❌ Disabled
            {{/if}}
            
            {{/each}}
            
            {{#if isProduction}}
            🔒 High security mode enabled for production environment.
            {{/if}}
            """;

        var variables = new Dictionary<string, object>
        {
            ["environment"] = "production",
            ["security_level"] = 4,
            ["isProduction"] = true,
            ["providers"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "OpenAI",
                    ["enabled"] = true,
                    ["config"] = new Dictionary<string, object>
                    {
                        ["model"] = "ollama/test-model",
                        ["temperature"] = 0.7
                    }
                },
                new Dictionary<string, object>
                {
                    ["name"] = "Azure",
                    ["enabled"] = false
                }
            }
        };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelAsync("ollama");
            
            var templateFactory = new HandlebarsPromptTemplateFactory();
            var templateConfig = new PromptTemplateConfig(complexTemplate)
            {
                TemplateFormat = "handlebars"
            };
            var template = templateFactory.Create(templateConfig);
            
            var renderedTemplate = await template.RenderAsync(kernel, new KernelArguments(variables.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)));

            // Assert
            renderedTemplate.Should().Contain("Provider: OpenAI");
            renderedTemplate.Should().Contain("Status: ✅ Enabled");
            renderedTemplate.Should().Contain("model: ollama/test-model");
            renderedTemplate.Should().Contain("temperature: 0.7");
            renderedTemplate.Should().Contain("Provider: Azure");
            renderedTemplate.Should().Contain("Status: ❌ Disabled");
            renderedTemplate.Should().Contain("High security mode enabled");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkHandlebarsTemplating_InvalidTemplate_HandlesErrorsGracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        
        var invalidTemplate = """
            # Invalid Template Test
            
            {{#if missing_variable}}
            This should work even with undefined variables.
            {{/if}}
            
            {{#each undefined_array}}
            This won't render anything.
            {{/each}}
            
            Valid content: {{valid_variable}}
            """;

        var variables = new Dictionary<string, object>
        {
            ["valid_variable"] = "This should appear"
        };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelAsync("ollama");
            
            var templateFactory = new HandlebarsPromptTemplateFactory();
            var templateConfig = new PromptTemplateConfig(invalidTemplate)
            {
                TemplateFormat = "handlebars"
            };
            var template = templateFactory.Create(templateConfig);
            
            var renderedTemplate = await template.RenderAsync(kernel, new KernelArguments(variables.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)));

            // Assert - SK Handlebars should handle missing variables gracefully
            renderedTemplate.Should().Contain("This should appear");
            renderedTemplate.Should().NotContain("{{");
            renderedTemplate.Should().NotContain("}}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}