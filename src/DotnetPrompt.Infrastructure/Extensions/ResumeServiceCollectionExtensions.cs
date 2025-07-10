using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Resume;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering the unified workflow resume system services
/// </summary>
public static class ResumeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the unified workflow resume system services with dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWorkflowResumeSystem(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Core resume state services
        services.AddScoped<IResumeStateManager, FileResumeStateManager>();
        services.AddScoped<AIWorkflowResumeOptimization>();
        services.AddScoped<SKTelemetryResumeStateCapture>();

        // Configuration binding
        services.Configure<ResumeConfig>(config => configuration.GetSection("Resume").Bind(config));

        // Register SK filter for state capture
        services.AddSingleton<IFunctionInvocationFilter, SKTelemetryResumeStateCapture>();

        return services;
    }

    /// <summary>
    /// Registers the unified workflow resume system services with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureResume">Resume configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWorkflowResumeSystem(
        this IServiceCollection services,
        Action<ResumeConfig> configureResume)
    {
        // Core resume state services
        services.AddScoped<IResumeStateManager, FileResumeStateManager>();
        services.AddScoped<AIWorkflowResumeOptimization>();
        services.AddScoped<SKTelemetryResumeStateCapture>();

        // Configuration
        services.Configure(configureResume);

        // Register SK filter for state capture
        services.AddSingleton<IFunctionInvocationFilter, SKTelemetryResumeStateCapture>();

        return services;
    }

    /// <summary>
    /// Registers the unified workflow resume system services with default configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWorkflowResumeSystemWithDefaults(
        this IServiceCollection services)
    {
        return services.AddWorkflowResumeSystem(config =>
        {
            config.StorageLocation = "./.dotnet-prompt/resume";
            config.RetentionDays = 7;
            config.EnableCompression = false;
            config.MaxFileSizeBytes = 1024 * 1024; // 1MB
            config.CheckpointFrequency = 1; // After each tool execution
            config.EnableAtomicWrites = true;
            config.EnableBackup = true;
        });
    }
}