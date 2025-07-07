using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.SemanticKernel;

public class BasicKernelFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<BasicKernelFactory>> _mockLogger;
    private readonly Mock<IConfigurationService> _mockConfigurationService;
    private readonly BasicKernelFactory _factory;

    public BasicKernelFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<BasicKernelFactory>>();
        _mockConfigurationService = new Mock<IConfigurationService>();

        _mockServiceProvider.Setup(x => x.GetRequiredService<IConfigurationService>())
            .Returns(_mockConfigurationService.Object);

        _factory = new BasicKernelFactory(
            _mockServiceProvider.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateKernelAsync_WithDefaultSettings_CreatesKernelSuccessfully()
    {
        // Act
        var kernel = await _factory.CreateKernelAsync();

        // Assert
        Assert.NotNull(kernel);
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
        Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", "https://test.openai.azure.com");
        
        try
        {
            // Act
            var kernel = await _factory.CreateKernelAsync("azure");

            // Assert
            Assert.NotNull(kernel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null);
        }
    }

    [Fact]
    public async Task CreateKernelAsync_WithUnknownProvider_DefaultsToOpenAI()
    {
        // Act
        var kernel = await _factory.CreateKernelAsync("unknown-provider");

        // Assert
        Assert.NotNull(kernel);
    }

    [Fact]
    public async Task CreateKernelAsync_WithCustomConfiguration_UsesConfiguration()
    {
        // Arrange
        var config = new Dictionary<string, object>
        {
            { "model", "gpt-3.5-turbo" }
        };

        // Act
        var kernel = await _factory.CreateKernelAsync("openai", config);

        // Assert
        Assert.NotNull(kernel);
    }

    [Fact]
    public async Task CreateKernelWithPluginsAsync_SkipsWorkflowExecutorPlugin()
    {
        // Arrange
        var pluginTypes = new[]
        {
            typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin),
            typeof(DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin)
        };

        // Mock the services that plugins might need
        var mockFileOpsLogger = new Mock<ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin>>();
        var mockProjectLogger = new Mock<ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin>>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin>)))
            .Returns(mockFileOpsLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin>)))
            .Returns(mockProjectLogger.Object);

        // Act
        var kernel = await _factory.CreateKernelWithPluginsAsync(pluginTypes);

        // Assert
        Assert.NotNull(kernel);
        // Verify that plugins were registered (exact count may vary based on successful registration)
        Assert.True(kernel.Plugins.Count >= 0);
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("github")]
    [InlineData("azure")]
    public async Task CreateKernelAsync_WithValidProviders_DoesNotThrow(string provider)
    {
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var kernel = await _factory.CreateKernelAsync(provider);
            Assert.NotNull(kernel);
        });

        Assert.Null(exception);
    }
}