using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Core.Models.Enums;
using DotnetPrompt.Core.Models.RoslynAnalysis;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;

namespace DotnetPrompt.UnitTests.Utilities;

/// <summary>
/// Utility class for creating test service collections with standardized mock services
/// </summary>
public static class TestServiceCollectionBuilder
{
    /// <summary>
    /// Creates a service collection with all infrastructure services for testing
    /// </summary>
    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add configuration
        services.AddSingleton<IConfiguration>(CreateTestConfiguration());

        // Add infrastructure services
        AddInfrastructureServices(services);
        AddConfigurationServices(services);
        AddSemanticKernelOrchestrator(services);

        return services;
    }

    /// <summary>
    /// Creates a minimal service collection for basic testing
    /// </summary>
    public static IServiceCollection CreateMinimalTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add configuration
        services.AddSingleton<IConfiguration>(CreateTestConfiguration());

        return services;
    }

    /// <summary>
    /// Creates a service collection specifically for Semantic Kernel testing
    /// </summary>
    public static IServiceCollection CreateSemanticKernelTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add configuration
        services.AddSingleton<IConfiguration>(CreateTestConfiguration());

        // Add Semantic Kernel services
        AddSemanticKernelOrchestrator(services);
        
        // Add configuration services for kernel factory
        AddConfigurationServices(services);

        // Add required function invocation filters for KernelFactory
        AddMockFunctionInvocationFilters(services);

        // Add required plugins for KernelFactory
        AddMockPluginServices(services);

        // Register IKernelFactory
        services.AddTransient<IKernelFactory, KernelFactory>();

        return services;
    }

    /// <summary>
    /// Creates a service collection for Roslyn analysis testing
    /// </summary>
    public static IServiceCollection CreateRoslynAnalysisTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add configuration
        services.AddSingleton<IConfiguration>(CreateTestConfiguration());

        // Add mock Roslyn analysis service
        services.AddSingleton<IRoslynAnalysisService, MockRoslynAnalysisService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services to the service collection
    /// </summary>
    private static void AddInfrastructureServices(IServiceCollection services)
    {
        // Add mock services
        services.AddSingleton<IRoslynAnalysisService, MockRoslynAnalysisService>();
    }

    /// <summary>
    /// Adds configuration services to the service collection
    /// </summary>
    private static void AddConfigurationServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService, MockConfigurationService>();
    }

    /// <summary>
    /// Adds Semantic Kernel orchestrator services to the service collection
    /// </summary>
    private static void AddSemanticKernelOrchestrator(IServiceCollection services)
    {
        // Register IKernelFactory
        services.AddTransient<IKernelFactory, KernelFactory>();
    }

    /// <summary>
    /// Adds required function invocation filters for KernelFactory
    /// </summary>
    private static void AddMockFunctionInvocationFilters(IServiceCollection services)
    {
        // Add a mock primary function invocation filter (KernelFactory expects this)
        services.AddSingleton<IFunctionInvocationFilter, MockFunctionInvocationFilter>();
    }

    /// <summary>
    /// Adds mock plugin services required by KernelFactory
    /// </summary>
    private static void AddMockPluginServices(IServiceCollection services)
    {
        // Add FileSystem options required by FileSystemPlugin
        services.Configure<FileSystemOptions>(options =>
        {
            options.AllowedDirectories = new[] { Directory.GetCurrentDirectory() };
            options.BlockedDirectories = new[] { "bin", "obj", ".git" };
            options.MaxFileSizeBytes = 1024 * 1024;
            options.EnableAuditLogging = false;
        });

        // Add mock Roslyn analysis service required by ProjectAnalysisPlugin
        services.AddSingleton<IRoslynAnalysisService, MockRoslynAnalysisService>();

        // Add required plugins
        services.AddTransient<FileSystemPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
    }

    /// <summary>
    /// Creates a test configuration with default values
    /// </summary>
    private static IConfiguration CreateTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "DefaultProvider", "ollama" },
            { "DefaultModel", "test-model" },
            { "Timeout", "30" },
            { "CacheEnabled", "false" },
            { "TelemetryEnabled", "false" }
        });
        return configBuilder.Build();
    }
}

