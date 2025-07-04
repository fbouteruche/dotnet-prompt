using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.Configuration;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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
    /// Adds AI provider services with framework-agnostic interfaces
    /// Uses Semantic Kernel as the implementation but through agnostic interfaces
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAiProviderServices(this IServiceCollection services)
    {
        // Register core AI orchestration services with framework-agnostic interfaces
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddSingleton<IWorkflowOrchestrator, SemanticKernelOrchestrator>();
        
        // Register SK plugins (implementation detail behind the orchestrator)
        services.AddTransient<WorkflowExecutorPlugin>();
        services.AddTransient<FileOperationsPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        
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