using System.Diagnostics;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Extensions;

/// <summary>
/// Extension methods for integrating Serilog with SK-enhanced logging
/// </summary>
public static class SemanticKernelLoggingExtensions
{
    /// <summary>
    /// Configure Serilog with SK-optimized settings and correlation ID enrichment
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSemanticKernelLogging(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Get logging configuration
        var loggingConfig = configuration.GetSection("Logging").Get<LoggingConfiguration>() ?? new LoggingConfiguration();
        
        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("Application", "dotnet-prompt")
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithCorrelationId()
            .Enrich.WithSemanticKernelContext();

        // Configure log level
        var logLevel = ParseLogLevel(loggingConfig.Level);
        loggerConfig.MinimumLevel.Is(logLevel);

        // Configure console output
        if (loggingConfig.Console ?? true)
        {
            if (loggingConfig.Structured ?? false)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
            }
            else
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
            }
        }

        // Configure file output
        if (!string.IsNullOrEmpty(loggingConfig.File))
        {
            var logFilePath = Path.IsPathRooted(loggingConfig.File) 
                ? loggingConfig.File 
                : Path.Combine(Directory.GetCurrentDirectory(), loggingConfig.File);
            
            loggerConfig.WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
        }

        // Create and configure the logger
        var logger = loggerConfig.CreateLogger();
        Log.Logger = logger;

        // Add Serilog to the service collection
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Add correlation ID enrichment to Serilog
    /// </summary>
    private static LoggerConfiguration EnrichWith(this LoggerConfiguration loggerConfig, ILogEventEnricher enricher)
    {
        return loggerConfig.Enrich.With(enricher);
    }

    /// <summary>
    /// Add correlation ID enrichment using System.Diagnostics.Activity
    /// </summary>
    private static LoggerConfiguration WithCorrelationId(this LoggerEnrichmentConfiguration enrichmentConfig)
    {
        return enrichmentConfig.With<CorrelationIdEnricher>();
    }

    /// <summary>
    /// Add SK-specific context enrichment
    /// </summary>
    private static LoggerConfiguration WithSemanticKernelContext(this LoggerEnrichmentConfiguration enrichmentConfig)
    {
        return enrichmentConfig.With<SemanticKernelContextEnricher>();
    }

    /// <summary>
    /// Parse log level from configuration string
    /// </summary>
    private static LogEventLevel ParseLogLevel(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "trace" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}

/// <summary>
/// Serilog enricher for correlation IDs using System.Diagnostics.Activity
/// </summary>
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}

/// <summary>
/// Serilog enricher for SK-specific context information
/// </summary>
public class SemanticKernelContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Add SK-specific context if available
        var activity = Activity.Current;
        if (activity != null)
        {
            // Add any SK-specific tags from the activity
            foreach (var tag in activity.Tags)
            {
                if (tag.Key.StartsWith("sk."))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(tag.Key, tag.Value));
                }
            }
        }
    }
}