using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure.Configuration;
using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Infrastructure.Filters;
using DotnetPrompt.Infrastructure.Mcp;
using DotnetPrompt.Infrastructure.Middleware;
using DotnetPrompt.Infrastructure.Models;
using DotnetPrompt.Infrastructure.Resume;
using DotnetPrompt.Infrastructure.SemanticKernel;
using DotnetPrompt.Infrastructure.SemanticKernel.Plugins;
using DotnetPrompt.Infrastructure.Analysis;
using DotnetPrompt.Infrastructure.Analysis.Compilation;
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
        
        // Add file system options configuration
        services.Configure<FileSystemOptions>(options =>
        {
            // Default configuration - will be overridden by appsettings.json or CLI parameters
            options.AllowedDirectories = Array.Empty<string>(); // Default to working directory
            options.BlockedDirectories = new[] { "bin", "obj", ".git", "node_modules" };
            options.AllowedExtensions = Array.Empty<string>(); // Allow all by default
            options.BlockedExtensions = new[] { ".exe", ".dll", ".so", ".dylib" };
            options.MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
            options.MaxFilesPerOperation = 1000;
            options.RequireConfirmationForDelete = true;
            options.EnableAuditLogging = true;
        });
        
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
        
        // Register file system security filter
        services.AddSingleton<IFunctionInvocationFilter, FileSystemSecurityFilter>();

        // Register resume state tracking filter instead of progress tracking
        services.AddSingleton<IFunctionInvocationFilter, SKTelemetryResumeStateCapture>();

        // Register middleware (as additional filters)
        services.AddSingleton<IFunctionInvocationFilter, RetryMiddleware>();
        services.AddSingleton<IFunctionInvocationFilter, CircuitBreakerMiddleware>();

        // Register options for middleware
        services.AddSingleton<RetryOptions>();
        services.AddSingleton<CircuitBreakerOptions>();

        return services;
    }

    /// <summary>
    /// Adds Semantic Kernel orchestrator services with native Handlebars templating and MCP integration
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
        
        // Register kernel factory with MCP support
        services.AddSingleton<IKernelFactory, KernelFactory>();
        
        // Add MCP integration services
        services.AddMcpIntegrationServices();
        
        return services;
    }

    /// <summary>
    /// Adds MCP (Model Context Protocol) integration services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpIntegrationServices(this IServiceCollection services)
    {
        // Core MCP services using official SDK
        services.AddSingleton<McpConnectionTypeDetector>();
        services.AddSingleton<McpServerResolver>();
        services.AddSingleton<McpConfigurationService>();
        // Note: With official MCP SDK, McpClientFactory.CreateAsync() is used directly
        
        // MCP execution filter for error handling
        services.AddSingleton<McpExecutionFilter>();
        
        return services;
    }

    /// <summary>
    /// Adds workflow resume state tracking services using file-based storage with Semantic Kernel
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddResumeStateServices(this IServiceCollection services)
    {
        // Use the comprehensive resume system with default configuration
        services.AddWorkflowResumeSystemWithDefaults();
        
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
        
        // Add Roslyn analysis services for ProjectAnalysisPlugin
        services.AddRoslynAnalysisServices();
        
        // Register essential SK plugins (excluding WorkflowExecutorPlugin which is replaced by SK native capabilities)
        services.AddTransient<FileSystemPlugin>();
        services.AddTransient<ProjectAnalysisPlugin>();
        services.AddTransient<SubWorkflowPlugin>();
        // NOTE: WorkflowExecutorPlugin is intentionally excluded - replaced by SK Handlebars templating
        
        return services;
    }

    /// <summary>
    /// Adds Roslyn analysis services for comprehensive project analysis
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRoslynAnalysisServices(this IServiceCollection services)
    {
        // Initialize MSBuild setup early in the DI registration process
        // This ensures MSBuild Locator is ready before any services are created
        MSBuildSetup.EnsureInitialized();
        
        // Core Roslyn analysis service using Core interface
        services.AddScoped<IRoslynAnalysisService, RoslynAnalysisService>();
        
        // MSBuild diagnostics handler for error processing
        services.AddScoped<MSBuildDiagnosticsHandler>();
        
        // Compilation strategies with MSBuild integration
        services.AddScoped<MSBuildWorkspaceStrategy>();
        services.AddScoped<CustomCompilationStrategy>();
        
        // Strategy factory for selecting optimal compilation approach
        services.AddScoped<ICompilationStrategyFactory, CompilationStrategyFactory>();
        
        // Analysis engines (to be implemented in future phases)
        // services.AddScoped<SemanticAnalysisEngine>();
        // services.AddScoped<MetricsAnalysisEngine>();
        // services.AddScoped<PatternDetectionEngine>();
        // services.AddScoped<SecurityAnalysisEngine>();
        
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
        services.AddResumeStateServices();
        
        return services;
    }
}