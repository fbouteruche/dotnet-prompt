# GitHub Copilot Instructions for dotnet-prompt

## Project Overview

dotnet-prompt is a CLI tool for .NET developers to execute AI-powered workflows using markdown files with YAML frontmatter. The project follows Clean Architecture principles with comprehensive Microsoft Semantic Kernel integration.

## Core Architecture Principles

### 1. Semantic Kernel First Approach
- **Always use Semantic Kernel (SK) patterns** for AI orchestration, function calling, and conversation management
- **Leverage Microsoft.Extensions.AI** for unified provider abstractions instead of custom implementations
- **Use SK plugins** for all tool implementations (built-in and MCP)
- **Apply SK filters and middleware** for cross-cutting concerns (error handling, logging, performance)
- **Utilize SK Vector Stores** for caching, memory, and conversation persistence

### 2. Clean Architecture Layers
```
CLI Layer (DotnetPrompt.Cli)
├── Application Layer (DotnetPrompt.Application) - SK orchestration
├── Domain Layer (DotnetPrompt.Core) - Pure business logic
└── Infrastructure Layer (DotnetPrompt.Infrastructure) - SK services
```

### 3. Dependency Injection Patterns
- Use `Microsoft.Extensions.DependencyInjection` throughout
- Register SK services with proper scoping (singleton for kernels, scoped for execution)
- Inject `IKernel`, `IChatCompletionService`, and SK abstractions
- Use configuration pattern with `IOptions<T>` for settings

## Code Generation Guidelines

### Semantic Kernel Integration

When generating AI-related code:

```csharp
// ✅ Correct: Use SK function attributes
[KernelFunction("analyze_project")]
[Description("Analyzes a .NET project structure and dependencies")]
public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
    [Description("Path to the project file")] string projectPath,
    CancellationToken cancellationToken = default)

// ❌ Avoid: Custom function calling implementations
```

When implementing tool plugins:
```csharp
// ✅ Correct: SK plugin with comprehensive annotations
public class ProjectAnalysisPlugin
{
    private readonly ILogger<ProjectAnalysisPlugin> _logger;
    private readonly IVectorStore _vectorStore;
    
    [KernelFunction("analyze_project")]
    [Description("Comprehensive .NET project analysis")]
    public async Task<ProjectAnalysisResult> AnalyzeAsync(...)
}

// ❌ Avoid: Custom tool execution frameworks
```

### Configuration Management

Use hierarchical configuration with proper typing:

```csharp
// ✅ Correct: Strongly typed configuration
public class ProjectAnalysisOptions
{
    public bool IncludePrivateMembers { get; set; } = false;
    public long MaxFileSizeBytes { get; set; } = 1024 * 1024;
    public string[] ExcludedDirectories { get; set; } = { "bin", "obj", ".git" };
}

// ✅ Correct: Configuration injection
public ProjectAnalysisPlugin(IOptions<ProjectAnalysisOptions> options)
{
    _options = options.Value;
}
```

### Error Handling with SK Filters

```csharp
// ✅ Correct: SK filter for error handling
public class WorkflowExecutionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (IsRetryableError(ex))
        {
            await HandleRetryableError(context, ex);
            throw;
        }
    }
}

// ❌ Avoid: Custom exception handling frameworks
```

## Naming Conventions

### Project Structure
- `DotnetPrompt.Cli` - CLI entry point and commands
- `DotnetPrompt.Core` - Domain models and business logic  
- `DotnetPrompt.Application` - Use cases and SK orchestration
- `DotnetPrompt.Infrastructure` - External integrations and SK services
- `DotnetPrompt.Shared` - Cross-cutting utilities

### Class Naming
- Commands: `{Verb}Command` (e.g., `RunCommand`, `ValidateCommand`)
- Handlers: `{UseCase}Handler` (e.g., `ExecuteWorkflowHandler`)
- Plugins: `{Tool}Plugin` (e.g., `ProjectAnalysisPlugin`, `BuildTestPlugin`)
- Services: `I{Service}Service` / `{Service}Service` (e.g., `IWorkflowService`)
- Options: `{Component}Options` (e.g., `ProjectAnalysisOptions`)

