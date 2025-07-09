# .NET Prompt Tool - Architecture Recommendation

## Overview

This document outlines the recommended architecture for the .NET Prompt CLI tool based on the comprehensive requirements analysis. The architecture follows Clean Architecture principles with Command Pattern for CLI operations, ensuring maintainability, testability, and extensibility.

## 1. Overall Architecture Pattern

**Clean Architecture** approach with **Command Pattern** for CLI operations, organized into the following layers:

```
┌─────────────────────────────────────┐
│           CLI Interface             │
│    (Commands & Option Parsing)      │
├─────────────────────────────────────┤
│        Application Layer            │
│   (Use Cases & Orchestration)       │
├─────────────────────────────────────┤
│          Domain Layer               │
│    (Core Business Logic)            │
├─────────────────────────────────────┤
│       Infrastructure Layer          │
│ (AI Providers, MCP, File System)    │
└─────────────────────────────────────┘
```

## 2. Project Structure

```
src/
├── DotnetPrompt.Cli/                 # CLI Entry Point
├── DotnetPrompt.Core/                # Domain Layer
├── DotnetPrompt.Application/         # Application Layer
├── DotnetPrompt.Infrastructure/      # Infrastructure Layer
└── DotnetPrompt.Shared/             # Shared Components
tests/
├── DotnetPrompt.UnitTests/
├── DotnetPrompt.IntegrationTests/
└── DotnetPrompt.AcceptanceTests/
```

## 3. Core Domain Model

### 3.1 Primary Entities

- **Workflow**: Represents a complete .prompt.md file with frontmatter and content
- **WorkflowExecution**: Tracks the execution state and progress
- **Tool**: Abstraction for both built-in and MCP tools
- **Provider**: AI model provider abstraction
- **Configuration**: Hierarchical configuration management

### 3.2 Value Objects

- **ModelConfiguration**: Model settings (temperature, max_tokens, etc.)
- **ProviderSettings**: Provider-specific configuration
- **WorkflowMetadata**: Parsed frontmatter data
- **ExecutionContext**: Current execution state and parameters

## 4. Layer-by-Layer Design

### 4.1 CLI Layer (DotnetPrompt.Cli)

Use **System.CommandLine** for robust CLI functionality:

```csharp
// Primary Commands
- RunCommand         // dotnet prompt run
- RestoreCommand     // dotnet prompt restore  
- ListCommand        // dotnet prompt list
- ValidateCommand    // dotnet prompt validate
- ResumeCommand      // dotnet prompt resume
```

**Key Responsibilities:**
- Command parsing and validation
- Global option handling (--verbose, --provider, etc.)
- Dependency injection container setup
- Error handling and user feedback

### 4.2 Application Layer (DotnetPrompt.Application)

Implements use cases leveraging **Semantic Kernel** for AI orchestration and **native Handlebars templating**:

```csharp
// Use Cases (MediatR Handlers)
- ExecuteWorkflowHandler (uses SemanticKernelOrchestrator with Handlebars)
- RestoreDependenciesHandler (uses SK tool orchestration)
- ValidateWorkflowHandler (uses SK Handlebars template validation)
- ResumeWorkflowHandler (uses SK conversation state & chat history)
- ListWorkflowsHandler (uses SK vector search for discovery)

// Services (SK-native implementation)
- IWorkflowOrchestrator (SemanticKernelOrchestrator with Handlebars templates)
- IProgressManager (FileProgressManager with SK ChatHistory serialization)
- IConfigurationResolver (uses SK dependency injection patterns)
- IKernelFactory (configures SK with providers, tools, and Handlebars factory)
```

**Key Responsibilities:**
- Workflow execution orchestration via SK Handlebars templates and function calling
- Variable substitution via SK native Handlebars templating engine
- Sub-workflow composition via dedicated SubWorkflowPlugin
- Progress tracking and resume via file-based storage with SK ChatHistory serialization
- Configuration hierarchy via SK dependency injection patterns
- Error handling and retry via SK filters and middleware
- Observability and telemetry via SK built-in instrumentation

