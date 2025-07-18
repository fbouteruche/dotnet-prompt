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
    private readonly IResumeStateManager? _resumeStateManager;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;
    private readonly Dictionary<string, ChatHistory> _conversationStore = new();
    private Kernel? _kernel;

    public SemanticKernelOrchestrator(
        IKernelFactory kernelFactory,
        IPromptTemplateFactory handlebarsFactory,
        ILogger<SemanticKernelOrchestrator> logger,
        IResumeStateManager? resumeStateManager = null)
    {
        _kernelFactory = kernelFactory;
        _handlebarsFactory = handlebarsFactory;
        _resumeStateManager = resumeStateManager;
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
            
            // Extract provider and configuration from context
            Dictionary<string, object>? providerConfig = null;
            string? providerName = null;
            
            if (context.Configuration != null)
            {
                providerName = context.Configuration.DefaultProvider;
                var model = context.Configuration.DefaultModel;
                
                _logger.LogInformation("Using configuration from context: Provider={Provider}, Model={Model}", 
                    providerName, model);
                
                // Build provider configuration from resolved configuration
                if (!string.IsNullOrEmpty(model))
                {
                    providerConfig = new Dictionary<string, object> { ["Model"] = model };
                    
                    // Add provider-specific configuration if available
                    if (!string.IsNullOrEmpty(providerName) && 
                        context.Configuration.Providers.TryGetValue(providerName, out var provider))
                    {
                        if (!string.IsNullOrEmpty(provider.ApiKey))
                            providerConfig["ApiKey"] = provider.ApiKey;
                        if (!string.IsNullOrEmpty(provider.Token))
                            providerConfig["Token"] = provider.Token;
                        if (!string.IsNullOrEmpty(provider.Endpoint))
                            providerConfig["Endpoint"] = provider.Endpoint;
                        if (!string.IsNullOrEmpty(provider.BaseUrl))
                            providerConfig["BaseUrl"] = provider.BaseUrl;
                    }
                }
            }
            else
            {
                _logger.LogWarning("No configuration found in context - kernel creation may fail without explicit model");
            }
            
            // 1. Get or create kernel with required plugins and MCP servers from workflow
            _kernel ??= await _kernelFactory.CreateKernelWithWorkflowAsync(workflow, null, providerName, providerConfig);

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

            // 7. Setup resume state tracking if available
            if (_resumeStateManager != null)
            {
                // Store workflow hash for compatibility validation
                var workflowHash = ComputeWorkflowHash(workflow.Content?.RawMarkdown ?? string.Empty);
                context.SetVariable("workflow_id", workflowId);
                context.SetVariable("workflow_hash", workflowHash);
                context.SetVariable("workflow_file", workflow.Name ?? "unknown");
                context.SetVariable("original_content", workflow.Content?.RawMarkdown ?? "");
                
                // Save initial resume state
                await _resumeStateManager.SaveResumeStateAsync(workflowId, context, chatHistory, cancellationToken);
                _logger.LogDebug("Initial resume state saved for workflow {WorkflowId}", workflowId);
            }

            _logger.LogInformation("Executing workflow with SK Handlebars templating and automatic function calling");
            
            // 8. Execute with SK's automatic function calling and Handlebars rendering
            // Pass execution settings via KernelArguments for proper function calling behavior
            kernelArgs.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
            {
                { PromptExecutionSettings.DefaultServiceId, executionSettings }
            };
            
            var result = await workflowFunction.InvokeAsync(_kernel, kernelArgs, cancellationToken);

            // 9. Save conversation state for resume
            await SaveChatHistoryAsync(workflowId, chatHistory);

            // 10. Save final resume state if available
            if (_resumeStateManager != null)
            {
                await _resumeStateManager.SaveResumeStateAsync(workflowId, context, chatHistory, cancellationToken);
                _logger.LogDebug("Final resume state saved for workflow {WorkflowId}", workflowId);
            }

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
                    
                    // Extract configuration for validation (same as execution)
                    Dictionary<string, object>? providerConfig = null;
                    string? validationProviderName = null;
                    
                    if (context.Configuration != null)
                    {
                        validationProviderName = context.Configuration.DefaultProvider;
                        var model = context.Configuration.DefaultModel;
                        
                        if (!string.IsNullOrEmpty(model))
                        {
                            providerConfig = new Dictionary<string, object> { ["Model"] = model };
                            
                            // Add provider-specific configuration if available
                            if (!string.IsNullOrEmpty(validationProviderName) && 
                                context.Configuration.Providers.TryGetValue(validationProviderName, out var provider))
                            {
                                if (!string.IsNullOrEmpty(provider.ApiKey))
                                    providerConfig["ApiKey"] = provider.ApiKey;
                                if (!string.IsNullOrEmpty(provider.Token))
                                    providerConfig["Token"] = provider.Token;
                                if (!string.IsNullOrEmpty(provider.Endpoint))
                                    providerConfig["Endpoint"] = provider.Endpoint;
                                if (!string.IsNullOrEmpty(provider.BaseUrl))
                                    providerConfig["BaseUrl"] = provider.BaseUrl;
                            }
                        }
                    }
                    
                    // Test render to validate variable references
                    await template.RenderAsync(_kernel ?? await _kernelFactory.CreateKernelWithWorkflowAsync(workflow, null, validationProviderName, providerConfig), kernelArgs, cancellationToken);
                    
                    _logger.LogDebug("SK Handlebars template validation passed");
                }
                catch (Exception ex)
                {
                    errors.Add($"Handlebars template validation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SK Handlebars template validation failed");
                }
            }

            // Validate provider configuration exists
            var providerName = context.Configuration?.DefaultProvider ?? ExtractProviderFromModel(workflow.Model) ?? "openai";
            var modelName = context.Configuration?.DefaultModel ?? workflow.Model;
            
            if (string.IsNullOrEmpty(modelName))
            {
                errors.Add("No model specified in workflow frontmatter or configuration. Please specify a model using 'model: \"model-name\"' in the workflow frontmatter.");
            }
            
            if (!IsProviderConfigured(providerName))
            {
                warnings.Add($"AI provider '{providerName}' may not be properly configured. Ensure required environment variables or configuration are set before execution.");
            }

            // Advanced validation with kernel (if provider is configured)
            if (context.RequireAdvancedValidation && IsProviderConfigured(providerName))
            {
                try
                {
                    // Extract configuration for advanced validation (same as execution)
                    Dictionary<string, object>? advancedProviderConfig = null;
                    string? advancedProviderName = null;
                    
                    if (context.Configuration != null)
                    {
                        advancedProviderName = context.Configuration.DefaultProvider;
                        var model = context.Configuration.DefaultModel;
                        
                        if (!string.IsNullOrEmpty(model))
                        {
                            advancedProviderConfig = new Dictionary<string, object> { ["Model"] = model };
                            
                            // Add provider-specific configuration if available
                            if (!string.IsNullOrEmpty(advancedProviderName) && 
                                context.Configuration.Providers.TryGetValue(advancedProviderName, out var provider))
                            {
                                if (!string.IsNullOrEmpty(provider.ApiKey))
                                    advancedProviderConfig["ApiKey"] = provider.ApiKey;
                                if (!string.IsNullOrEmpty(provider.Token))
                                    advancedProviderConfig["Token"] = provider.Token;
                                if (!string.IsNullOrEmpty(provider.Endpoint))
                                    advancedProviderConfig["Endpoint"] = provider.Endpoint;
                                if (!string.IsNullOrEmpty(provider.BaseUrl))
                                    advancedProviderConfig["BaseUrl"] = provider.BaseUrl;
                            }
                        }
                    }
                    
                    _kernel ??= await _kernelFactory.CreateKernelWithWorkflowAsync(workflow, null, advancedProviderName, advancedProviderConfig);
                    
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

    public async Task<WorkflowExecutionResult> ResumeWorkflowAsync(string workflowId, DotpromptWorkflow workflow, CancellationToken cancellationToken = default)
    {
        var executionStopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Resuming SK-native workflow execution: {WorkflowId}", workflowId);

            if (_resumeStateManager == null)
            {
                throw new InvalidOperationException("Resume state manager is required for workflow resume functionality");
            }

            // 1. Load resume state from resume state manager
            var resumeData = await _resumeStateManager.LoadResumeStateAsync(workflowId, cancellationToken);
            if (resumeData == null)
            {
                throw new InvalidOperationException($"No resume state found for workflow {workflowId}");
            }

            var (restoredContext, restoredChatHistory) = resumeData.Value;
            if (restoredContext == null || restoredChatHistory == null)
            {
                throw new InvalidOperationException($"Invalid resume state for workflow {workflowId}");
            }

            // 2. Validate workflow compatibility
            var currentWorkflowContent = workflow.Content?.RawMarkdown ?? string.Empty;
            var compatibility = await _resumeStateManager.ValidateResumeCompatibilityAsync(workflowId, currentWorkflowContent, cancellationToken);
            
            if (!compatibility.CanResume)
            {
                var warnings = string.Join(", ", compatibility.Warnings);
                throw new InvalidOperationException($"Workflow cannot be resumed due to compatibility issues: {warnings}");
            }

            // 3. Get or create kernel with required plugins and MCP servers from workflow
            // Extract provider and configuration from restored context
            Dictionary<string, object>? providerConfig = null;
            string? providerName = null;
            
            if (restoredContext.Configuration != null)
            {
                providerName = restoredContext.Configuration.DefaultProvider;
                var model = restoredContext.Configuration.DefaultModel;
                
                if (!string.IsNullOrEmpty(model))
                {
                    providerConfig = new Dictionary<string, object> { ["Model"] = model };
                    
                    // Add provider-specific configuration if available
                    if (!string.IsNullOrEmpty(providerName) && 
                        restoredContext.Configuration.Providers.TryGetValue(providerName, out var provider))
                    {
                        if (!string.IsNullOrEmpty(provider.ApiKey))
                            providerConfig["ApiKey"] = provider.ApiKey;
                        if (!string.IsNullOrEmpty(provider.Token))
                            providerConfig["Token"] = provider.Token;
                        if (!string.IsNullOrEmpty(provider.Endpoint))
                            providerConfig["Endpoint"] = provider.Endpoint;
                        if (!string.IsNullOrEmpty(provider.BaseUrl))
                            providerConfig["BaseUrl"] = provider.BaseUrl;
                    }
                }
            }
            
            _kernel ??= await _kernelFactory.CreateKernelWithWorkflowAsync(workflow, null, providerName, providerConfig);

            // 4. Restore conversation state
            _conversationStore[workflowId] = restoredChatHistory;
            
            // 5. Add resume message to chat history
            restoredChatHistory.AddSystemMessage($"Resuming workflow execution from step {restoredContext.CurrentStep} at {DateTimeOffset.UtcNow}");

            // 6. Create SK Handlebars template configuration
            var promptConfig = new PromptTemplateConfig
            {
                Template = currentWorkflowContent,
                TemplateFormat = "handlebars",
                Name = workflow.Name ?? "workflow",
                Description = workflow.Metadata?.Description ?? "Resumed workflow execution"
            };

            // 7. Convert restored context variables to KernelArguments for SK
            var kernelArgs = ConvertToKernelArguments(restoredContext);

            // 8. Create function from template using SK's Handlebars factory
            var workflowFunction = _kernel.CreateFunctionFromPrompt(promptConfig, _handlebarsFactory);

            // 9. Configure execution settings
            var executionSettings = CreateExecutionSettings(workflow);

            _logger.LogInformation("Resuming workflow execution with restored state");
            
            // 10. Execute with SK's automatic function calling and Handlebars rendering
            // Pass execution settings via KernelArguments for proper function calling behavior
            kernelArgs.ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
            {
                { PromptExecutionSettings.DefaultServiceId, executionSettings }
            };
            
            var result = await workflowFunction.InvokeAsync(_kernel, kernelArgs, cancellationToken);

            // 11. Save conversation state and resume state
            await SaveChatHistoryAsync(workflowId, restoredChatHistory);
            await _resumeStateManager.SaveResumeStateAsync(workflowId, restoredContext, restoredChatHistory, cancellationToken);

            _logger.LogInformation("SK-native workflow resume completed successfully in {Duration}ms", 
                executionStopwatch.ElapsedMilliseconds);

            return new WorkflowExecutionResult(
                Success: true,
                Output: result.ToString(),
                ErrorMessage: null,
                ExecutionTime: executionStopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume SK-native workflow execution: {WorkflowId}", workflowId);
            
            return new WorkflowExecutionResult(
                Success: false,
                Output: null,
                ErrorMessage: ex.Message,
                ExecutionTime: executionStopwatch.Elapsed
            );
        }
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

    /// <summary>
    /// Compute hash of workflow content for compatibility validation
    /// </summary>
    private static string ComputeWorkflowHash(string workflowContent)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(workflowContent));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}