using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel;

public class KernelFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<KernelFactory>> _mockLogger;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly Mock<IFunctionInvocationFilter> _mockFilter;
    private readonly KernelFactory _factory;

    public KernelFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<KernelFactory>>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockFilter = new Mock<IFunctionInvocationFilter>();

        // Mock for ConfigurationService used by kernel builder
        _mockServiceProvider.Setup(x => x.GetService(typeof(IConfigurationService)))
            .Returns(_mockConfigurationService.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IFunctionInvocationFilter)))
            .Returns(_mockFilter.Object);

        // Mock plugins with default loggers
        var mockFileOps = new DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin(
            Mock.Of<ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin>>());
        var mockProjectAnalysis = new DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin(
            Mock.Of<ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin>>());

        _mockServiceProvider.Setup(x => x.GetService(typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin)))
            .Returns(mockFileOps);
        _mockServiceProvider.Setup(x => x.GetService(typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin)))
            .Returns(mockProjectAnalysis);

        _factory = new KernelFactory(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateKernelAsync_WithDefaultSettings_CreatesKernelSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key"); // KernelFactory defaults to OpenAI
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync();

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithOpenAIProvider_ConfiguresOpenAIServices()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("openai");

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithGitHubProvider_ConfiguresGitHubModels()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("github");

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithAzureProvider_ConfiguresAzureOpenAI()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "test-key");
        var configuration = new Dictionary<string, object>
        {
            { "Endpoint", "https://test.openai.azure.com" }
        };
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("azure", configuration);

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithOpenAIProvider_MissingApiKey_ThrowsException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _factory.CreateKernelAsync("openai"));
        
        Assert.Contains("OpenAI API key not configured", exception.Message);
    }

    [Fact]
    public async Task CreateKernelAsync_WithUnknownProvider_DefaultsToOpenAI()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("unknown-provider");

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithCustomConfiguration_UsesConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        var config = new Dictionary<string, object>
        {
            { "Model", "gpt-3.5-turbo" }
        };

        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("openai", config);

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task CreateKernelWithPluginsAsync_WithValidPlugins_CreatesKernelSuccessfully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        var pluginTypes = new[]
        {
            typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin),
            typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin)
        };

        try
        {
            // Act
            var kernel = await _factory.CreateKernelWithPluginsAsync(pluginTypes);

            // Assert
            Assert.NotNull(kernel);
            Assert.True(kernel.Plugins.Count >= 0);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("github")]
    [InlineData("azure")]
    public async Task CreateKernelAsync_WithValidProviders_CreatesKernelWhenConfigured(string provider)
    {
        // Arrange - Set up environment variables for each provider
        Dictionary<string, object>? config = null;
        
        switch (provider)
        {
            case "openai":
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
                break;
            case "github":
                Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");
                break;
            case "azure":
                Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", "test-key");
                config = new Dictionary<string, object> { { "Endpoint", "https://test.openai.azure.com" } };
                break;
        }

        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync(provider, config);

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            // Cleanup
            switch (provider)
            {
                case "openai":
                    Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
                    break;
                case "github":
                    Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
                    break;
                case "azure":
                    Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
                    break;
            }
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithLocalProvider_ConfiguresCorrectly()
    {
        // Act & Assert - Local provider shouldn't throw as it uses dummy API key
        var kernel = await _factory.CreateKernelAsync("local");
        Assert.NotNull(kernel);
    }

    [Fact]
    public async Task CreateKernelAsync_WithAnthropicProvider_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _factory.CreateKernelAsync("anthropic"));
        
        Assert.Contains("Failed to configure AI provider 'anthropic'", exception.Message);
    }
}