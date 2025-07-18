using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Parsing;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.IntegrationTests.Utilities;

/// <summary>
/// Enhanced utility class for setting up integration test services with consistent mocking patterns
/// Uses existing MockConfigurationService and MockWorkflowExecutionFilter from MockTestClasses.cs
/// </summary>
public static class TestServiceCollectionBuilder
{
    /// <summary>
    /// Creates a complete service collection configured for integration testing scenarios
    /// Uses valid 'ollama' provider configuration and existing mock services
    /// </summary>
    public static IServiceCollection CreateIntegrationTestServices()
    {
        var services = new ServiceCollection();
        
        // Add basic configuration for integration tests
        var configuration = CreateIntegrationTestConfiguration();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add integration-specific logging services
        AddIntegrationLoggingServices(services);
        
        // Add mock services using existing integration test mocks
        services.AddSingleton<IConfigurationService, MockConfigurationService>();
        services.AddSingleton<IFunctionInvocationFilter, MockWorkflowExecutionFilter>();
        services.AddSingleton<IRoslynAnalysisService, MockRoslynAnalysisService>();
        
        // Add core services and dependencies
        services.AddSingleton<IDotpromptParser, DotpromptParser>();
        
        // Configure FileSystemOptions for FileSystemPlugin integration tests
        services.Configure<FileSystemOptions>(options =>
        {
            options.MaxFileSizeBytes = 1024 * 1024; // 1MB for integration tests
            options.AllowedExtensions = new[] { ".md", ".txt", ".json", ".yaml", ".yml", ".cs", ".csproj" };
            options.BlockedDirectories = new[] { "bin", "obj", ".git", "node_modules" };
            options.AllowedDirectories = new[] { "." }; // Allow current directory for tests
        });
        
        // Register SK plugins with proper dependencies for integration testing
        services.AddSingleton<FileSystemPlugin>();
        services.AddSingleton<ProjectAnalysisPlugin>();
        
        // Add Semantic Kernel factory and orchestration services
        services.AddSingleton<IKernelFactory, KernelFactory>();
        
        return services;
    }
    
    /// <summary>
    /// Creates test configuration with valid provider settings for integration tests
    /// </summary>
    private static IConfiguration CreateIntegrationTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            // Use 'ollama' provider for integration tests (valid configuration)
            ["AI:DefaultProvider"] = "ollama",
            ["AI:DefaultModel"] = "test-model",
            
            // Local provider configuration (ollama uses local provider in KernelFactory)
            ["AI:Local:Endpoint"] = "http://localhost:11434",
            ["AI:Local:Model"] = "test-model",
            
            // OpenAI provider configuration (for tests that require model specification)
            ["AI:OpenAI:ApiKey"] = "test-api-key",
            ["AI:OpenAI:Model"] = "gpt-4o",
            
            // GitHub provider (for integration scenarios requiring real provider)
            ["AI:GitHub:Token"] = "integration-test-token",
            ["AI:GitHub:Model"] = "gpt-4o",
            ["AI:GitHub:BaseUrl"] = "https://models.inference.ai.azure.com",
            
            // Azure provider configuration
            ["AI:Azure:ApiKey"] = "test-azure-key",
            ["AI:Azure:Endpoint"] = "https://test.openai.azure.com",
            ["AI:Azure:Model"] = "gpt-4",
            
            // Logging configuration for integration tests
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:Microsoft"] = "Warning",
            ["Logging:LogLevel:DotnetPrompt"] = "Debug"
        };
        
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
    
    /// <summary>
    /// Adds comprehensive logging services for integration test scenarios
    /// </summary>
    private static void AddIntegrationLoggingServices(IServiceCollection services)
    {
        // Add all required logger instances for infrastructure components
        services.AddSingleton<ILogger<KernelFactory>>(new MockLogger<KernelFactory>());
        services.AddSingleton<ILogger<FileSystemPlugin>>(new MockLogger<FileSystemPlugin>());
        services.AddSingleton<ILogger<ProjectAnalysisPlugin>>(new MockLogger<ProjectAnalysisPlugin>());
        services.AddSingleton<ILogger<SubWorkflowPlugin>>(new MockLogger<SubWorkflowPlugin>());
    }
}

/// <summary>
/// Mock implementation of IRoslynAnalysisService for integration testing
/// Returns minimal mock analysis results for testing purposes
/// </summary>
public class MockRoslynAnalysisService : IRoslynAnalysisService
{
    public Task<RoslynAnalysisResult> AnalyzeAsync(
        string projectPath, 
        AnalysisOptions options, 
        CancellationToken cancellationToken = default)
    {
        var result = new RoslynAnalysisResult
        {
            ProjectPath = projectPath,
            AnalysisTimestamp = DateTime.UtcNow.ToString("O"),
            AnalysisDepth = options.AnalysisDepth,
            SemanticDepth = options.SemanticDepth,
            CompilationStrategy = options.CompilationStrategy,
            Success = true,
            ExecutionTimeMs = 150
        };
        
        return Task.FromResult(result);
    }
}
