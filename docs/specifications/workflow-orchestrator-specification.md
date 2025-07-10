# Workflow Orchestrator Specification (Semantic Kernel Implementation)

## Overview

This document defines the `IWorkflow            // 4. Convert context variables to KernelArguments
            var kernelArgs = ConvertToKernelArguments(context);

            // 5. Create function from template using SK's Handlebars factory
            var workflowFunction = _kernel.CreateFunctionFromPrompt(promptConfig, _handlebarsFactory);

            // 6. Configure execution settings
            var executionSettings = CreateExecutionSettings(workflow);

            // 7. Setup conversation for resume capability
            var workflowId = GenerateWorkflowId(workflow, context);
            var chatHistory = await GetChatHistoryAsync(workflowId);

            // 8. Execute with SK's automatic function calling and Handlebars rendering
            _logger.LogInformation("Executing workflow with SK Handlebars templating and function calling");nterface and its Semantic Kernel-based implementation (`SemanticKernelOrchestrator`), which serves as the core workflow execution engine leveraging SK's native Handlebars templating, automatic function calling, and conversation state management.

## Status
✅ **COMPLETE** - SK-native orchestrator architecture defined

## Core Architecture

### IWorkflowOrchestrator Interface

```csharp
namespace DotnetPrompt.Core.Interfaces;