### 4.3 Domain Layer (DotnetPrompt.Core)

Pure business logic with no external dependencies:

```csharp
// Domain Services
- IWorkflowParser
- IToolRegistry
- IProviderResolver
- IWorkflowValidator

// Domain Models
- Workflow, WorkflowExecution
- Tool, BuiltInTool, McpTool
- Provider, ModelConfiguration
- ExecutionProgress, ConversationState
```

**Key Responsibilities:**
- Workflow parsing and validation
- Tool registration and discovery
- Provider selection logic
- Business rule enforcement

### 4.4 Infrastructure Layer (DotnetPrompt.Infrastructure)

External integrations and persistence, fully leveraging SK capabilities:

```csharp
// AI Provider Integration (using Microsoft.Extensions.AI + SK)
- IChatClient (unified interface via Microsoft.Extensions.AI)
- Semantic Kernel service registration and configuration
- SK filters for retry policies, rate limiting, and circuit breakers
- SK middleware for logging, telemetry, and authentication

// Semantic Kernel Integration (Native Handlebars + Plugins)
- KernelBuilder with full dependency injection setup
- HandlebarsPromptTemplateFactory for dotprompt-compatible templating
- Built-in tool plugins with SK function annotations (no WorkflowExecutorPlugin)
- SubWorkflowPlugin for sub-workflow composition and orchestration
- SK ChatHistory serialization for progress files
- SK filters for function calling, prompt execution, and chat completion
- SK middleware for observability, security, and performance monitoring

// Built-in Tools (as SK Plugins with full SK capabilities)
- ProjectAnalysisPlugin (SK function with parameter validation)
- BuildTestPlugin (SK function with retry policies)
- FileSystemPlugin (SK function with security filters)
- SubWorkflowPlugin (SK function for workflow composition)

// Progress & State Management (File-Based)
- FileProgressManager for JSON progress file persistence
- Progress file management utilities (cleanup, backup, validation)
- Atomic file operations for progress checkpoint safety
- Configurable retention policies for progress file cleanup

// MCP Integration (via SK Plugin Architecture)
- McpServerPlugin (SK plugin wrapper for MCP servers)
- Dynamic MCP tool registration as SK functions
- SK parameter validation for MCP tool parameters
- SK error handling and retry for MCP server communication

// Cross-Cutting Concerns (SK Filters & Middleware)
- PromptExecutionFilter for security and validation
- FunctionInvocationFilter for performance monitoring
- ChatCompletionFilter for content safety and compliance
- Custom SK middleware for workflow-specific concerns

// External Services (SK-enhanced)
- GitHubCliAuthenticator (with SK secure credential management)
- ConfigurationPersistence (using SK dependency injection patterns)
- TelemetryService (leveraging SK built-in instrumentation)
```

## 5. Key Architectural Patterns (SK-Maximized)

### 5.1 Microsoft.Extensions.AI + SK Integration

```csharp
// Unified AI provider configuration with SK dependency injection
var builder = Kernel.CreateBuilder();

// Add AI services via Microsoft.Extensions.AI
builder.Services.AddChatClient(chatBuilder =>
{
    var provider = configuration.GetProvider();
    return provider switch
    {
        "github" => chatBuilder.UseGitHubModels(apiKey),
        "openai" => chatBuilder.UseOpenAI(apiKey),
        "azure" => chatBuilder.UseAzureOpenAI(endpoint, apiKey),
        "anthropic" => chatBuilder.UseAnthropic(apiKey),
        "local" => chatBuilder.UseOllama(endpoint),
        _ => throw new ArgumentException($"Unknown provider: {provider}")
    };
})
.UseRetry() // SK-compatible retry middleware
.UseLogging() // SK-compatible logging middleware
.UseTelemetry(); // SK-compatible telemetry middleware

// Register all workflow components as SK services
builder.Services.AddSingleton<IWorkflowOrchestrator, SkWorkflowOrchestrator>();
builder.Services.AddSingleton<IProgressManager, SkProgressManager>();
builder.Services.AddSingleton<IConversationStateManager, SkConversationStateManager>();

// Add SK Vector Store for memory and caching
builder.Services.AddSingleton<IVectorStore>(provider => 
    new InMemoryVectorStore()); // or Qdrant, Azure AI Search, etc.

var kernel = builder.Build();
```

