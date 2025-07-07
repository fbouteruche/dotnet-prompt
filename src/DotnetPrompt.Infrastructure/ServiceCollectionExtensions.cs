using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.Configuration;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace DotnetPrompt.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        return services;
    }

    /// <summary>
    /// Adds Semantic Kernel orchestrator services with native Handlebars templating
    /// This replaces custom variable substitution with SK native capabilities
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSemanticKernelOrchestrator(this IServiceCollection services)
    {
        // Register Handlebars factory for SK native templating
        services.AddSingleton<IPromptTemplateFactory, HandlebarsPromptTemplateFactory>();
        
        // Register orchestrator (replaces WorkflowExecutorPlugin usage)
        services.AddScoped<IWorkflowOrchestrator, SemanticKernelOrchestrator>();
        
        // Register kernel factory (no MCP yet)
        services.AddSingleton<IKernelFactory, KernelFactory>();
        
        return services;
    }

    /// <summary>
    /// Adds AI provider services with framework-agnostic interfaces
    /// Uses Semantic Kernel as the implementation but through agnostic interfaces
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAiProviderServices(this IServiceCollection services)
    {
        // Use the new SK orchestrator instead of the old approach
        services.AddSemanticKernelOrchestrator();
        
        // Register essential SK plugins (excluding WorkflowExecutorPlugin which is replaced by SK native capabilities)
        services.AddTransient<FileOperationsPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        // NOTE: WorkflowExecutorPlugin is intentionally excluded - replaced by SK Handlebars templating
        
        // Register SK filters (implementation detail)
        services.AddSingleton<IFunctionInvocationFilter, WorkflowExecutionFilter>();
        
        return services;
    }

    /// <summary>
    /// Adds Semantic Kernel services and plugins (legacy method for compatibility)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    [Obsolete("Use AddAiProviderServices for framework-agnostic approach")]
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        return services.AddAiProviderServices();
    }

    /// <summary>
    /// Adds all infrastructure services including AI provider integration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddConfigurationServices();
        services.AddAiProviderServices();
        
        return services;
    }
}