/// <summary>
/// Core interface for workflow orchestration and execution
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes a workflow with the provided context
    /// </summary>
    /// <param name="workflow">Parsed workflow to execute</param>
    /// <param name="context">Execution context with variables and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow execution result</returns>
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow without executing it
    /// </summary>
    /// <param name="workflow">Workflow to validate</param>
    /// <param name="context">Context for validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying Semantic Kernel instance for advanced operations
    /// </summary>
    /// <returns>Configured Kernel instance</returns>
    Kernel GetKernel();

    /// <summary>
    /// Retrieves conversation history for a workflow execution
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <returns>Chat history for the workflow</returns>
    Task<ChatHistory> GetChatHistoryAsync(string workflowId);

    /// <summary>
    /// Saves conversation history for resume functionality
    /// </summary>
    /// <param name="workflowId">Unique workflow execution identifier</param>
    /// <param name="chatHistory">Chat history to save</param>
    Task SaveChatHistoryAsync(string workflowId, ChatHistory chatHistory);
}
```

## Semantic Kernel Implementation

### SemanticKernelOrchestrator Class

```csharp
namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel-based workflow orchestrator that leverages SK's native capabilities
/// for Handlebars templating, function calling, and conversation management
/// </summary>
public class SemanticKernelOrchestrator : IWorkflowOrchestrator
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IHandlebarsPromptTemplateFactory _handlebarsFactory;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;
    private readonly IVectorStore _vectorStore;
    private readonly Dictionary<string, ChatHistory> _conversationStore = new();
    private Kernel? _kernel;

    public SemanticKernelOrchestrator(
        IKernelFactory kernelFactory,
        IHandlebarsPromptTemplateFactory handlebarsFactory,
        ILogger<SemanticKernelOrchestrator> logger,
        IVectorStore vectorStore)
    {
        _kernelFactory = kernelFactory;
        _handlebarsFactory = handlebarsFactory;
        _logger = logger;
        _vectorStore = vectorStore;
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        DotpromptWorkflow workflow, 
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken = default)
    {
        var executionStopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting SK workflow execution: {WorkflowName}", workflow.Name ?? "unnamed");
            
            // 1. Initialize Kernel with plugins
            _kernel ??= await _kernelFactory.CreateKernelAsync();

            // 2. Apply input default values from workflow specification
            ApplyInputDefaults(workflow, context);

            // 3. Create Handlebars template from workflow content
            var promptConfig = new PromptTemplateConfig
            {
                Template = workflow.Content?.RawMarkdown ?? throw new ArgumentException("Workflow content is required"),
                TemplateFormat = "handlebars", // Use SK's native Handlebars support
                Name = workflow.Name ?? "workflow",
                Description = workflow.Metadata?.Description ?? "Workflow execution"
            };

            // 4. Convert context variables to KernelArguments
            var kernelArgs = ConvertToKernelArguments(context);

            // 4. Create function from template using SK's Handlebars factory
            var workflowFunction = _kernel.CreateFunctionFromPrompt(promptConfig, _handlebarsFactory);

            // 5. Configure execution settings
            var executionSettings = CreateExecutionSettings(workflow);

            // 6. Setup conversation for resume capability
            var workflowId = GenerateWorkflowId(workflow, context);
            var chatHistory = await GetChatHistoryAsync(workflowId);

            // 7. Execute with SK's automatic function calling and Handlebars rendering
            _logger.LogInformation("Executing workflow with SK Handlebars templating and function calling");
            
            var result = await workflowFunction.InvokeAsync(_kernel, kernelArgs, cancellationToken);

            // 8. Save conversation state for resume
            await SaveChatHistoryAsync(workflowId, chatHistory);

            _logger.LogInformation("SK workflow execution completed successfully in {Duration}ms", 
                executionStopwatch.ElapsedMilliseconds);

            return new WorkflowExecutionResult(
                Success: true,
                Output: result.ToString(),
                ErrorMessage: null,
                ExecutionTime: executionStopwatch.Elapsed,
                Metadata: ExtractExecutionMetadata(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK workflow execution for {WorkflowName}", workflow.Name);
            
            return new WorkflowExecutionResult(
                Success: false,
                Output: null,
                ErrorMessage: $"Workflow execution failed: {ex.Message}",
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

            // 1. Validate workflow structure
            if (string.IsNullOrEmpty(workflow.Content?.RawMarkdown))
            {
                errors.Add("Workflow content cannot be empty");
            }

            // 2. Validate frontmatter configuration
            if (string.IsNullOrEmpty(workflow.Model))
            {
                warnings.Add("No model specified in frontmatter, will use default");
            }

            // 3. Validate Handlebars template syntax
            if (!string.IsNullOrEmpty(workflow.Content?.RawMarkdown))
            {
                try
                {
                    var promptConfig = new PromptTemplateConfig
                    {
                        Template = workflow.Content.RawMarkdown,
                        TemplateFormat = "handlebars"
                    };

                    // Test template creation (validates Handlebars syntax)
                    var template = _handlebarsFactory.Create(promptConfig);
                    
                    // Test rendering with empty context (validates variable references)
                    var testArgs = new KernelArguments();
                    await template.RenderAsync(_kernel ?? await _kernelFactory.CreateKernelAsync(), testArgs, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add($"Handlebars template validation failed: {ex.Message}");
                }
            }

            // 4. Validate provider configuration
            var providerName = ExtractProviderFromModel(workflow.Model) ?? "openai";
            if (!IsProviderConfigured(providerName))
            {
                warnings.Add($"AI provider '{providerName}' may not be properly configured");
            }

            // 5. Validate input schema and parameter references
            ValidateParameterReferences(workflow, errors, warnings);

            // 6. Validate tool dependencies
            ValidateToolDependencies(workflow, warnings);

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

        // TODO: Load from persistent file-based progress storage
        return Task.FromResult(new ChatHistory());
    }

    public async Task SaveChatHistoryAsync(string workflowId, ChatHistory chatHistory)
    {
        _conversationStore[workflowId] = chatHistory;
        
        // TODO: Persist to progress files for durable storage
        _logger.LogDebug("Saved chat history for workflow {WorkflowId} with {MessageCount} messages", 
            workflowId, chatHistory.Count);
        
        await Task.CompletedTask;
    }

    // Private helper methods
    private void ApplyInputDefaults(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        if (workflow.Input == null) return;

        _logger.LogDebug("Applying input defaults from workflow specification");

        // Step 1: Collect all parameter names from both locations
        var allParameters = new HashSet<string>();
        
        // From workflow-level defaults
        if (workflow.Input.Default != null)
        {
            foreach (var param in workflow.Input.Default.Keys)
                allParameters.Add(param);
        }
        
        // From schema definitions
        if (workflow.Input.Schema != null)
        {
            foreach (var param in workflow.Input.Schema.Keys)
                allParameters.Add(param);
        }

        // Step 2: Resolve each parameter using dotprompt precedence hierarchy
        foreach (var paramName in allParameters)
        {
            // Skip if already set in context (CLI parameters take highest precedence)
            if (context.Variables.ContainsKey(paramName))
            {
                _logger.LogDebug("Parameter '{ParameterName}' already set in context, skipping defaults", paramName);
                continue;
            }

            object? resolvedValue = null;

            // Priority 1: Schema-level default (input.schema.{param}.default)
            if (workflow.Input.Schema?.TryGetValue(paramName, out var schema) == true 
                && schema.Default != null)
            {
                resolvedValue = schema.Default;
                _logger.LogDebug("Applied schema-level default for parameter '{ParameterName}': {Value}", 
                    paramName, resolvedValue);
            }
            
            // Priority 2: Workflow-level default (input.default.{param}) - only if not already resolved
            else if (workflow.Input.Default?.TryGetValue(paramName, out var workflowDefault) == true)
            {
                resolvedValue = workflowDefault;
                _logger.LogDebug("Applied workflow-level default for parameter '{ParameterName}': {Value}", 
                    paramName, resolvedValue);
            }

            // Apply resolved value to context
            if (resolvedValue != null)
            {
                context.SetVariable(paramName, resolvedValue);
            }
        }

        _logger.LogInformation("Applied defaults for {Count} parameters from workflow specification", 
            allParameters.Count(p => context.Variables.ContainsKey(p)));
    }

    private KernelArguments ConvertToKernelArguments(WorkflowExecutionContext context)
    {
        var args = new KernelArguments();
        
        foreach (var (key, value) in context.Variables)
        {
            args[key] = value;
        }

        // Add built-in variables
        args["current_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
        args["current_time"] = DateTime.UtcNow.ToString("HH:mm:ss");
        args["working_directory"] = context.WorkingDirectory;

        return args;
    }

    private PromptExecutionSettings CreateExecutionSettings(DotpromptWorkflow workflow)
    {
        return new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), // Enable automatic function calling
            Temperature = workflow.Config?.Temperature ?? 0.7,
            MaxTokens = workflow.Config?.MaxOutputTokens ?? 4000,
            TopP = workflow.Config?.TopP,
            StopSequences = workflow.Config?.StopSequences?.ToArray()
        };
    }

    private string GenerateWorkflowId(DotpromptWorkflow workflow, WorkflowExecutionContext context)
    {
        var contentHash = ComputeContentHash(workflow.Content?.RawMarkdown ?? "");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{workflow.Name ?? "workflow"}_{contentHash[..8]}_{timestamp}";
    }

    private Dictionary<string, object> ExtractExecutionMetadata(FunctionResult result)
    {
        return new Dictionary<string, object>
        {
            ["function_name"] = result.Function.Name,
            ["plugin_name"] = result.Function.PluginName,
            ["execution_time"] = DateTime.UtcNow.ToString("O"),
            ["token_usage"] = result.Metadata?.ContainsKey("Usage") == true ? result.Metadata["Usage"] : null
        };
    }

    private void ValidateParameterReferences(DotpromptWorkflow workflow, List<string> errors, List<string> warnings)
    {
        if (workflow.Content?.ParameterReferences == null || workflow.Input?.Schema == null)
            return;

        var definedParameters = workflow.Input.Schema.Keys.ToHashSet();
        var referencedParameters = workflow.Content.ParameterReferences;

        foreach (var param in referencedParameters)
        {
            if (!definedParameters.Contains(param))
            {
                warnings.Add($"Parameter '{param}' is referenced but not defined in input schema");
            }
        }

        // Validate conflicting default values
        ValidateConflictingDefaults(workflow, warnings);
    }

    private void ValidateConflictingDefaults(DotpromptWorkflow workflow, List<string> warnings)
    {
        if (workflow.Input?.Default == null || workflow.Input?.Schema == null)
            return;

        foreach (var defaultParam in workflow.Input.Default.Keys)
        {
            if (workflow.Input.Schema.TryGetValue(defaultParam, out var schema) 
                && schema.Default != null)
            {
                // Check if values are different
                var workflowDefault = workflow.Input.Default[defaultParam];
                var schemaDefault = schema.Default;
                
                if (!Equals(workflowDefault, schemaDefault))
                {
                    warnings.Add($"Parameter '{defaultParam}' has conflicting defaults: " +
                        $"input.default = '{workflowDefault}', input.schema.{defaultParam}.default = '{schemaDefault}'. " +
                        $"Schema-level default will take precedence per dotprompt specification.");
                }
                else
                {
                    warnings.Add($"Parameter '{defaultParam}' has redundant defaults specified in both " +
                        $"input.default and input.schema.{defaultParam}.default. " +
                        $"Consider using only one location for clarity.");
                }
            }
        }
    }

    private void ValidateToolDependencies(DotpromptWorkflow workflow, List<string> warnings)
    {
        if (workflow.Tools == null) return;

        var availableTools = new HashSet<string> { "project-analysis", "build-test", "file-system", "sub-workflow" };
        
        foreach (var tool in workflow.Tools)
        {
            if (!availableTools.Contains(tool))
            {
                warnings.Add($"Tool '{tool}' may not be available or properly configured");
            }
        }
    }

    private bool IsProviderConfigured(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "openai" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
            "github" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN")),
            "azure" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")),
            "local" or "ollama" => true,
            _ => false
        };
    }

    private string? ExtractProviderFromModel(string? model)
    {
        if (string.IsNullOrEmpty(model)) return null;

        var parts = model.Split('/', 2);
        return parts.Length == 2 ? parts[0] : "openai";
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
```

## Key Features

### 1. **Native Handlebars Integration**
- Uses SK's `IHandlebarsPromptTemplateFactory` for full dotprompt compatibility
- Supports all Handlebars features: conditionals, loops, expressions, helpers
- Automatic variable substitution with proper type handling
- Template validation during workflow validation phase

### 2. **Automatic Function Calling**
- Leverages SK's `FunctionChoiceBehavior.Auto()` for intelligent tool orchestration
- Built-in tools available as SK functions (ProjectAnalysisPlugin, FileSystemPlugin, etc.)
- SubWorkflowPlugin enables recursive workflow composition
- Parallel function execution when supported by the model

### 3. **Conversation State Management**
- SK ChatHistory for persistent conversation state
- Resume functionality through file-based progress persistence
- Progress file storage for durable workflow state
- Workflow-specific conversation isolation

### 4. **Comprehensive Validation**
- Handlebars template syntax validation
- Parameter reference validation against input schema
- Tool dependency validation
- Provider configuration validation
- AI provider connectivity checks (optional)

### 5. **Performance Optimization**
- Lazy kernel initialization
- Function result caching via SK mechanisms
- File-based progress storage for efficient state management
- Efficient conversation state serialization

## Integration Points

### Dependency Injection Setup

```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddSemanticKernelOrchestrator(this IServiceCollection services)
{
    // Register Handlebars factory
    services.AddSingleton<IPromptTemplateFactory, HandlebarsPromptTemplateFactory>();
    services.AddSingleton<IHandlebarsPromptTemplateFactory, HandlebarsPromptTemplateFactory>();
    
    // Register orchestrator
    services.AddScoped<IWorkflowOrchestrator, SemanticKernelOrchestrator>();
    
    // Register kernel factory
    services.AddSingleton<IKernelFactory, KernelFactory>();
    
    // Register progress manager for file-based persistence
    services.AddSingleton<IProgressManager, FileProgressManager>();
    
    return services;
}
```

### Kernel Factory Implementation

```csharp
public class KernelFactory : IKernelFactory
{
    public async Task<Kernel> CreateKernelAsync()
    {
        var builder = Kernel.CreateBuilder();
        
        // Add AI services via Microsoft.Extensions.AI
        builder.Services.AddChatClient(/* configuration */);
        
        // Add built-in tool plugins
        builder.Plugins.AddFromType<ProjectAnalysisPlugin>("ProjectAnalysis");
        builder.Plugins.AddFromType<FileSystemPlugin>("FileSystem");
        builder.Plugins.AddFromType<BuildTestPlugin>("BuildTest");
        builder.Plugins.AddFromType<SubWorkflowPlugin>("SubWorkflow");
        
        // Add MCP plugins dynamically
        await AddMcpPluginsAsync(builder);
        
        // Add filters and middleware
        builder.Services.AddSingleton<IPromptRenderFilter, WorkflowSecurityFilter>();
        builder.Services.AddSingleton<IFunctionInvocationFilter, WorkflowPerformanceFilter>();
        
        return builder.Build();
    }
}
```

## Error Handling Strategy

### 1. **SK Filter-based Error Handling**
- `IPromptRenderFilter` for template rendering errors
- `IFunctionInvocationFilter` for tool execution errors
- `IChatCompletionFilter` for AI service errors

### 2. **Graceful Degradation**
- Fallback to basic execution when advanced features fail
- Provider failover through Microsoft.Extensions.AI
- Tool isolation to prevent cascading failures

### 3. **Retry and Circuit Breaker**
- SK middleware for intelligent retry policies
- Circuit breaker pattern for unstable services
- Exponential backoff for rate-limited APIs

## Future Enhancements

### 1. **Advanced Vector Store Integration**
- Semantic workflow caching based on content similarity
- Intelligent workflow discovery and recommendation
- Cross-workflow knowledge sharing

### 2. **Enhanced Conversation Management**
- Branching conversation histories for different execution paths
- Conversation merging for parallel workflow execution
- Advanced resume capabilities with partial state recovery

### 3. **Performance Optimizations**
- Workflow compilation and caching
- Template pre-compilation for frequently used workflows
- Streaming execution for large workflows

### 4. **Monitoring and Observability**
- Detailed execution telemetry through SK instrumentation
- Workflow performance analytics
- AI token usage tracking and optimization

## Testing Strategy

### Unit Tests
- Mock SK interfaces for isolated testing
- Template rendering validation
- Error handling scenarios
- Configuration validation

### Integration Tests
- Full SK pipeline testing with real AI providers
- Sub-workflow composition testing
- Conversation state persistence testing
- MCP integration testing

### Performance Tests
- Large workflow execution benchmarks
- Memory usage profiling
- Concurrent execution testing
- Vector Store performance validation

This orchestrator design provides a solid foundation for AI-powered workflow execution while leveraging Semantic Kernel's mature, enterprise-ready capabilities for production use.