### 5.2 SK Native Handlebars Templating for Variable Substitution

```csharp
// Native SK Handlebars integration instead of custom regex replacement
public class SemanticKernelOrchestrator : IWorkflowOrchestrator
{
    private readonly IHandlebarsPromptTemplateFactory _handlebarsFactory;
    private readonly IChatCompletionService _chatService;
    
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        DotpromptWorkflow workflow,
        WorkflowExecutionContext context)
    {
        // 1. Create SK Handlebars template directly
        var promptConfig = new PromptTemplateConfig
        {
            Template = workflow.Content.RawMarkdown,
            TemplateFormat = "handlebars", // Native SK Handlebars support
            Name = workflow.Name,
            Description = workflow.Metadata?.Description
        };

        // 2. Convert context variables to KernelArguments
        var kernelArgs = ConvertToKernelArguments(context.Variables);

        // 3. Create function from template using SK's Handlebars factory
        var function = _kernel.CreateFunctionFromPrompt(promptConfig, _handlebarsFactory);
        
        // 4. Execute with SK's automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = workflow.Config?.Temperature ?? 0.7,
            MaxTokens = workflow.Config?.MaxOutputTokens ?? 4000
        };

        // 5. Execute with full SK orchestration (includes Handlebars rendering)
        var result = await function.InvokeAsync(_kernel, kernelArgs);
        
        return new WorkflowExecutionResult(true, result.ToString());
    }
}
```

### 5.3 SubWorkflowPlugin for Composition

```csharp
// Dedicated plugin for sub-workflow composition (replaces WorkflowExecutorPlugin)
[Description("Executes and composes sub-workflows")]
public class SubWorkflowPlugin
{
    private readonly IDotpromptParser _parser;
    private readonly IWorkflowOrchestrator _orchestrator;

    [KernelFunction("execute_sub_workflow")]
    [Description("Executes a sub-workflow from a file path with parameters")]
    public async Task<string> ExecuteSubWorkflowAsync(
        [Description("Path to the sub-workflow .prompt.md file")] string workflowPath,
        [Description("JSON object containing variables for the sub-workflow")] string parameters = "{}",
        [Description("Context inheritance mode: 'inherit', 'isolated', or 'merge'")] string contextMode = "inherit",
        CancellationToken cancellationToken = default)
    {
        // Parse and execute sub-workflow using the main orchestrator
        var subWorkflow = await _parser.ParseFileAsync(workflowPath, cancellationToken);
        var subContext = CreateSubWorkflowContext(parameters, contextMode);
        var result = await _orchestrator.ExecuteWorkflowAsync(subWorkflow, subContext, cancellationToken);
        
        return result.Output ?? "Sub-workflow completed successfully";
    }
}
```

### 5.4 File-Based Progress Management with SK ChatHistory

```csharp
// File-based progress management with SK ChatHistory integration
public class FileProgressManager : IProgressManager
{
    private readonly ILogger<FileProgressManager> _logger;
    private readonly ProgressConfig _config;
    
    public async Task<ChatHistory> LoadConversationStateAsync(string workflowId)
    {
        var progressData = await LoadProgressAsync(workflowId);
        if (progressData?.ChatHistory != null)
        {
            // Reconstruct SK ChatHistory from saved progress file
            var chatHistory = new ChatHistory();
            foreach (var message in progressData.Value.ChatHistory)
            {
                chatHistory.Add(new ChatMessageContent(
                    AuthorRole.Parse(message.Role),
                    message.Content));
            }
            return chatHistory;
        }
        
        return new ChatHistory();
    }
    
    public async Task SaveConversationStateAsync(string workflowId, ChatHistory chatHistory)
    {
        // Save as part of the progress file - no separate conversation persistence needed
        var existingProgress = await LoadProgressAsync(workflowId);
        if (existingProgress != null)
        {
            await SaveProgressAsync(workflowId, existingProgress.Value.Context!, chatHistory);
        }
    }
}
        });
    }
}
```