/// <summary>
/// Mock implementation of IRoslynAnalysisService for testing
/// </summary>
public class MockRoslynAnalysisService : IRoslynAnalysisService
{
    public Task<RoslynAnalysisResult> AnalyzeAsync(string projectPath, AnalysisOptions options, CancellationToken cancellationToken = default)
    {
        var result = new RoslynAnalysisResult
        {
            Success = true,
            ProjectPath = projectPath,
            AnalysisTimestamp = DateTime.UtcNow.ToString("O"),
            AnalysisDepth = options.AnalysisDepth,
            SemanticDepth = options.SemanticDepth,
            CompilationStrategy = options.CompilationStrategy,
            ExecutionTimeMs = 100,
            ProjectMetadata = new ProjectMetadata
            {
                Name = "TestProject",
                TargetFrameworks = new List<string> { "net8.0" },
                ProjectType = "Console",
                NullableEnabled = true
            },
            Structure = new ProjectStructure
            {
                TotalFiles = 5,
                TotalLines = 150,
                Directories = new List<DirectoryStructureInfo>
                {
                    new() { Name = "Controllers", RelativePath = "Controllers", FileCount = 2, LineCount = 80 },
                    new() { Name = "Models", RelativePath = "Models", FileCount = 3, LineCount = 70 }
                },
                SourceFiles = new List<SourceFileInfo>
                {
                    new() { Name = "Program.cs", RelativePath = "Program.cs", LineCount = 50, SizeBytes = 1024 }
                }
            },
            Dependencies = new DependencyAnalysis
            {
                PackageReferences = new List<PackageReference>
                {
                    new() { Name = "Microsoft.Extensions.Hosting", Version = "8.0.0", Type = "Framework" }
                },
                ProjectReferences = new List<ProjectReference>(),
                DependencyCounts = new DependencyCount 
                { 
                    DirectPackages = 1, 
                    TransitivePackages = 5 
                }
            },
            Recommendations = new List<Recommendation>
            {
                new()
                {
                    Category = "Performance",
                    Priority = "Low",
                    Message = "Test recommendation for mock analysis",
                    Actionable = true
                }
            },
            Performance = new PerformanceMetrics
            {
                TotalTimeMs = 100,
                FilesProcessed = 5,
                MemoryUsageMB = 50.0
            },
            Metadata = new Dictionary<string, object>
            {
                { "tool", "roslyn-analyzer" },
                { "version", "1.0.0" }
            }
        };

        return Task.FromResult(result);
    }
}

/// <summary>
/// Mock implementation of IConfigurationService for testing
/// </summary>
public class MockConfigurationService : IConfigurationService
{
    public Task<DotPromptConfiguration> LoadConfigurationAsync(
        string? cliProvider = null,
        string? cliModel = null,
        bool? cliVerbose = null,
        string? cliConfigFile = null,
        string? projectPath = null,
        string? workflowModel = null,
        CancellationToken cancellationToken = default)
    {
        var config = new DotPromptConfiguration
        {
            DefaultProvider = cliProvider ?? "ollama",
            DefaultModel = cliModel ?? workflowModel ?? "test-model",
            Providers = new Dictionary<string, ProviderConfiguration>
            {
                {
                    "ollama", new ProviderConfiguration
                    {
                        BaseUrl = "http://localhost:11434",
                        ApiKey = "test-key"
                    }
                }
            },
            Timeout = 30,
            CacheEnabled = false,
            TelemetryEnabled = false
        };

        return Task.FromResult(config);
    }

    public ConfigurationValidationResult ValidateConfiguration(DotPromptConfiguration configuration)
    {
        var result = new ConfigurationValidationResult
        {
            IsValid = true,
            Errors = new List<ConfigurationValidationError>(),
            Warnings = new List<ConfigurationValidationWarning>()
        };

        return result;
    }

    public Task SaveConfigurationAsync(
        DotPromptConfiguration configuration,
        bool isGlobal = true,
        string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public string? GetConfigurationValue(string key, string? defaultValue = null)
    {
        return key switch
        {
            "DefaultProvider" => "ollama",
            "DefaultModel" => "test-model",
            "Timeout" => "30",
            "CacheEnabled" => "false",
            "TelemetryEnabled" => "false",
            _ => defaultValue
        };
    }

    public string GetGlobalConfigurationPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotprompt", "config.yaml");
    }

    public string GetProjectConfigurationPath(string projectPath)
    {
        return Path.Combine(projectPath, ".dotprompt.yaml");
    }
}

/// <summary>
/// Mock implementation of IFunctionInvocationFilter for testing
/// </summary>
public class MockFunctionInvocationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        // Simple pass-through implementation for testing
        await next(context);
    }
}
