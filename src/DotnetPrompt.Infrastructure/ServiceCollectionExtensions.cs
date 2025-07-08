using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.Configuration;
using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Infrastructure.Filters;
using DotnetPrompt.Infrastructure.Middleware;
using DotnetPrompt.Infrastructure.Progress;
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
    /// Adds comprehensive SK error handling and logging filters
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSemanticKernelErrorHandling(this IServiceCollection services)
    {
        // Register main workflow execution filter (already implements both interfaces)
        services.AddSingleton<IFunctionInvocationFilter, WorkflowExecutionFilter>();
        services.AddSingleton<IPromptRenderFilter>(serviceProvider => 
            serviceProvider.GetRequiredService<IFunctionInvocationFilter>() as WorkflowExecutionFilter 
            ?? throw new InvalidOperationException("WorkflowExecutionFilter must implement IPromptRenderFilter"));

        // Register additional specialized filters
        services.AddSingleton<IFunctionInvocationFilter, SecurityValidationFilter>();
        services.AddSingleton<IPromptRenderFilter, SecurityValidationFilter>();
        services.AddSingleton<IFunctionInvocationFilter, PerformanceMonitoringFilter>();
        services.AddSingleton<IPromptRenderFilter, PerformanceMonitoringFilter>();

        // Register progress tracking filter if progress manager is available
        services.AddSingleton<IFunctionInvocationFilter, ProgressTrackingFilter>();

        // Register middleware (as additional filters)
        services.AddSingleton<IFunctionInvocationFilter, RetryMiddleware>();
        services.AddSingleton<IFunctionInvocationFilter, CircuitBreakerMiddleware>();

        // Register options for middleware
        services.AddSingleton<RetryOptions>();
        services.AddSingleton<CircuitBreakerOptions>();

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
    /// Adds progress tracking and resume services using Semantic Kernel
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddProgressTrackingServices(this IServiceCollection services)
    {
        // Register progress manager with in-memory storage for MVP
        services.AddSingleton<IProgressManager, SkProgressManager>();
        
        // Progress tracking filter is already added in AddSemanticKernelErrorHandling
        
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
        
        // Add comprehensive error handling and logging
        services.AddSemanticKernelErrorHandling();
        
        // Register essential SK plugins (excluding WorkflowExecutorPlugin which is replaced by SK native capabilities)
        services.AddTransient<FileOperationsPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        // NOTE: WorkflowExecutorPlugin is intentionally excluded - replaced by SK Handlebars templating
        
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
        services.AddProgressTrackingServices();
        
        return services;
    }
}