### 5.5 SK Filters for Cross-Cutting Concerns

```csharp
// SK filters for comprehensive error handling and observability
public class WorkflowExecutionFilter : IPromptRenderFilter, IFunctionInvocationFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        // Pre-execution: Validate workflow syntax, check permissions
        await ValidateWorkflowSyntax(context);
        
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // SK filter handles all workflow execution errors consistently
            await LogWorkflowError(context, ex);
            throw;
        }
    }
    
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        // Pre-execution: Parameter validation, security checks
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await next(context);
            
            // Post-execution: Performance monitoring, result validation
            await LogFunctionPerformance(context.Function.Name, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            await LogFunctionError(context, ex);
            throw;
        }
    }
}

// Register SK filters in kernel configuration
builder.Services.AddSingleton<IPromptRenderFilter, WorkflowExecutionFilter>();
builder.Services.AddSingleton<IFunctionInvocationFilter, WorkflowExecutionFilter>();
```

### 5.6 File-Based Progress Management

```csharp
// File-based progress management with SK ChatHistory serialization
public class FileProgressManager : IProgressManager
{
    private readonly ILogger<FileProgressManager> _logger;
    private readonly ProgressConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public async Task SaveProgressAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        var progressDir = GetProgressDirectory();
        Directory.CreateDirectory(progressDir);
        
        var progressFile = Path.Combine(progressDir, $"{workflowId}.json");
        var progressData = new WorkflowProgressFile
        {
            WorkflowMetadata = CreateMetadata(workflowId, context),
            ChatHistory = SerializeChatHistory(chatHistory),
            ExecutionContext = context
        };
        
        // Atomic write to prevent corruption
        var tempFile = $"{progressFile}.tmp";
        var json = JsonSerializer.Serialize(progressData, _jsonOptions);
        await File.WriteAllTextAsync(tempFile, json);
        File.Move(tempFile, progressFile);
        
        _logger.LogInformation("Progress saved to {ProgressFile}", progressFile);
    }
    
    public async Task<(WorkflowExecutionContext?, ChatHistory?)?> LoadProgressAsync(string workflowId)
    {
        var progressFile = Path.Combine(GetProgressDirectory(), $"{workflowId}.json");
        
        if (!File.Exists(progressFile))
            return null;
            
        var json = await File.ReadAllTextAsync(progressFile);
        var progressData = JsonSerializer.Deserialize<WorkflowProgressFile>(json, _jsonOptions);
        
        if (progressData == null)
            return null;
            
        var chatHistory = DeserializeChatHistory(progressData.ChatHistory);
        return (progressData.ExecutionContext, chatHistory);
    }
    
    private string GetProgressDirectory() => _config.StorageLocation ?? "./.dotnet-prompt/progress";
}
```

## 6. Technology Stack Recommendations

### 6.1 Core Framework

- **.NET 8.0** (LTS) as minimum target
- **C# 12** with latest language features
- **System.CommandLine** for CLI parsing
- **Microsoft.Extensions.DependencyInjection** for IoC

### 6.2 Microsoft AI Ecosystem Libraries (SK Native Approach)

- **Microsoft.Extensions.AI** - Unified AI provider abstraction with SK-compatible middleware
- **Microsoft.SemanticKernel** - Complete AI orchestration framework with function calling and state management
- **Microsoft.SemanticKernel.PromptTemplates.Handlebars** - Native Handlebars templating for dotprompt compatibility
- **Microsoft.SemanticKernel.Plugins.Core** - Built-in SK plugins for core operations
- **Microsoft.Extensions.AI.Abstractions** - Core interfaces for AI services
- **C# SDK for MCP** - Native Model Context Protocol support integrated via SK plugins

