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
    /// Adds Semantic Kernel services and plugins to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
    {
        // Register core SK services
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddSingleton<ISemanticKernelOrchestrator, SemanticKernelOrchestrator>();
        
        // Register SK plugins
        services.AddTransient<WorkflowExecutorPlugin>();
        services.AddTransient<FileOperationsPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        
        // Register SK filters
        services.AddSingleton<IFunctionInvocationFilter, WorkflowExecutionFilter>();
        
        return services;
    }

    /// <summary>
    /// Adds all infrastructure services including SK integration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddConfigurationServices();
        services.AddSemanticKernelServices();
        
        return services;
    }
}