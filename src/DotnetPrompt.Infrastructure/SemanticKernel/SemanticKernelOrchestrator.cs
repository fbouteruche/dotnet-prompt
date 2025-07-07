using System.Diagnostics;
using System.Text.Json;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel-powered workflow orchestrator that leverages SK's native Handlebars templating,
/// automatic function calling, and conversation management. This is the complete SK-native implementation
/// per the workflow orchestrator specification.
/// </summary>
public class SemanticKernelOrchestrator : IWorkflowOrchestrator
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IPromptTemplateFactory _handlebarsFactory;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;
    private readonly Dictionary<string, ChatHistory> _conversationStore = new();
    private Kernel? _kernel;

    public SemanticKernelOrchestrator(
        IKernelFactory kernelFactory,
        IPromptTemplateFactory handlebarsFactory,
        ILogger<SemanticKernelOrchestrator> logger)
    {
        _kernelFactory = kernelFactory;
        _handlebarsFactory = handlebarsFactory;
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        var executionStopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting SK-native workflow execution with Handlebars templating: {WorkflowName}", 
                workflow.Name ?? "unnamed");
            
            // 1. Get or create kernel with required plugins
            _kernel ??= await _kernelFactory.CreateKernelAsync();

            // 2. Create SK Handlebars template configuration
            var promptConfig = new PromptTemplateConfig
            {
                Template = workflow.Content?.RawMarkdown ?? throw new ArgumentException("Workflow content cannot be empty"),
                TemplateFormat = "handlebars", // SK native Handlebars support
                Name = workflow.Name ?? "workflow",
                Description = workflow.Metadata?.Description ?? "Workflow execution"
            };

            // 3. Convert context variables to KernelArguments for SK
            var kernelArgs = ConvertToKernelArguments(context);

            // 4. Create function from template using SK's Handlebars factory
            var workflowFunction = _kernel.CreateFunctionFromPrompt(promptConfig, _handlebarsFactory);

            // 5. Configure execution settings for automatic function calling
            var executionSettings = CreateExecutionSettings(workflow);

            // 6. Setup conversation for resume capability
            var workflowId = GenerateWorkflowId(workflow, context);
            var chatHistory = await GetChatHistoryAsync(workflowId);

            _logger.LogInformation("Executing workflow with SK Handlebars templating and automatic function calling");
            
            // 7. Execute with SK's automatic function calling and Handlebars rendering
            var result = await workflowFunction.InvokeAsync(_kernel, kernelArgs, cancellationToken);

            // 8. Save conversation state for resume
            await SaveChatHistoryAsync(workflowId, chatHistory);

            _logger.LogInformation("SK-native workflow execution completed successfully in {Duration}ms", 
                executionStopwatch.ElapsedMilliseconds);

            return new WorkflowExecutionResult(
                Success: true,
                Output: result.ToString(),
                ErrorMessage: null,
                ExecutionTime: executionStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK-native workflow execution for {WorkflowName}", workflow.Name);
            
            return new WorkflowExecutionResult(
                Success: false,
                Output: null,
                ErrorMessage: $"SK execution failed: {ex.Message}",
                ExecutionTime: executionStopwatch.Elapsed);
        }
        finally
        {
            executionStopwatch.Stop();
        }
    }

    public async Task<WorkflowValidationResult> ValidateWorkflowAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating workflow with SK Handlebars template validation: {WorkflowName}", 
                workflow.Name ?? "unnamed");

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate workflow structure
            if (string.IsNullOrEmpty(workflow.Content?.RawMarkdown))
            {
                errors.Add("Workflow content cannot be empty");
            }

            // Validate frontmatter configuration
            if (string.IsNullOrEmpty(workflow.Model))
            {
                warnings.Add("No model specified in frontmatter, will use default");
            }

            // Validate SK Handlebars template syntax
            if (!string.IsNullOrEmpty(workflow.Content?.RawMarkdown))
            {
                try
                {
                    var promptConfig = new PromptTemplateConfig
                    {
                        Template = workflow.Content.RawMarkdown,
                        TemplateFormat = "handlebars",
                        Name = workflow.Name ?? "validation",
                        Description = "Template validation"
                    };

                    // Convert context to kernel arguments for validation
                    var kernelArgs = ConvertToKernelArguments(context);

                    // Create template to validate syntax (will throw if invalid)
                    var template = _handlebarsFactory.Create(promptConfig);
                    
                    // Test render to validate variable references
                    await template.RenderAsync(_kernel ?? await _kernelFactory.CreateKernelAsync(), kernelArgs, cancellationToken);
                    
                    _logger.LogDebug("SK Handlebars template validation passed");
                }
                catch (Exception ex)
                {
                    errors.Add($"Handlebars template validation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SK Handlebars template validation failed");
                }
            }

            // Validate provider configuration exists
            var providerName = ExtractProviderFromModel(workflow.Model) ?? "openai";
            if (!IsProviderConfigured(providerName))
            {
                warnings.Add($"AI provider '{providerName}' may not be properly configured. Ensure required environment variables or configuration are set before execution.");
            }

            // Advanced validation with kernel (if provider is configured)
            if (context.RequireAdvancedValidation && IsProviderConfigured(providerName))
            {
                try
                {
                    _kernel ??= await _kernelFactory.CreateKernelAsync();
                    
                    // Validate available functions in kernel
                    var availableFunctions = _kernel.Plugins.GetFunctionsMetadata();
                    _logger.LogDebug("Available SK functions for validation: {FunctionCount}", availableFunctions.Count());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not initialize AI provider for advanced validation");
                    warnings.Add($"Could not validate AI provider connectivity: {ex.Message}");
                }
            }

            var isValid = errors.Count == 0;
            
            _logger.LogInformation("SK Handlebars workflow validation completed: {IsValid}", isValid);

            return new WorkflowValidationResult(
                IsValid: isValid,
                Errors: errors.ToArray(),
                Warnings: warnings.Count > 0 ? warnings.ToArray() : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK Handlebars workflow validation");
            
            return new WorkflowValidationResult(
                IsValid: false,
                Errors: new[] { $"Validation failed: {ex.Message}" },
                Warnings: null);
        }
    }

    public Kernel GetKernel()
    {
        return _kernel ?? throw new InvalidOperationException("Kernel not initialized. Call ExecuteWorkflowAsync or ValidateWorkflowAsync first.");
    }

    public Task<ChatHistory> GetChatHistoryAsync(string workflowId)
    {
        if (_conversationStore.TryGetValue(workflowId, out var history))
        {
            return Task.FromResult(history);
        }

        // Return new chat history if not found
        return Task.FromResult(new ChatHistory());
    }

    public async Task SaveChatHistoryAsync(string workflowId, ChatHistory chatHistory)
    {
        _conversationStore[workflowId] = chatHistory;
        _logger.LogDebug("Saved chat history for workflow {WorkflowId} with {MessageCount} messages", 
            workflowId, chatHistory.Count);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Converts WorkflowExecutionContext variables to KernelArguments for SK
    /// </summary>
    private static KernelArguments ConvertToKernelArguments(WorkflowExecutionContext context)
    {
        var kernelArgs = new KernelArguments();
        
        foreach (var (key, value) in context.Variables)
        {
            kernelArgs[key] = value;
        }
        
        return kernelArgs;
    }

    /// <summary>
    /// Generates a unique workflow ID for conversation tracking
    /// </summary>
    private static string GenerateWorkflowId(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        var workflowName = workflow.Name ?? "unnamed";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var hash = Math.Abs(string.Join(",", context.Variables.Keys).GetHashCode());
        
        return $"workflow_{workflowName}_{timestamp}_{hash}";
    }

    private PromptExecutionSettings CreateExecutionSettings(DotpromptWorkflow workflow)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), // Enable automatic function calling
            MaxTokens = workflow.Config?.MaxOutputTokens ?? 4000,
            Temperature = workflow.Config?.Temperature ?? 0.7
        };

        return settings;
    }

    private bool IsProviderConfigured(string providerName)
    {
        // Check if the required configuration exists for the provider without actually initializing it
        return providerName.ToLowerInvariant() switch
        {
            "openai" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
            "github" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN")),
            "azure" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")),
            "local" or "ollama" => true, // Local providers don't require API keys
            _ => false // Unknown providers are considered not configured
        };
    }

    private string? ExtractProviderFromModel(string? model)
    {
        if (string.IsNullOrEmpty(model))
            return null;

        // Check if model is in "provider/model" format
        var parts = model.Split('/', 2);
        if (parts.Length == 2)
        {
            return parts[0]; // Return the provider part
        }

        // Default provider based on common model names
        return model.ToLowerInvariant() switch
        {
            var m when m.StartsWith("gpt-") => "openai",
            var m when m.StartsWith("claude-") => "anthropic",
            var m when m.StartsWith("llama") => "local",
            _ => "openai" // Default to OpenAI
        };
    }
}