### 6.3 Supporting Libraries

- **MediatR** - CQRS/Mediator pattern for use cases
- **Serilog** - Structured logging with AI telemetry integration
- **YamlDotNet** - YAML frontmatter parsing
- **Markdig** - Markdown processing
- **System.Text.Json** - JSON serialization

### 6.3 Testing Framework

- **xUnit** - Unit testing
- **FluentAssertions** - Test assertions
- **Moq** - Mocking framework
- **TestContainers** - Integration testing

## 7. Security and Error Handling (SK-Enhanced)

### 7.1 Security Considerations (Leveraging SK Security Features)

- **SK Security Filters**: Use SK's built-in security filters for prompt injection detection and content safety
- **Function Authorization**: SK function-level authorization attributes for tool access control
- **Configuration Encryption**: Encrypt sensitive data using SK's secure credential management
- **Token Management**: Secure API key storage via SK dependency injection with Azure Key Vault integration
- **Input Validation**: SK automatic parameter validation with custom validation attributes
- **Trust Boundaries**: SK function isolation and sandboxing for safe tool execution
- **Prompt Safety**: SK built-in prompt execution filters for preventing malicious prompts

### 7.2 Error Handling Strategy (SK Filters & Middleware)

- **SK Exception Filters**: Comprehensive error handling via SK's IFunctionInvocationFilter
- **Semantic Retry Policies**: SK-aware retry logic that understands AI service failures and token limits
- **Circuit Breaker Pattern**: SK middleware for protecting against cascading failures
- **Graceful Degradation**: SK function fallback mechanisms when services are unavailable
- **Error Categorization**: SK-specific error types for AI service failures, function execution errors, and planning failures
- **Observability Integration**: SK built-in telemetry for error tracking and performance monitoring

## 8. Performance Considerations (SK-Optimized)

### 8.1 Startup Performance (SK Best Practices)

- **Lazy Kernel Initialization**: Create SK kernels on-demand with cached configurations
- **Plugin Registration Optimization**: Register only required SK plugins per workflow
- **Service Provider Caching**: Cache SK service configurations for rapid kernel creation
- **Progress File Indexing**: Fast scanning of progress directory for available workflows
- **Function Compilation**: SK function compilation caching for repeated workflow executions

### 8.2 Runtime Performance (SK Features)

- **SK Function Caching**: Leverage SK's built-in function result caching mechanisms
- **File-Based Progress**: Efficient JSON serialization and atomic file operations
- **Conversation State Compression**: Efficient SK ChatHistory serialization and storage
- **Parallel Function Execution**: SK support for parallel function calling when appropriate
- **Token Management**: SK automatic token counting and optimization for model interactions
- **Memory Efficiency**: SK streaming capabilities for large file operations and conversation histories

## 9. Extensibility Points (SK-Powered)

### 9.1 Plugin Architecture (SK Native)

- **SK Function Plugins**: Additional built-in tools via SK's native plugin system
- **SubWorkflowPlugin**: Dedicated plugin for workflow composition and orchestration
- **Provider Extensions**: Custom AI provider implementations via Microsoft.Extensions.AI
- **Progress Extensions**: Custom progress management implementations via IProgressManager interface
- **Filter Pipeline**: Custom SK filters for workflow-specific processing and validation
- **Middleware Extensions**: Custom SK middleware for specialized cross-cutting concerns

### 9.2 MCP Integration (SK Plugin Wrappers)

- **Dynamic MCP Loading**: Runtime discovery of MCP servers via SK plugin factory
- **SK Function Mapping**: Automatic conversion of MCP tools to SK functions with proper annotations
- **Version Compatibility**: Graceful MCP version handling via SK function versioning
- **Tool Isolation**: SK function sandboxing for safe MCP tool execution
- **State Management**: MCP server state persistence via progress file integration

