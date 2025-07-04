using System.Diagnostics;
using System.Text.Json;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel-powered workflow orchestrator that leverages SK's function calling and conversation management
/// This is the SK-specific implementation of the framework-agnostic IWorkflowOrchestrator interface
/// </summary>
public class SemanticKernelOrchestrator : IWorkflowOrchestrator
{
    private readonly IKernelFactory _kernelFactory;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;
    private readonly Dictionary<string, ChatHistory> _conversationStore = new();
    private Kernel? _kernel;

    public SemanticKernelOrchestrator(
        IKernelFactory kernelFactory,
        ILogger<SemanticKernelOrchestrator> logger)
    {
        _kernelFactory = kernelFactory;
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
            _logger.LogInformation("Starting SK-powered workflow execution: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // Get or create kernel with required plugins
            _kernel ??= await _kernelFactory.CreateKernelAsync();

            // Load conversation history for resume capability
            var workflowId = $"workflow_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var chatHistory = await GetChatHistoryAsync(workflowId);

            // Execute workflow using SK's automatic function calling
            var executionSettings = CreateExecutionSettings(workflow, context);
            
            // Prepare the workflow prompt with context
            var workflowPrompt = PrepareWorkflowPrompt(workflow, context);
            
            // Add the workflow execution request to chat history
            chatHistory.AddUserMessage(workflowPrompt);

            _logger.LogInformation("Executing workflow with SK function calling for {StepCount} logical steps", 
                ExtractStepCount(workflow));

            // Use SK's chat completion with automatic function calling
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory, 
                executionSettings, 
                _kernel, 
                cancellationToken);

            // Add AI response to chat history
            chatHistory.Add(response);

            // Save conversation state for resume capability
            await SaveChatHistoryAsync(workflowId, chatHistory);

            _logger.LogInformation("SK workflow execution completed successfully in {Duration}ms", 
                executionStopwatch.ElapsedMilliseconds);

            return new WorkflowExecutionResult(
                Success: true,
                Output: response.Content ?? "Workflow completed successfully",
                ErrorMessage: null,
                ExecutionTime: executionStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK workflow execution for {WorkflowName}", workflow.Name);
            
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
            _logger.LogInformation("Validating workflow with SK: {WorkflowName}", workflow.Name ?? "unnamed");

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate workflow structure (no AI provider needed)
            if (string.IsNullOrEmpty(workflow.Content?.RawMarkdown))
            {
                errors.Add("Workflow content cannot be empty");
            }

            // Validate frontmatter configuration
            if (string.IsNullOrEmpty(workflow.Model))
            {
                warnings.Add("No model specified in frontmatter, will use default");
            }

            // Validate provider configuration exists (but don't initialize it)
            var providerName = ExtractProviderFromModel(workflow.Model) ?? "openai";
            if (!IsProviderConfigured(providerName))
            {
                warnings.Add($"AI provider '{providerName}' may not be properly configured. Ensure required environment variables or configuration are set before execution.");
            }

            // Only create kernel if we need advanced validation and have a provider configured
            if (context.RequireAdvancedValidation && IsProviderConfigured(providerName))
            {
                try
                {
                    _kernel ??= await _kernelFactory.CreateKernelAsync();
                    
                    // Validate available functions in kernel
                    var availableFunctions = _kernel.Plugins.GetFunctionsMetadata();
                    _logger.LogDebug("Available SK functions: {FunctionCount}", availableFunctions.Count());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not initialize AI provider for advanced validation");
                    warnings.Add($"Could not validate AI provider connectivity: {ex.Message}");
                }
            }

            // Validate variable references (delegated to existing variable resolver)
            var workflowPrompt = PrepareWorkflowPrompt(workflow, context);
            // Additional SK-specific validation could be added here

            var isValid = errors.Count == 0;
            
            _logger.LogInformation("SK workflow validation completed: {IsValid}", isValid);

            return new WorkflowValidationResult(
                IsValid: isValid,
                Errors: errors.ToArray(),
                Warnings: warnings.Count > 0 ? warnings.ToArray() : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK workflow validation");
            
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

    private PromptExecutionSettings CreateExecutionSettings(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), // Enable automatic function calling
            MaxTokens = workflow.Config?.MaxOutputTokens ?? 4000,
            Temperature = workflow.Config?.Temperature ?? 0.7
        };

        return settings;
    }

    private string PrepareWorkflowPrompt(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        var prompt = $"""
            Execute the following workflow step by step:

            Workflow Name: {workflow.Name ?? "Unnamed Workflow"}
            
            Context Variables:
            {string.Join("\n", context.Variables.Select(kv => $"- {kv.Key}: {kv.Value}"))}

            Workflow Content:
            {workflow.Content?.RawMarkdown}

            Please execute this workflow using the available functions. Use file operations for reading/writing files, 
            and ensure all variable substitutions are properly resolved.
            """;

        return prompt;
    }

    private static int ExtractStepCount(DotpromptWorkflow workflow)
    {
        // Simple heuristic - count logical steps mentioned in content
        return workflow.Content?.RawMarkdown?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.TrimStart().StartsWith("- ") || 
                          line.TrimStart().StartsWith("1.") ||
                          line.TrimStart().StartsWith("2.") ||
                          line.TrimStart().StartsWith("3.")) ?? 0;
    }

    private static int CountFunctionCalls(ChatHistory chatHistory)
    {
        // Count function calls in the conversation
        return chatHistory.Count(message => message.Role == AuthorRole.Tool);
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