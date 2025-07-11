using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Mcp;
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

    /// <summary>
    /// Creates a Kernel with specific plugins and MCP servers from workflow
    /// </summary>
    /// <param name="workflow">Workflow containing MCP configuration</param>
    /// <param name="pluginTypes">Types of plugins to register</param>
    /// <param name="providerName">AI provider name</param>
    /// <param name="configuration">Provider-specific configuration</param>
    /// <returns>Configured Kernel instance with specified plugins and MCP servers</returns>
    Task<Kernel> CreateKernelWithWorkflowAsync(DotpromptWorkflow? workflow = null, Type[]? pluginTypes = null, string? providerName = null, Dictionary<string, object>? configuration = null);
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
        return await CreateKernelWithWorkflowAsync(null, null, providerName, configuration);
    }

    public async Task<Kernel> CreateKernelWithPluginsAsync(Type[] pluginTypes, string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        return await CreateKernelWithWorkflowAsync(null, pluginTypes, providerName, configuration);
    }

    public async Task<Kernel> CreateKernelWithWorkflowAsync(DotpromptWorkflow? workflow = null, Type[]? pluginTypes = null, string? providerName = null, Dictionary<string, object>? configuration = null)
    {
        var builder = Kernel.CreateBuilder();

        // Configure AI services per the architecture's Microsoft.Extensions.AI + SK pattern
        await ConfigureAIServicesAsync(builder, providerName, configuration);

        // Register workflow-specific services
        builder.Services.AddSingleton(_serviceProvider.GetRequiredService<IConfigurationService>());

        // Get the workflow execution filter from the main service provider
        var workflowFilter = _serviceProvider.GetRequiredService<IFunctionInvocationFilter>();
        builder.Services.AddSingleton(workflowFilter);

        // Add MCP execution filter
        var mcpFilter = _serviceProvider.GetService<McpExecutionFilter>();
        if (mcpFilter != null)
        {
            builder.Services.AddSingleton<IFunctionInvocationFilter>(mcpFilter);
        }

        // Build the kernel
        var kernel = builder.Build();

        // Register built-in plugins
        var defaultPluginTypes = pluginTypes ?? new[]
        {
            typeof(Plugins.FileSystemPlugin),
            typeof(Plugins.ProjectAnalysisPlugin)
            // TODO: Re-enable SubWorkflowPlugin when its dependencies are properly registered in tests
            // typeof(Plugins.SubWorkflowPlugin)
        };

        foreach (var pluginType in defaultPluginTypes)
        {
            var plugin = _serviceProvider.GetRequiredService(pluginType);
            var pluginName = pluginType.Name.Replace("Plugin", "");
            kernel.Plugins.AddFromObject(plugin, pluginName);
            
            _logger.LogDebug("Registered SK plugin: {PluginName} from {PluginType}", pluginName, pluginType.Name);
        }

        // Add MCP servers if specified in workflow
        if (workflow != null)
        {
            try
            {
                await kernel.AddMcpServersFromWorkflowAsync(workflow, _serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register MCP servers from workflow, continuing without MCP integration");
                // Continue without MCP rather than failing the entire kernel creation
            }
        }

        _logger.LogInformation("Created Kernel with {PluginCount} plugins for provider {Provider}", 
            defaultPluginTypes.Length, providerName ?? "default");

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
                    var supportedProviders = "openai, github, azure, anthropic, local, ollama";
                    var errorMessage = $"Unknown AI provider: '{providerName}'. Supported providers are: {supportedProviders}. " +
                                     "Please specify a valid provider in your workflow frontmatter or configuration.";
                    _logger.LogError("Unknown AI provider specified: {Provider}", providerName);
                    throw new InvalidOperationException(errorMessage);
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
                 ?? throw new InvalidOperationException(
                    "No model specified for OpenAI provider. " +
                    "Please specify a model in workflow frontmatter (model: \"gpt-4\") " +
                    "or configure AI:OpenAI:Model in your settings.");

        builder.AddOpenAIChatCompletion(model, apiKey);
        _logger.LogInformation("Configured OpenAI with model: {Model}", model);
    }

    private void ConfigureGitHubModels(IKernelBuilder builder, Dictionary<string, object>? config)
    {
        var token = GetConfigValue(config, "Token") 
                 ?? _configuration["AI:GitHub:Token"]
                 ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                 ?? throw new InvalidOperationException("GitHub token not configured. Set GITHUB_TOKEN environment variable or configure in settings.");

        var model = GetConfigValue(config, "Model") 
                 ?? _configuration["AI:GitHub:Model"] 
                 ?? throw new InvalidOperationException(
                    "No model specified for GitHub Models provider. " +
                    "Please specify a model in workflow frontmatter (model: \"gpt-4o\") " +
                    "or configure AI:GitHub:Model in your settings.");

        // GitHub Models uses OpenAI-compatible API
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: token,
            endpoint: new Uri("https://models.inference.ai.azure.com"));
        #pragma warning restore SKEXP0010
        
        _logger.LogInformation("Configured GitHub Models with model: {Model}", model);
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
                 ?? throw new InvalidOperationException(
                    "No model specified for Azure OpenAI provider. " +
                    "Please specify a model in workflow frontmatter (model: \"gpt-4\") " +
                    "or configure AI:Azure:Model in your settings.");

        builder.AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
        _logger.LogInformation("Configured Azure OpenAI with model: {Model} at endpoint: {Endpoint}", model, endpoint);
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
                 ?? throw new InvalidOperationException(
                    "No model specified for local provider. " +
                    "Please specify a model in workflow frontmatter (model: \"llama3\") " +
                    "or configure AI:Local:Model in your settings.");

        // For local providers like Ollama, we can use OpenAI-compatible API
        #pragma warning disable SKEXP0010
        builder.AddOpenAIChatCompletion(
            modelId: model,
            apiKey: "dummy", // Local providers often don't require API keys
            endpoint: new Uri(endpoint));
        #pragma warning restore SKEXP0010
        
        _logger.LogInformation("Configured local provider with model: {Model} at endpoint: {Endpoint}", model, endpoint);
    }

    private static string? GetConfigValue(Dictionary<string, object>? config, string key)
    {
        return config?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
    }
}