## 10. Deployment and Distribution

### 10.1 Packaging

- **Single File Deployment**: Self-contained executable option
- **NuGet Tool Package**: Standard .NET tool distribution
- **Cross-Platform**: Native builds for Windows, macOS, Linux

### 10.2 Versioning Strategy

- **Semantic Versioning**: Clear version compatibility rules
- **Breaking Change Policy**: Major version for breaking changes
- **Backward Compatibility**: Maintain workflow format compatibility

## 11. Implementation Roadmap

### Phase 1: Foundation (MVP)
1. Core domain models and interfaces
2. Basic CLI structure with System.CommandLine
3. Configuration hierarchy implementation
4. Workflow parsing (YAML frontmatter + Markdown)

### Phase 2: Core Functionality
1. GitHub Models provider implementation
2. Built-in tools (Project Analysis, Build & Test)
3. Basic workflow execution engine
4. Progress tracking and resume functionality

### Phase 3: Extensibility
1. Additional AI providers (OpenAI, Azure, Anthropic, Local)
2. MCP server integration
3. Sub-workflow composition
4. Advanced error handling and retry logic

### Phase 4: Polish & Optimization
1. Performance optimizations
2. Comprehensive testing suite
3. Documentation and examples
4. Community feedback integration

## 12. Quality Attributes

### 12.1 Maintainability
- Clear separation of concerns through layered architecture
- SOLID principles adherence
- Comprehensive unit and integration testing
- Well-documented APIs and interfaces

### 12.2 Extensibility
- Plugin architecture for providers and tools
- MCP server integration for custom tools
- Configuration provider extensibility
- Event-driven architecture for cross-cutting concerns

### 12.3 Performance
- Lazy loading and minimal startup overhead
- Efficient memory management
- Async/await throughout for I/O operations
- Caching strategies for repeated operations

### 12.4 Reliability
- Comprehensive error handling
- Retry mechanisms for transient failures
- Progress persistence for long-running operations
- Graceful degradation when services are unavailable

## 13. CLI Output Patterns & User Experience Design

### 13.1 Hybrid Logging Architecture (Industry Standard)

The CLI tool implements a **dual-output pattern** that separates user-facing communication from operational telemetry:

```csharp
// ✅ User-Facing Output (Console.WriteLine/Error.WriteLine)
Console.WriteLine("✓ Configuration is valid");
Console.Error.WriteLine("Error: Workflow file not found");

// ✅ Structured Logging (ILogger) 
_logger.LogInformation("Workflow completed successfully in {ExecutionTime}", result.ExecutionTime);
_logger.LogError("Workflow execution failed: {ErrorMessage}", result.ErrorMessage);
```

### 13.2 CLI Output Design Principles

**User-Facing Output Responsibilities:**
- **Primary user interface** - Direct communication with CLI users
- **Machine parseable** - Scripts and automation can capture and process output
- **Immediate feedback** - Appears instantly without formatting overhead
- **Exit code correlation** - Error messages directly correlate with exit codes
- **Pipeline compatible** - Works correctly with stdout/stderr redirection

**Structured Logging Responsibilities:**
- **Operational observability** - Debugging, monitoring, troubleshooting
- **Structured data** - Correlation IDs, timing, context enrichment
- **Configurable output** - Can be redirected to files, external sinks
- **Development insights** - Verbose mode, detailed execution traces
- **Telemetry integration** - Performance metrics and error analytics

### 13.3 Command Implementation Pattern

