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
        // Register the workflow engine
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        
        // Register variable resolver
        services.AddScoped<IVariableResolver, VariableResolver>();
        
        // Register step executors
        services.AddScoped<IStepExecutor, PromptStepExecutor>();
        services.AddScoped<IStepExecutor, FileReadStepExecutor>();
        services.AddScoped<IStepExecutor, FileWriteStepExecutor>();
        
        return services;
    }
}