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

Implements use cases leveraging **Semantic Kernel** for AI orchestration:

```csharp
// Use Cases (MediatR Handlers)
- ExecuteWorkflowHandler (uses Semantic Kernel)
- RestoreDependenciesHandler
- ValidateWorkflowHandler
- ResumeWorkflowHandler
- ListWorkflowsHandler

// Services
- IWorkflowOrchestrator (wraps Semantic Kernel)
- IProgressManager (integrates with SK conversation state)
- IConfigurationResolver
- IKernelFactory (configures Semantic Kernel with providers and tools)
```

**Key Responsibilities:**
- Workflow execution orchestration
- Sub-workflow composition
- Progress tracking and resume logic
- Configuration hierarchy resolution

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

External integrations and persistence:

```csharp
// AI Provider Integration (using Microsoft.Extensions.AI)
- IChatClient (unified interface for all providers)
- GitHubModelsChatClient
- OpenAIChatClient  
- AzureOpenAIChatClient
- AnthropicChatClient
- OllamaChatClient

// Semantic Kernel Integration
- KernelBuilder and Kernel configuration
- Built-in tool plugins for Semantic Kernel
- Conversation state management
- Prompt template management

// Built-in Tools (as Semantic Kernel Plugins)
- ProjectAnalysisPlugin
- BuildTestPlugin
- FileSystemPlugin

// MCP Integration (using C# MCP SDK)
- McpClientFactory
- McpToolRegistry
- McpServerConnectionManager

// External Services
- GitHubCliAuthenticator
- ConfigurationPersistence
```

## 5. Key Architectural Patterns

### 5.1 Microsoft.Extensions.AI Integration

```csharp
// Unified AI provider configuration using Microsoft.Extensions.AI
services.AddChatClient(builder =>
{
    var provider = configuration.GetProvider();
    return provider switch
    {
        "github" => builder.UseGitHubModels(apiKey),
        "openai" => builder.UseOpenAI(apiKey),
        "azure" => builder.UseAzureOpenAI(endpoint, apiKey),
        "anthropic" => builder.UseAnthropic(apiKey),
        "local" => builder.UseOllama(endpoint),
        _ => throw new ArgumentException($"Unknown provider: {provider}")
    };
})
.UseRetry(retryOptions)
.UseLogging()
.UseTelemetry();
```

### 5.2 Semantic Kernel Tool Integration

```csharp
// Built-in tools as Semantic Kernel plugins
[KernelFunction]
public async Task<string> AnalyzeProject(
    [Description("The project file path")] string projectPath)
{
    // Tool implementation
}

// MCP tools dynamically registered
kernel.Plugins.AddFromMcpServer("filesystem-mcp", mcpClient);
```

### 5.3 Factory Pattern for Kernel Configuration

```csharp
public interface IKernelFactory
{
    Task<Kernel> CreateKernelAsync(WorkflowConfiguration config);
    Task RegisterToolsAsync(Kernel kernel, IEnumerable<string> requiredTools);
    Task RegisterMcpServersAsync(Kernel kernel, IEnumerable<McpServerConfig> mcpServers);
}
```

### 5.4 Observer Pattern for Progress Tracking

```csharp
public interface IProgressObserver
{
    Task OnProgressUpdated(WorkflowExecution execution);
    Task OnToolExecuted(string toolName, object result);
    Task OnConversationStateChanged(ConversationState state);
}
```

## 6. Technology Stack Recommendations

### 6.1 Core Framework

- **.NET 8.0** (LTS) as minimum target
- **C# 12** with latest language features
- **System.CommandLine** for CLI parsing
- **Microsoft.Extensions.DependencyInjection** for IoC

### 6.2 Microsoft AI Ecosystem Libraries

- **Microsoft.Extensions.AI** - Unified AI provider abstraction with built-in middleware
- **Microsoft.SemanticKernel** - AI orchestration, tool calling, and conversation management
- **Microsoft.Extensions.AI.Abstractions** - Core IChatClient and IEmbeddingGenerator interfaces
- **C# SDK for MCP** - Native Model Context Protocol support (when available)

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

## 7. Security and Error Handling

### 7.1 Security Considerations

