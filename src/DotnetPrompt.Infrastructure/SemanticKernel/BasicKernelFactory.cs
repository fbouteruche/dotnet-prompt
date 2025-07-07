using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Basic kernel factory for Task 4.1 - creates SK kernels without MCP/Vector Store
/// This is a simplified version for the core orchestrator refactoring phase
/// </summary>
public class BasicKernelFactory : IKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BasicKernelFactory> _logger;

    public BasicKernelFactory(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<BasicKernelFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Kernel> CreateKernelAsync(string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        var builder = Kernel.CreateBuilder();
        
        // Configure AI services via Microsoft.Extensions.AI pattern
        await ConfigureAIServicesAsync(builder, providerName, configuration);
        
        // Register essential services and plugins in the kernel's DI container
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IConfigurationService>());
        
        // Register plugin dependencies
        builder.Services.AddLogging();
        builder.Services.AddSingleton<FileOperationsPlugin>();
        builder.Services.AddSingleton<ProjectAnalysisPlugin>();
        
        var kernel = builder.Build();
        
        // Add plugins to kernel after building (only if plugins exist)
        try
        {
            kernel.Plugins.AddFromObject(kernel.Services.GetRequiredService<FileOperationsPlugin>(), "FileSystem");
            kernel.Plugins.AddFromObject(kernel.Services.GetRequiredService<ProjectAnalysisPlugin>(), "ProjectAnalysis");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add some plugins to kernel");
        }
        
        _logger.LogInformation("BasicKernelFactory created kernel with {PluginCount} plugins", 
            kernel.Plugins.Count());
        
        return kernel;
    }

    public async Task<Kernel> CreateKernelWithPluginsAsync(Type[] pluginTypes, string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        var builder = Kernel.CreateBuilder();
        
        // Configure AI services
        await ConfigureAIServicesAsync(builder, providerName, configuration);
        
        // Register specified plugins with DI container
        foreach (var pluginType in pluginTypes)
        {
            // Skip WorkflowExecutorPlugin - it's being replaced by SK native capabilities
            if (pluginType.Name == "WorkflowExecutorPlugin")
            {
                _logger.LogInformation("Skipping WorkflowExecutorPlugin - replaced by SK native Handlebars templating");
                continue;
            }
            
            // Register the plugin type with the DI container
            builder.Services.AddSingleton(pluginType);
        }
        
        // Register essential services and logging
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IConfigurationService>());
        builder.Services.AddLogging();
        
        var kernel = builder.Build();
        
        // Add plugins to kernel after building
        foreach (var pluginType in pluginTypes)
        {
            // Skip WorkflowExecutorPlugin - it's being replaced by SK native capabilities
            if (pluginType.Name == "WorkflowExecutorPlugin")
            {
                continue;
            }
            
            try
            {
                var plugin = kernel.Services.GetRequiredService(pluginType);
                kernel.Plugins.AddFromObject(plugin, pluginType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add plugin {PluginType} to kernel", pluginType.Name);
            }
        }
        
        _logger.LogInformation("BasicKernelFactory created kernel with {PluginCount} plugins for custom plugin set", 
            kernel.Plugins.Count());
        
        return kernel;
    }

    private async Task ConfigureAIServicesAsync(IKernelBuilder builder, string? providerName, Dictionary<string, object>? config)
    {
        await Task.CompletedTask; // Make async for future extension
        
        // Use configuration service to determine provider
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var effectiveProvider = providerName ?? "openai"; // Default to OpenAI
        
        _logger.LogInformation("Configuring AI services for provider: {Provider}", effectiveProvider);
        
        switch (effectiveProvider.ToLowerInvariant())
        {
            case "openai":
                ConfigureOpenAI(builder, config);
                break;
            case "github":
                ConfigureGitHubModels(builder, config);
                break;
            case "azure":
                ConfigureAzureOpenAI(builder, config);
                break;
            default:
                _logger.LogWarning("Unknown provider {Provider}, defaulting to OpenAI", effectiveProvider);
                ConfigureOpenAI(builder, config);
                break;
        }
    }

    private void ConfigureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY not found, OpenAI services may not work");
            return;
        }

        var model = config?.GetValueOrDefault("model")?.ToString() ?? "gpt-4o";
        
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(model, apiKey);
        #pragma warning restore SKEXP0010
        
        _logger.LogDebug("Configured OpenAI with model: {Model}", model);
    }

    private void ConfigureGitHubModels(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GITHUB_TOKEN not found, GitHub Models may not work");
            return;
        }

        var model = config?.GetValueOrDefault("model")?.ToString() ?? "gpt-4o";
        
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            model, 
            token, 
            httpClient: new HttpClient { BaseAddress = new Uri("https://models.inference.ai.azure.com/") });
        #pragma warning restore SKEXP0010
        
        _logger.LogDebug("Configured GitHub Models with model: {Model}", model);
    }

    private void ConfigureAzureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("Azure OpenAI configuration incomplete, service may not work");
            return;
        }

        var model = config?.GetValueOrDefault("model")?.ToString() ?? "gpt-4";
        
        #pragma warning disable SKEXP0010
        builder.AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
        #pragma warning restore SKEXP0010
        
        _logger.LogDebug("Configured Azure OpenAI with model: {Model}", model);
    }
}