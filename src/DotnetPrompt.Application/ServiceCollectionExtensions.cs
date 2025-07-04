using DotnetPrompt.Application.Execution;
using DotnetPrompt.Application.Execution.Steps;
using DotnetPrompt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetPrompt.Application;

/// <summary>
/// Extension methods for registering application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds workflow execution services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWorkflowExecutionServices(this IServiceCollection services)
    {
        // Register the AI-powered workflow engine as the primary implementation
        services.AddScoped<IWorkflowEngine, AiWorkflowEngine>();
        
        // Keep the original engine available for fallback if needed
        services.AddScoped<WorkflowEngine>();
        
        // Register variable resolver (still needed by custom step executors)
        services.AddScoped<IVariableResolver, VariableResolver>();
        
        // Keep step executors for backward compatibility and potential fallback
        services.AddScoped<IStepExecutor, PromptStepExecutor>();
        services.AddScoped<IStepExecutor, FileReadStepExecutor>();
        services.AddScoped<IStepExecutor, FileWriteStepExecutor>();
        
        return services;
    }

    /// <summary>
    /// Adds AI-powered workflow services with framework-agnostic interfaces
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAiWorkflowServices(this IServiceCollection services)
    {
        // Register only AI-powered services (cleaner approach)
        services.AddScoped<IWorkflowEngine, AiWorkflowEngine>();
        services.AddScoped<IVariableResolver, VariableResolver>();
        
        return services;
    }
}