- **Configuration Encryption**: Encrypt sensitive data in config files using DPAPI
- **Token Management**: Secure storage of API keys with appropriate file permissions
- **Input Validation**: Robust validation of workflow files and user inputs
- **Trust Boundaries**: Clear documentation of full-trust execution model

### 7.2 Error Handling Strategy

- **Result Pattern**: Use Result<T> for operation outcomes
- **Custom Exceptions**: Domain-specific exceptions with clear messages
- **Retry Logic**: Exponential backoff for transient failures
- **Circuit Breaker**: Protection against failing external services

## 8. Performance Considerations

### 8.1 Startup Performance

- **Lazy Loading**: Initialize providers and tools on-demand
- **Assembly Loading**: Minimal assembly loading at startup
- **Configuration Caching**: Cache parsed configuration for subsequent runs

### 8.2 Memory Management

- **Streaming**: Stream large file operations
- **Disposal**: Proper IDisposable implementation for resources
- **Weak References**: For large conversation histories

## 9. Extensibility Points

### 9.1 Plugin Architecture

- **Provider Plugins**: Custom AI provider implementations
- **Tool Plugins**: Additional built-in tools via MEF composition
- **Configuration Sources**: Custom configuration providers

### 9.2 MCP Integration

- **Dynamic Loading**: Runtime discovery of MCP servers
- **Version Compatibility**: Graceful handling of MCP version differences
- **Tool Isolation**: Sandboxed execution of MCP tools

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

## 13. Microsoft AI Ecosystem Integration Benefits

### 13.1 Microsoft.Extensions.AI Advantages

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

### 13.2 Semantic Kernel Benefits

**AI Orchestration**:
- Native function calling support aligns perfectly with our tool system
- Built-in conversation state management for resume functionality
- Prompt template management enhances our markdown workflows
- Plugin architecture simplifies tool integration

**Tool Integration**:
- Built-in tools become Semantic Kernel plugins with minimal code
- MCP tools can be dynamically registered as SK functions
- Automatic parameter validation and type conversion
- Consistent tool calling interface across all tools

**Conversation Management**:
- Built-in conversation history and state management
- Automatic prompt optimization and token management
- Native support for multi-turn conversations
- Progress tracking becomes simpler with SK's state management

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

### 13.4 Architecture Simplification

**Before (Custom Implementation)**:
- Custom AI provider abstractions (~500 lines)
- Custom tool calling implementation (~800 lines)
- Custom MCP protocol implementation (~1200 lines)
- Custom conversation state management (~400 lines)
- **Total**: ~2900 lines of infrastructure code

**After (Microsoft AI Ecosystem)**:
- Provider configuration (~50 lines)
- Semantic Kernel setup (~100 lines)
- MCP integration (~150 lines)
- Progress tracking integration (~100 lines)
- **Total**: ~400 lines of infrastructure code

**Code Reduction**: ~85% reduction in infrastructure code
**Maintenance Burden**: Significant reduction as Microsoft maintains the core libraries
**Reliability**: Battle-tested libraries used by enterprise applications

## 14. Updated Implementation Roadmap

### Phase 1: Foundation (MVP) - Enhanced
1. Microsoft.Extensions.AI provider configuration
2. Basic CLI structure with System.CommandLine
3. Semantic Kernel integration and setup
4. Workflow parsing (YAML frontmatter + Markdown)

### Phase 2: Core Functionality - Simplified
1. Built-in tools as Semantic Kernel plugins
2. Workflow execution via Semantic Kernel orchestration
3. Progress tracking with SK conversation state
4. Resume functionality leveraging SK state management

### Phase 3: Extensibility - Streamlined
1. Additional AI providers via Microsoft.Extensions.AI
2. MCP server integration via C# MCP SDK
3. Sub-workflow composition through SK planning
4. Advanced middleware via Microsoft.Extensions.AI

### Phase 4: Polish & Optimization - Focused
1. Performance tuning of SK configuration
2. Comprehensive testing with AI ecosystem mocks
3. Documentation and examples using standard patterns
4. Community feedback integration

---

*The integration of Microsoft's AI ecosystem libraries significantly simplifies the architecture while providing enterprise-grade reliability, maintainability, and extensibility. This approach reduces development time, maintenance burden, and leverages battle-tested libraries used across the Microsoft ecosystem.*