### Method Naming
- Async methods: `{Action}Async` (e.g., `AnalyzeProjectAsync`)
- SK functions: Use snake_case for function names (e.g., `analyze_project`)
- Boolean methods: `Is{Condition}`, `Has{Property}`, `Can{Action}`

## Technology Stack Requirements

### Primary Dependencies
```xml
<!-- Core Framework -->
<PackageReference Include="Microsoft.SemanticKernel" />
<PackageReference Include="Microsoft.Extensions.AI" />
<PackageReference Include="System.CommandLine" />

<!-- SK Extensions -->
<PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" />
<PackageReference Include="Microsoft.Extensions.VectorData.Abstractions" />

<!-- Supporting Libraries -->
<PackageReference Include="MediatR" />
<PackageReference Include="Serilog" />
<PackageReference Include="YamlDotNet" />
```

### Avoid Custom Implementations
- ❌ Custom AI provider abstractions (use Microsoft.Extensions.AI)
- ❌ Custom function calling (use SK function calling)
- ❌ Custom conversation management (use SK ChatHistory)
- ❌ Custom caching (use SK Vector Store)
- ❌ Custom retry logic (use SK filters)

## Workflow File Format

When generating workflow examples or parsing logic, follow dotprompt specification:

```yaml
---
# Standard dotprompt fields
name: "workflow-name"
model: "gpt-4o"
tools: ["project-analysis", "build-test"]

config:
  temperature: 0.7
  maxOutputTokens: 4000

# dotnet-prompt extensions (namespaced)
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"

dotnet-prompt.sub-workflows:
  - name: "analysis"
    path: "./analysis/detailed.prompt.md"
---

# Natural language workflow content
Analyze the project and generate documentation...
```

## Built-in Tools Standards

### Tool Implementation Pattern
```csharp
public class {Tool}Plugin
{
    private readonly ILogger<{Tool}Plugin> _logger;
    private readonly IOptions<{Tool}Options> _options;
    
    [KernelFunction("{function_name}")]
    [Description("Clear description of functionality")]
    public async Task<ToolResponse<TResult>> {Function}Async(
        [Description("Parameter description")] TParam parameter,
        CancellationToken cancellationToken = default)
    {
        // Implementation with proper error handling
        // Return ToolResponse<TResult> for consistency
    }
}
```

### Unified Response Format
```csharp
public class ToolResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public ToolSource Source { get; set; } // BuiltIn, MCP, External
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

## Testing Patterns

### Unit Tests
```csharp
// Use xUnit with FluentAssertions
public class ProjectAnalysisPluginTests
{
    [Fact]
    public async Task AnalyzeProjectAsync_ValidPath_ReturnsAnalysis()
    {
        // Arrange
        var plugin = CreatePlugin();
        
        // Act
        var result = await plugin.AnalyzeProjectAsync("./test.csproj");
        
        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
}
```

### Integration Tests
- Use TestContainers for external dependencies
- Mock SK services appropriately
- Test workflow execution end-to-end

## Security Considerations

### Input Validation
```csharp
// ✅ Validate file paths
if (!Path.IsPathRooted(projectPath))
{
    throw new ArgumentException("Project path must be absolute", nameof(projectPath));
}

// ✅ Check file existence before operations
if (!File.Exists(configPath))
{
    throw new FileNotFoundException($"Configuration file not found: {configPath}");
}
```

### Secure Configuration
- Never hardcode secrets in source code
- Use Azure Identity for authentication when possible
- Encrypt sensitive configuration data
- Implement proper credential management

## Performance Guidelines

### SK Performance Patterns
```csharp
// ✅ Use SK Vector Store for caching
var collection = _vectorStore.GetCollection<string, CachedResult>("cache");
var cached = await collection.GetAsync(cacheKey);

// ✅ Leverage SK's built-in performance optimizations
var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    Temperature = 0.7
};
```

### Memory Management
- Dispose kernels properly in long-running scenarios
- Use streaming for large file operations
- Implement proper cancellation token support
- Cache frequently accessed data appropriately

## Documentation Standards

### XML Documentation
```csharp
/// <summary>
/// Analyzes a .NET project and extracts comprehensive metadata.
/// </summary>
/// <param name="projectPath">Absolute path to the project file</param>
/// <param name="cancellationToken">Cancellation token for operation cancellation</param>
/// <returns>Comprehensive project analysis results</returns>
/// <exception cref="FileNotFoundException">Thrown when project file is not found</exception>
[KernelFunction("analyze_project")]
public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(...)
```

### README Sections
When updating documentation, maintain these sections:
- Overview and key features
- Quick start guide
- Architecture summary
- Configuration examples
- Use case scenarios
- Development status

## Common Patterns to Follow

### Service Registration
```csharp
// Program.cs or ServiceCollectionExtensions
services.AddScoped<IWorkflowService, WorkflowService>();
services.AddSingleton<IKernelFactory, KernelFactory>();
services.AddOptions<ProjectAnalysisOptions>()
    .BindConfiguration("ToolConfiguration:ProjectAnalysis");