```csharp
public async Task<int> ExecuteAsync(RunOptions options)
{
    try
    {
        // Structured logging for operations
        _logger.LogInformation("Executing workflow: {WorkflowFile}", options.WorkflowFile);
        
        var result = await _workflowService.ExecuteAsync(options.WorkflowFile, executionOptions);
        
        if (result.Success)
        {
            // User-facing success output
            if (!string.IsNullOrEmpty(result.Output))
            {
                Console.WriteLine(result.Output);  // ✅ User sees this
            }
            
            // Structured logging for telemetry
            _logger.LogInformation("Workflow completed successfully in {ExecutionTime}", result.ExecutionTime);
            return ExitCodes.Success;
        }
        else
        {
            // User-facing error output
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");  // ✅ User sees this
            
            // Structured logging for debugging
            _logger.LogError("Workflow execution failed: {ErrorMessage}", result.ErrorMessage);
            return ExitCodes.GeneralError;
        }
    }
    catch (FileNotFoundException ex)
    {
        // User-facing error
        Console.Error.WriteLine($"Error: Workflow file not found - {ex.Message}");  // ✅ User sees this
        
        // Structured logging
        _logger.LogError(ex, "Workflow file not found");  // ✅ Ops team sees this
        return ExitCodes.GeneralError;
    }
}
```

### 13.4 Verbose Mode Integration

```csharp
if (options.Verbose)
{
    // Show both user output AND structured logs
    Console.WriteLine("Executing workflow with options:");
    Console.WriteLine($"  Context: {executionOptions.Context}");
    
    // Structured logging provides additional detail
    _logger.LogDebug("Options: {@Options}", options);
}
```

### 13.5 Configuration-Driven Logging

The `LoggingConfiguration` model supports both output patterns:

```csharp
public class LoggingConfiguration
{
    public string? Level { get; set; }
    public bool? Console { get; set; }  // ✅ Controls ILogger console output
    public bool? Structured { get; set; }
    public bool? IncludeScopes { get; set; }
}
```

### 13.6 Industry Examples & Rationale

**Why Hybrid Approach (Not ILogger-Only):**

1. **Performance** - Direct console output has no formatting/enrichment overhead
2. **Reliability** - Users don't need to configure console sinks properly
3. **UX Consistency** - Output format matches CLI conventions (not log format)
4. **Pipeline Compatibility** - stdout/stderr redirection works correctly
5. **Testing Simplicity** - Easier to assert on user-visible output

**Successful CLI Tools Using This Pattern:**
- **Azure CLI**: User output to stdout, debug logging configurable
- **Docker CLI**: Command results to stdout, --log-level controls structured logs
- **Git**: User output to stdout/stderr, GIT_TRACE enables detailed logging

## 14. Microsoft AI Ecosystem Integration Benefits

### 14.1 Microsoft.Extensions.AI Advantages

**Unified Provider Interface**: 
- Eliminates custom provider abstraction code
- Consistent behavior across all AI providers
- Built-in middleware for retry, caching, telemetry
- Future-proof as new providers are added to the ecosystem

**Reduced Complexity**:
- No need to implement custom HTTP clients for each provider
- Standardized authentication and configuration patterns
- Built-in error handling and resilience patterns

**Enterprise Ready**:
- Native telemetry and monitoring support
- Configuration through .NET's Options pattern
- Dependency injection integration

### 14.2 Semantic Kernel Benefits (Comprehensive Utilization)

**AI Orchestration & Planning**:
- Native function calling with automatic planning eliminates custom orchestration code
- Built-in conversation state management provides resume functionality out-of-the-box
- Prompt template engine with Liquid syntax enhances markdown workflow processing
- Automatic parallel function execution optimizes multi-tool workflows
- Plugin architecture provides consistent tool integration across built-in and MCP tools

**Memory & State Management**:
- ChatHistory provides persistent conversation state with automatic serialization
- Vector Store connectors enable intelligent caching and semantic search
- Embedding generation services support workflow discovery and similarity matching
- Conversation state persistence handles long-running workflow interruptions
- Memory optimization with built-in compression and streaming capabilities

**Cross-Cutting Concerns**:
- Filter pipeline provides consistent error handling, logging, and validation
- Middleware system enables telemetry, performance monitoring, and security
- Dependency injection integration follows .NET best practices
- Built-in retry policies and circuit breakers for resilience
- Automatic parameter validation and type conversion for all functions

