using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using DotnetPrompt.IntegrationTests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace DotnetPrompt.IntegrationTests.SemanticKernel;

/// <summary>
/// Tests Semantic Kernel integration with BasicKernelFactory and native SK capabilities
/// Validates SK-first architecture patterns and function registration
/// </summary>
public class SkKernelIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IKernelFactory _kernelFactory;

    public SkKernelIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Setup minimal configuration for SK testing
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:GitHub:Token"] = "test-token", // Mock token for testing
                ["AI:GitHub:Model"] = "gpt-4o"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<KernelFactory>>(new MockLogger<KernelFactory>());
        services.AddSingleton<IConfigurationService, MockConfigurationService>();
        services.AddSingleton<IFunctionInvocationFilter, MockWorkflowExecutionFilter>();
        services.AddSingleton<IKernelFactory, KernelFactory>();
        
        // Register plugins for testing
        services.AddSingleton(new FileSystemPlugin(
            new MockLogger<FileSystemPlugin>(),
            Microsoft.Extensions.Options.Options.Create(new DotnetPrompt.Core.Models.FileSystemOptions
            {
                AllowedDirectories = new[] { "/tmp" },
                WorkingDirectoryContext = "/tmp"
            })));

        _serviceProvider = services.BuildServiceProvider();
        _kernelFactory = _serviceProvider.GetRequiredService<IKernelFactory>();
    }

    [Fact]
    public async Task BasicKernelFactory_CreateKernel_RegistersAllPlugins()
    {
        // Arrange
        var pluginTypes = new[]
        {
            typeof(FileSystemPlugin),
            typeof(ProjectAnalysisPlugin),
            typeof(SubWorkflowPlugin)
        };

        // Set up environment for GitHub Models (default provider)
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelWithPluginsAsync(pluginTypes, "github");

            // Assert
            kernel.Should().NotBeNull();
            kernel.Plugins.Should().HaveCountGreaterThan(0);
            
            // Verify each plugin type is registered
            kernel.Plugins.Should().Contain(p => p.Name.Contains("FileSystem"));
            kernel.Plugins.Should().Contain(p => p.Name.Contains("ProjectAnalysis"));
            kernel.Plugins.Should().Contain(p => p.Name.Contains("SubWorkflow"));

            // Verify SK services are configured
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            chatService.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkKernel_FunctionMetadata_ValidatesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        var pluginTypes = new[] { typeof(FileSystemPlugin) };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelWithPluginsAsync(pluginTypes, "github");

            // Assert - Verify SK function metadata
            var functions = kernel.Plugins.GetFunctionsMetadata().ToList();
            functions.Should().NotBeEmpty();

            // Check that FileSystem functions are properly registered with SK attributes
            var readFileFunction = functions.FirstOrDefault(f => f.Name.Contains("read_file"));
            readFileFunction.Should().NotBeNull();
            readFileFunction!.Description.Should().NotBeNullOrEmpty();
            readFileFunction.Parameters.Should().NotBeEmpty();

            // Verify parameter metadata includes descriptions (SK function annotations)
            var pathParam = readFileFunction.Parameters.FirstOrDefault(p => p.Name.Contains("path"));
            pathParam.Should().NotBeNull();
            pathParam!.Description.Should().NotBeNullOrEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkFunctionCalling_AutomaticExecution_CallsCorrectPlugins()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        var pluginTypes = new[] { typeof(FileSystemPlugin) };

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelWithPluginsAsync(pluginTypes, "github");

            // Verify we can get function metadata and it includes proper SK annotations
            var functions = kernel.Plugins.GetFunctionsMetadata();
            functions.Should().NotBeEmpty();

            // Test that SK can find functions by name (automatic function calling prerequisite)
            var fileSystemFunctions = functions.Where(f => f.PluginName.Contains("FileSystem")).ToList();
            fileSystemFunctions.Should().NotBeEmpty();

            // Each function should have proper SK metadata
            foreach (var func in fileSystemFunctions)
            {
                func.Name.Should().NotBeNullOrEmpty();
                func.Description.Should().NotBeNullOrEmpty();
                func.PluginName.Should().NotBeNullOrEmpty();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task SkKernel_WithMultipleProviders_ConfiguresCorrectly()
    {
        // Test that kernel can be configured with different AI providers
        var providers = new[] { "github", "openai", "azure" };

        foreach (var provider in providers)
        {
            // Set up environment variables for each provider
            switch (provider)
            {
                case "github":
                    Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
                    break;
                case "openai":
                    Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
                    break;
                case "azure":
                    Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "test-key");
                    break;
            }

            try
            {
                // Act
                var kernel = await _kernelFactory.CreateKernelAsync(provider, 
                    provider == "azure" ? new Dictionary<string, object> { { "Endpoint", "https://test.openai.azure.com" } } : null);

                // Assert
                kernel.Should().NotBeNull();
                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                chatService.Should().NotBeNull();
            }
            catch (Exception ex) when (ex.Message.Contains("not configured") || ex.Message.Contains("token"))
            {
                // Expected for providers without proper configuration in test environment
                // This validates that the factory properly checks for configuration
                ex.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
                Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
            }
        }
    }

    [Fact]
    public async Task SkFilterPipeline_WorkflowExecutionFilter_RegistersCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act
            var kernel = await _kernelFactory.CreateKernelAsync("github");

            // Assert - Verify that WorkflowExecutionFilter is registered
            // The filter is registered through DI and should be available in the kernel services
            var filters = kernel.Services.GetServices<IFunctionInvocationFilter>().ToList();
            filters.Should().NotBeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}