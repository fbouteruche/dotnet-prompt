using DotnetPrompt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetPrompt.Application;

/// <summary>
/// Extension methods for registering application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services with Semantic Kernel-based orchestration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application services rely on IWorkflowOrchestrator (implementation provided by Infrastructure layer)
        // No need to register custom workflow engines - SK handles all orchestration
        
        return services;
    }
}