**Observability & Monitoring**:
- Native telemetry integration with OpenTelemetry standards
- Built-in performance counters for function execution and model interactions
- Structured logging with correlation IDs for workflow tracing
- Health checks for AI services and external dependencies
- Real-time metrics for token usage, costs, and performance optimization

### 13.3 C# MCP SDK Benefits

**Protocol Implementation**:
- No need to implement MCP protocol from scratch
- Type-safe MCP server communication
- Built-in connection management and retry logic
- Automatic tool discovery and registration

**Integration Simplicity**:
- Direct integration with Semantic Kernel's function calling
- Strongly-typed tool definitions
- Automatic parameter marshaling
- Error handling and fault tolerance

### 13.4 Architecture Simplification (Maximum SK Leverage)

**Before (Custom Implementation)**:
- Custom AI provider abstractions (~500 lines)
- Custom tool calling implementation (~800 lines)
- Custom MCP protocol implementation (~1200 lines)
- Custom conversation state management (~400 lines)
- Custom error handling and retry logic (~600 lines)
- Custom caching and memory management (~450 lines)
- Custom observability and telemetry (~350 lines)
- **Total**: ~4300 lines of infrastructure code

**After (Maximum SK Utilization)**:
- AI provider configuration via Microsoft.Extensions.AI (~30 lines)
- Semantic Kernel setup with full feature utilization (~80 lines)
- MCP integration via SK plugin wrappers (~40 lines)
- Progress tracking via file-based storage with SK ChatHistory (~35 lines)
- Error handling via SK filters and middleware (~60 lines)
- File-based persistence and management (~25 lines)
- Observability via SK built-in telemetry (~15 lines)
- Workflow orchestration via SK function calling (~50 lines)
- **Total**: ~335 lines of infrastructure code

**Code Reduction**: ~92% reduction in infrastructure code
**Feature Enhancement**: Significantly more capabilities with less code
**Maintenance Burden**: Minimal - Microsoft maintains all core functionality
**Reliability**: Enterprise-grade reliability from battle-tested SK framework
**Performance**: Optimized SK implementations outperform custom solutions
**Extensibility**: Rich plugin ecosystem and standard patterns for customization

## 14. Updated Implementation Roadmap (SK-Maximized)

### Phase 1: Foundation (MVP) - SK-First Approach
1. Microsoft.Extensions.AI provider configuration with SK integration
2. Basic CLI structure with System.CommandLine
3. Full Semantic Kernel setup with dependency injection, filters, and middleware
4. Workflow parsing with SK prompt template engine integration
5. File-based progress management setup with JSON serialization

### Phase 2: Core Functionality - SK-Powered
1. Built-in tools as comprehensive SK plugins with proper annotations
2. Workflow execution via SK automatic function calling and planning
3. Progress tracking and state management via file-based storage with SK ChatHistory serialization
4. Resume functionality leveraging progress file restoration and SK conversation state
5. Error handling and retry via SK filters and middleware pipeline

### Phase 3: Extensibility - SK Plugin Ecosystem
1. Additional AI providers via Microsoft.Extensions.AI with SK middleware
2. MCP server integration via SK plugin wrappers and function factories
3. Sub-workflow composition through SK automatic planning and function orchestration
4. Advanced progress file management (cleanup, compression, backup)
5. Comprehensive observability via SK built-in telemetry and instrumentation

### Phase 4: Polish & Optimization - SK Performance Tuning
1. Performance optimization of SK kernel configuration and function execution
2. Comprehensive testing with SK-aware mocks and test harnesses
3. Documentation and examples using SK standard patterns and best practices
4. Community feedback integration with SK plugin contribution guidelines
5. Advanced SK features: custom filters, progress management optimizations, and enterprise integrations

---

*This architecture maximizes Semantic Kernel's comprehensive capabilities while using simple, maintainable file-based progress storage. By leveraging SK's function calling, planning, and ChatHistory management with straightforward JSON persistence, we achieve a robust, maintainable, and highly capable system with minimal infrastructure complexity.*
