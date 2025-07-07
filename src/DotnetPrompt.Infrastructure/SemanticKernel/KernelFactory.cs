using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Factory interface for creating Semantic Kernel instances
/// </summary>
public interface IKernelFactory
{
    /// <summary>
    /// Creates a configured Kernel instance with all required plugins and services
    /// </summary>
    /// <param name="providerName">AI provider name (openai, azure, github, etc.)</param>
    /// <param name="configuration">Provider-specific configuration</param>
    /// <returns>Configured Kernel instance</returns>
    Task<Kernel> CreateKernelAsync(string? providerName = null, Dictionary<string, object>? configuration = null);

    /// <summary>
    /// Creates a Kernel with specific plugins registered
    /// </summary>
    /// <param name="pluginTypes">Types of plugins to register</param>
    /// <param name="providerName">AI provider name</param>
    /// <param name="configuration">Provider-specific configuration</param>
    /// <returns>Configured Kernel instance with specified plugins</returns>
    Task<Kernel> CreateKernelWithPluginsAsync(Type[] pluginTypes, string? providerName = null, Dictionary<string, object>? configuration = null);
}

/// <summary>
/// Factory for creating and configuring Semantic Kernel instances with proper AI provider integration
/// Implements the architecture's Microsoft.Extensions.AI + SK pattern
/// </summary>
public class KernelFactory : IKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KernelFactory> _logger;

    public KernelFactory(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<KernelFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Kernel> CreateKernelAsync(string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        // Add all built-in workflow plugins by default
        var pluginTypes = new[]
        {
            typeof(Plugins.WorkflowExecutorPlugin),
            typeof(Plugins.FileOperationsPlugin),
            typeof(Plugins.ProjectAnalysisPlugin)
        };

        return await CreateKernelWithPluginsAsync(pluginTypes, providerName, configuration);
    }

    public async Task<Kernel> CreateKernelWithPluginsAsync(Type[] pluginTypes, string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        var builder = Kernel.CreateBuilder();

        // Configure AI services per the architecture's Microsoft.Extensions.AI + SK pattern
        await ConfigureAIServicesAsync(builder, providerName, configuration);

        // Register workflow-specific services
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IConfigurationService>());

        // Get the workflow execution filter from the main service provider
        var workflowFilter = _serviceProvider.GetRequiredService<IFunctionInvocationFilter>();
        builder.Services.AddSingleton(workflowFilter);

        // Build the kernel
        var kernel = builder.Build();

        // Register plugins with proper SK function annotations
        foreach (var pluginType in pluginTypes)
        {
            var plugin = _serviceProvider.GetRequiredService(pluginType);
            var pluginName = pluginType.Name.Replace("Plugin", "");
            kernel.Plugins.AddFromObject(plugin, pluginName);
            
            _logger.LogDebug("Registered SK plugin: {PluginName} from {PluginType}", pluginName, pluginType.Name);
        }

        _logger.LogInformation("Created Kernel with {PluginCount} plugins for provider {Provider}", 
            pluginTypes.Length, providerName ?? "default");

        return kernel;
    }

    private async Task ConfigureAIServicesAsync(IKernelBuilder builder, string? providerName, Dictionary<string, object>? configuration)
    {
        // Get provider from hierarchy: parameter > configuration > environment > default
        providerName ??= _configuration["AI:DefaultProvider"] 
                      ?? Environment.GetEnvironmentVariable("DOTNET_PROMPT_PROVIDER")
                      ?? "github";

        _logger.LogInformation("Configuring AI provider: {Provider}", providerName);

        try
        {
            switch (providerName.ToLowerInvariant())
            {
                case "openai":
                    ConfigureOpenAI(builder, configuration);
                    break;
                case "github":
                    ConfigureGitHubModels(builder, configuration);
                    break;
                case "azure":
                    ConfigureAzureOpenAI(builder, configuration);
                    break;
                case "anthropic":
                    ConfigureAnthropic(builder, configuration);
                    break;
                case "local":
                case "ollama":
                    ConfigureLocalProvider(builder, configuration);
                    break;
                default:
                    _logger.LogWarning("Unknown AI provider: {Provider}, falling back to OpenAI", providerName);
                    ConfigureOpenAI(builder, configuration);
                    break;
            }

            _logger.LogInformation("Successfully configured AI services for provider: {Provider}", providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure AI provider: {Provider}", providerName);
            throw new InvalidOperationException($"Failed to configure AI provider '{providerName}': {ex.Message}", ex);
        }

        await Task.CompletedTask;
    }

    private void ConfigureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var apiKey = GetConfigValue(config, "ApiKey") 
                  ?? _configuration["AI:OpenAI:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                  ?? throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable or configure in settings.");

        var model = GetConfigValue(config, "Model") 
                 ?? _configuration["AI:OpenAI:Model"] 
                 ?? "gpt-4o";

        builder.AddOpenAIChatCompletion(model, apiKey);
        _logger.LogDebug("Configured OpenAI with model: {Model}", model);
    }

    private void ConfigureGitHubModels(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var token = GetConfigValue(config, "Token") 
                 ?? _configuration["AI:GitHub:Token"]
                 ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                 ?? throw new InvalidOperationException("GitHub token not configured. Set GITHUB_TOKEN environment variable or configure in settings.");

        var model = GetConfigValue(config, "Model") 
                 ?? _configuration["AI:GitHub:Model"] 
                 ?? "gpt-4o";

        // GitHub Models uses OpenAI-compatible API
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: token,
            endpoint: new Uri("https://models.inference.ai.azure.com"));
        #pragma warning restore SKEXP0010
        
        _logger.LogDebug("Configured GitHub Models with model: {Model}", model);
    }

    private void ConfigureAzureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var endpoint = GetConfigValue(config, "Endpoint") 
                    ?? _configuration["AI:Azure:Endpoint"]
                    ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");

        var apiKey = GetConfigValue(config, "ApiKey") 
                  ?? _configuration["AI:Azure:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                  ?? throw new InvalidOperationException("Azure OpenAI API key not configured. Set AZURE_OPENAI_API_KEY environment variable or configure in settings.");

        var model = GetConfigValue(config, "Model") 
                 ?? _configuration["AI:Azure:Model"] 
                 ?? "gpt-4o";

        builder.AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
        _logger.LogDebug("Configured Azure OpenAI with model: {Model} at endpoint: {Endpoint}", model, endpoint);
    }

    private void ConfigureAnthropic(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        // Note: Anthropic support requires additional packages, this is a placeholder
        throw new NotImplementedException("Anthropic provider support is not yet implemented. Please use OpenAI, Azure, or GitHub providers.");
    }

    private void ConfigureLocalProvider(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var endpoint = GetConfigValue(config, "Endpoint") 
                    ?? _configuration["AI:Local:Endpoint"]
                    ?? "http://localhost:11434"; // Default Ollama endpoint

        var model = GetConfigValue(config, "Model") 
                 ?? _configuration["AI:Local:Model"] 
                 ?? "llama3";

        // For local providers like Ollama, we can use OpenAI-compatible API
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: "dummy", // Local providers often don't require API keys
            endpoint: new Uri(endpoint));
        #pragma warning restore SKEXP0010
        
        _logger.LogDebug("Configured local provider with model: {Model} at endpoint: {Endpoint}", model, endpoint);
    }

    private static string? GetConfigValue(Dictionary<string, object>? config, string key)
    {
        return config?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
    }
}