using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace DotnetPrompt.IntegrationTests.Utilities;

/// <summary>
/// Shared mock implementations for integration tests
/// </summary>

public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

public class MockConfigurationService : IConfigurationService
{
    public Task<DotPromptConfiguration> LoadConfigurationAsync(
        string? globalConfigPath = null, 
        string? projectConfigPath = null,
        bool? loadGlobalConfig = null,
        string? cliProvider = null,
        string? frontmatterModel = null,
        string? frontmatterProvider = null,
        CancellationToken cancellationToken = default)
    {
        var config = new DotPromptConfiguration
        {
            DefaultProvider = "ollama",
            DefaultModel = "test-model",
            Providers = new Dictionary<string, ProviderConfiguration>
            {
                ["ollama"] = new ProviderConfiguration
                {
                    BaseUrl = "http://localhost:11434",
                    Token = "dummy-token"
                },
                ["local"] = new ProviderConfiguration
                {
                    Endpoint = "http://localhost:11434",
                    Token = "dummy-token"
                }
            }
        };
        
        return Task.FromResult(config);
    }

    public Task<ConfigurationValidationResult> ValidateConfigurationAsync(
        DotPromptConfiguration configuration, 
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConfigurationValidationResult { IsValid = true });
    }

    public ConfigurationValidationResult ValidateConfiguration(DotPromptConfiguration configuration)
    {
        return new ConfigurationValidationResult { IsValid = true };
    }

    public Task SaveConfigurationAsync(
        DotPromptConfiguration configuration, 
        bool isGlobal = false, 
        string? configPath = null, 
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public DotPromptConfiguration ResolveConfiguration(
        string? cliProvider = null, 
        string? frontmatterModel = null, 
        string? projectConfigPath = null, 
        string? globalConfigPath = null)
    {
        return new DotPromptConfiguration();
    }

    public string? GetConfigurationValue(string key, string? defaultValue = null)
    {
        return defaultValue;
    }

    public string GetGlobalConfigurationPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet-prompt", "config.yaml");
    }

    public string GetProjectConfigurationPath(string projectDirectory)
    {
        return Path.Combine(projectDirectory, ".dotnet-prompt.yaml");
    }
}

public class MockWorkflowExecutionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await next(context);
    }
}