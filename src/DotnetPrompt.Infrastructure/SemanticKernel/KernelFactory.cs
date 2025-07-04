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
/// Factory for creating and configuring Semantic Kernel instances
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
        var builder = Kernel.CreateBuilder();

        // Configure AI services
        await ConfigureAIServicesAsync(builder, providerName, configuration);

        // Add all built-in workflow plugins
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

        // Configure AI services
        await ConfigureAIServicesAsync(builder, providerName, configuration);

        // Register workflow-specific services
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IVariableResolver>());
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IConfigurationService>());

        // Add workflow execution filter
        builder.Services.AddSingleton<IFunctionInvocationFilter, WorkflowExecutionFilter>();

        var kernel = builder.Build();

        // Register plugins
        foreach (var pluginType in pluginTypes)
        {
            var plugin = _serviceProvider.GetRequiredService(pluginType);
            kernel.Plugins.AddFromObject(plugin, pluginType.Name.Replace("Plugin", ""));
        }

        _logger.LogInformation("Created Kernel with {PluginCount} plugins for provider {Provider}", 
            pluginTypes.Length, providerName ?? "default");

        return kernel;
    }

    private async Task ConfigureAIServicesAsync(IKernelBuilder builder, string? providerName, Dictionary<string, object>? configuration)
    {
        providerName ??= _configuration["AI:DefaultProvider"] ?? "openai";

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
            default:
                _logger.LogWarning("Unknown AI provider: {Provider}, falling back to OpenAI", providerName);
                ConfigureOpenAI(builder, configuration);
                break;
        }

        _logger.LogInformation("Configured AI services for provider: {Provider}", providerName);
        await Task.CompletedTask;
    }

    private void ConfigureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var apiKey = GetConfigValue(config, "ApiKey") ?? _configuration["AI:OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI API key not configured");

        var model = GetConfigValue(config, "Model") ?? _configuration["AI:OpenAI:Model"] ?? "gpt-4o";

        builder.AddOpenAIChatCompletion(model, apiKey);
    }

    private void ConfigureGitHubModels(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var token = GetConfigValue(config, "Token") ?? _configuration["AI:GitHub:Token"]
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GitHub token not configured");

        var model = GetConfigValue(config, "Model") ?? _configuration["AI:GitHub:Model"] ?? "gpt-4o";

        // GitHub Models uses OpenAI-compatible API
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: token,
            endpoint: new Uri("https://models.inference.ai.azure.com"));
    }

    private void ConfigureAzureOpenAI(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var endpoint = GetConfigValue(config, "Endpoint") ?? _configuration["AI:Azure:Endpoint"]
            ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");

        var apiKey = GetConfigValue(config, "ApiKey") ?? _configuration["AI:Azure:ApiKey"]
            ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        var model = GetConfigValue(config, "Model") ?? _configuration["AI:Azure:Model"] ?? "gpt-4o";

        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
        }
        else
        {
            // Use Azure Identity for authentication
            builder.AddAzureOpenAIChatCompletion(model, endpoint);
        }
    }

    private static string? GetConfigValue(Dictionary<string, object>? config, string key)
    {
        return config?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
    }
}