```

### Logging
```csharp
// Use structured logging with Serilog
_logger.LogInformation("Analyzing project {ProjectPath} with options {@Options}", 
    projectPath, analysisOptions);

// Include correlation IDs for workflow tracking
using var activity = Activity.StartActivity("project-analysis");
activity?.SetTag("project.path", projectPath);
```

### Configuration Hierarchy
```csharp
// Respect the 4-level hierarchy: CLI → Frontmatter → Project → Global
public static PromptConfiguration ResolveConfiguration(
    string? cliProvider = null,           // 1. CLI override
    string? frontmatterModel = null,      // 2. Workflow frontmatter  
    string? projectConfigPath = null,     // 3. Project config
    string? globalConfigPath = null)      // 4. Global config
```

## Anti-Patterns to Avoid

### Don't Implement These
- ❌ Custom AI provider HTTP clients
- ❌ Custom function calling mechanisms  
- ❌ Custom conversation state management
- ❌ Custom prompt template engines
- ❌ Custom retry and circuit breaker logic
- ❌ Custom vector storage implementations
- ❌ Custom telemetry and observability

### Architecture Anti-Patterns
- ❌ Direct coupling between CLI and Infrastructure layers
- ❌ Business logic in controllers/commands
- ❌ Hardcoded configuration values
- ❌ Synchronous I/O operations
- ❌ Missing cancellation token support
- ❌ Improper exception handling

## Environment Variables

Support these environment variables:
- `DOTNET_PROMPT_PROVIDER` - Default AI provider
- `DOTNET_PROMPT_CONFIG` - Configuration file path
- `DOTNET_PROMPT_VERBOSE` - Enable verbose logging
- `DOTNET_PROMPT_CACHE_DIR` - Cache directory override

## File Organization

### Directory Structure
```
src/
├── DotnetPrompt.Cli/           # CLI commands and entry point
├── DotnetPrompt.Core/          # Domain models and interfaces
├── DotnetPrompt.Application/   # Use cases and SK orchestration  
├── DotnetPrompt.Infrastructure/ # SK services and external APIs
└── DotnetPrompt.Shared/        # Utilities and extensions

docs/                           # Comprehensive documentation
tests/                          # Unit, integration, and acceptance tests
.github/                        # GitHub workflows and templates
```

### Namespace Conventions
- `DotnetPrompt.Cli.Commands` - CLI command implementations
- `DotnetPrompt.Core.Domain` - Domain entities and value objects
- `DotnetPrompt.Application.Handlers` - MediatR handlers
- `DotnetPrompt.Infrastructure.Plugins` - SK plugin implementations

## Target .NET Version
- Primary: .NET 8.0 (LTS)
- Language: C# 12 with latest features
- Framework: Cross-platform (Windows, macOS, Linux)

---

When contributing to dotnet-prompt, follow these guidelines to ensure consistency with the project's architecture and goals. The emphasis is on leveraging Microsoft's AI ecosystem (Semantic Kernel, Extensions.AI) rather than building custom implementations.
