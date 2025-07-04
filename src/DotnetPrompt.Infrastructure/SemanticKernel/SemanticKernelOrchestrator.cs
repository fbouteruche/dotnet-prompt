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
/// </summary>
public class SemanticKernelOrchestrator : ISemanticKernelOrchestrator
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
            _kernel ??= await _kernelFactory.CreateKernelAsync(
                context.Configuration?.Provider, 
                context.Configuration?.ProviderSettings);

            // Load conversation history for resume capability
            var chatHistory = await GetChatHistoryAsync(context.WorkflowId ?? Guid.NewGuid().ToString());

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
            await SaveChatHistoryAsync(context.WorkflowId ?? Guid.NewGuid().ToString(), chatHistory);

            _logger.LogInformation("SK workflow execution completed successfully in {Duration}ms", 
                executionStopwatch.ElapsedMilliseconds);

            return new WorkflowExecutionResult(
                Success: true,
                Result: response.Content ?? "Workflow completed successfully",
                ErrorMessage: null,
                ExecutionTime: executionStopwatch.Elapsed,
                Metadata: new Dictionary<string, object>
                {
                    { "sk_execution", true },
                    { "function_calls_count", CountFunctionCalls(chatHistory) },
                    { "conversation_length", chatHistory.Count },
                    { "provider", context.Configuration?.Provider ?? "default" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK workflow execution for {WorkflowName}", workflow.Name);
            
            return new WorkflowExecutionResult(
                Success: false,
                Result: null,
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

            // Get kernel for validation
            _kernel ??= await _kernelFactory.CreateKernelAsync(
                context.Configuration?.Provider, 
                context.Configuration?.ProviderSettings);

            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate workflow structure
            if (string.IsNullOrEmpty(workflow.Content))
            {
                errors.Add("Workflow content cannot be empty");
            }

            // Validate frontmatter configuration
            if (workflow.Frontmatter?.ContainsKey("model") != true)
            {
                warnings.Add("No model specified in frontmatter, will use default");
            }

            // Validate available functions in kernel
            var availableFunctions = _kernel.Plugins.GetFunctionsMetadata();
            _logger.LogDebug("Available SK functions: {FunctionCount}", availableFunctions.Count());

            // Validate variable references (delegated to existing variable resolver)
            var workflowPrompt = PrepareWorkflowPrompt(workflow, context);
            // Additional SK-specific validation could be added here

            var isValid = errors.Count == 0;
            
            _logger.LogInformation("SK workflow validation completed: {IsValid}", isValid);

            return new WorkflowValidationResult(
                IsValid: isValid,
                Errors: errors.ToArray(),
                Warnings: warnings.Count > 0 ? warnings.ToArray() : null,
                Metadata: new Dictionary<string, object>
                {
                    { "sk_validation", true },
                    { "available_functions", availableFunctions.Count() },
                    { "provider", context.Configuration?.Provider ?? "default" }
                });
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

    public async Task<ChatHistory> GetChatHistoryAsync(string workflowId)
    {
        if (_conversationStore.TryGetValue(workflowId, out var history))
        {
            return history;
        }

        // Return new chat history if not found
        return new ChatHistory();
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
            MaxTokens = GetConfigValue<int?>(workflow.Frontmatter, "max_tokens") ?? 4000,
            Temperature = GetConfigValue<double?>(workflow.Frontmatter, "temperature") ?? 0.7
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
            {workflow.Content}

            Please execute this workflow using the available functions. Use file operations for reading/writing files, 
            and ensure all variable substitutions are properly resolved.
            """;

        return prompt;
    }

    private static int ExtractStepCount(DotpromptWorkflow workflow)
    {
        // Simple heuristic - count logical steps mentioned in content
        return workflow.Content?.Split('\n')
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

    private static T? GetConfigValue<T>(Dictionary<string, object>? frontmatter, string key)
    {
        if (frontmatter?.TryGetValue(key, out var value) == true)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
}