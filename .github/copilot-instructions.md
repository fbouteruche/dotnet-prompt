# GitHub Copilot Instructions for dotnet-prompt

## ⚠️ CRITICAL: Specification Adherence Policy

**THE SPECIFICATIONS ARE THE SINGLE SOURCE OF TRUTH**

Before implementing ANY code changes or architectural decisions, you MUST:

1. **Reference the Authoritative Documentation**:
   - [`docs/architecture.md`](../docs/architecture.md) - Complete architectural patterns and design decisions
   - [`docs/requirements.md`](../docs/requirements.md) - Product requirements and functional specifications  
   - [`docs/specifications/`](../docs/specifications/) - Detailed technical specifications for all components:
     - [`builtin-tools-api-specification.md`](../docs/specifications/builtin-tools-api-specification.md)
     - [`cli-interface-specification.md`](../docs/specifications/cli-interface-specification.md)
     - [`configuration-system-specification.md`](../docs/specifications/configuration-system-specification.md)
     - [`error-handling-logging-specification.md`](../docs/specifications/error-handling-logging-specification.md)
     - [`mcp-integration-specification.md`](../docs/specifications/mcp-integration-specification.md)
     - [`sub-workflow-composition-specification.md`](../docs/specifications/sub-workflow-composition-specification.md)
     - [`workflow-format-specification.md`](../docs/specifications/workflow-format-specification.md)
     - [`workflow-orchestrator-specification.md`](../docs/specifications/workflow-orchestrator-specification.md)
     - [`workflow-resume-system-specification.md`](../docs/specifications/workflow-resume-system-specification.md)

2. **Validate Against Specifications**: If a user request conflicts with established specifications:
   - **STOP** and inform the user of the conflict
   - **REFERENCE** the specific specification(s) that conflict
   - **REQUEST** explicit confirmation to proceed with specification changes
   - **EXAMPLE**: "This request conflicts with the CLI Interface Specification (docs/specifications/cli-interface-specification.md) which defines... Do you want me to update the specification first?"

3. **Maintain Specification Consistency**: When user confirms changes that conflict with specifications:
   - **FIRST** update the relevant specification files in `docs/specifications/`
   - **THEN** update `docs/architecture.md` and `docs/requirements.md` if needed
   - **FINALLY** implement the code changes aligned with updated specifications
   - **VERIFY** all specification cross-references remain consistent

4. **Code-Specification Alignment Rule**: 
   - **All generated code MUST align with current specifications**
   - **Specifications are updated BEFORE code implementation**
   - **No exceptions to this rule - specifications drive implementation**

## Project Overview

dotnet-prompt is a CLI tool for .NET developers to execute AI-powered workflows using markdown files with YAML frontmatter. The project follows Clean Architecture principles with comprehensive Microsoft Semantic Kernel integration.

**For complete project details, see [`docs/requirements.md`](../docs/requirements.md) and [`docs/architecture.md`](../docs/architecture.md)**.

## Core Architecture Principles

**Complete architectural guidance is defined in [`docs/architecture.md`](../docs/architecture.md)**

### Key Principles Summary (see architecture.md for full details):

1. **Semantic Kernel First Approach** - Always use SK patterns for AI orchestration
2. **Clean Architecture Layers** - Strict separation between CLI, Application, Domain, and Infrastructure
3. **Microsoft.Extensions.AI Integration** - Unified provider abstractions
4. **File-Based Progress Management** - Resume functionality via progress files

### Architecture Validation Checklist:
- ✅ Does this follow the layer separation defined in `docs/architecture.md`?
- ✅ Does this use SK patterns as specified in the architecture?
- ✅ Does this align with the technology stack in `docs/architecture.md`?

## CLI Output Patterns

**Complete CLI output and error handling patterns are specified in [`docs/specifications/error-handling-logging-specification.md`](../docs/specifications/error-handling-logging-specification.md)**

### Hybrid Logging Architecture (Industry Standard)

The CLI tool implements a **dual-output pattern** that separates user-facing communication from operational telemetry:

```csharp
// ✅ User-Facing Output (Console.WriteLine/Error.WriteLine)
Console.WriteLine("✓ Configuration is valid");
Console.Error.WriteLine("Error: Workflow file not found");

// ✅ Structured Logging (ILogger) 
_logger.LogInformation("Workflow completed successfully in {ExecutionTime}", result.ExecutionTime);
_logger.LogError("Workflow execution failed: {ErrorMessage}", result.ErrorMessage);
```

**Refer to the Error Handling & Logging Specification for complete implementation patterns and requirements.**

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

**Complete configuration system specification is defined in [`docs/specifications/configuration-system-specification.md`](../docs/specifications/configuration-system-specification.md)**

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

**Refer to the Configuration System Specification for the complete 4-level hierarchy, provider resolution logic, and configuration file schemas.**

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

<!-- MCP Integration (Official SDK) -->
<PackageReference Include="ModelContextProtocol" />

<!-- Supporting Libraries -->
<PackageReference Include="MediatR" />
<PackageReference Include="Serilog" />
<PackageReference Include="YamlDotNet" />
```

### API References
- [Microsoft Semantic Kernel API Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel?view=semantic-kernel-dotnet) - Complete API reference for all Semantic Kernel classes, interfaces, and methods

### MCP Integration Guidelines
- **Use Official MCP SDK**: Always use the official `ModelContextProtocol` NuGet package
- **API Reference**: https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html
- **GitHub Repository**: https://github.com/modelcontextprotocol/csharp-sdk
- **SK Integration**: MCP tools (`McpClientTool`) inherit from `AIFunction` and work directly with SK
- **Transport Layer**: Use `McpClientFactory.CreateAsync()` with `StdioClientTransport` or `SseClientTransport`
- **Enterprise Features**: Preserve configuration-driven server management through custom mapping layers

### Avoid Custom Implementations
- ❌ Custom AI provider abstractions (use Microsoft.Extensions.AI)
- ❌ Custom function calling (use SK function calling)
- ❌ Custom conversation management (use SK ChatHistory)
- ❌ Custom caching (use SK Vector Store)
- ❌ Custom retry logic (use SK filters)

## Workflow File Format

**Complete workflow format specification is defined in [`docs/specifications/workflow-format-specification.md`](../docs/specifications/workflow-format-specification.md)**

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

**Refer to the Workflow Format Specification for complete YAML schema, validation rules, and extension patterns.**

## Built-in Tools Standards

**Complete built-in tools API specification is defined in [`docs/specifications/builtin-tools-api-specification.md`](../docs/specifications/builtin-tools-api-specification.md)**

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

**Refer to the Built-in Tools API Specification for complete interface definitions, error handling patterns, and tool registration requirements.**

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
// Complete hierarchy defined in docs/specifications/configuration-system-specification.md
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

### C# File Organization Conventions

Follow standard C# conventions for type organization:

```csharp
// ✅ Correct: Each public type in its own file
// File: ProjectAnalysisPlugin.cs
public class ProjectAnalysisPlugin
{
    // Implementation
}

// File: ProjectAnalysisOptions.cs  
public class ProjectAnalysisOptions
{
    // Implementation
}

// File: IProjectAnalysisService.cs
public interface IProjectAnalysisService
{
    // Interface definition
}
```

**File Organization Rules:**
- **One public type per file** - Each public class, interface, struct, enum, or delegate should have its own file
- **File name matches type name** - `ProjectAnalysisPlugin.cs` contains `ProjectAnalysisPlugin` class
- **Private/internal types exception** - Small private or internal helper types may be co-located in the same file as their consuming public type
- **Partial classes** - Use separate files for each logical grouping (e.g., `MyClass.Core.cs`, `MyClass.Extensions.cs`)
- **Nested types** - Public nested types should be in the same file as their containing type

**Examples:**
```csharp
// ✅ Correct: Small internal helpers with main type
// File: WorkflowService.cs
public class WorkflowService : IWorkflowService
{
    // Main implementation
}

internal class WorkflowHelper // ✅ Internal helper can stay
{
    // Helper implementation
}

// ❌ Avoid: Multiple public types in one file
// File: Services.cs - DON'T DO THIS
public class WorkflowService { }
public class ConfigurationService { } // ❌ Should be in separate file
public interface IWorkflowService { } // ❌ Should be in separate file
```

## Target .NET Version
- Primary: .NET 8.0 (LTS)
- Language: C# 12 with latest features
- Framework: Cross-platform (Windows, macOS, Linux)

## Documentation Organization Standards

### Directory Structure Rules
The documentation follows a strict organizational hierarchy:

```
docs/
├── README.md                    # Comprehensive navigation index
├── architecture.md              # Technical architecture details  
├── requirements.md              # Product requirements and roadmap
├── user-guide/                  # End-user documentation
├── reference/                   # Quick reference materials
├── specifications/              # Technical specifications for implementers
├── developer/                   # Tool developer and contributor docs
└── examples/                    # Working examples and templates
    ├── configurations/          # Configuration file examples
    └── workflows/               # Workflow examples by category
```

### File Placement Guidelines

**User-Facing Documentation** (`user-guide/`):
- Getting started tutorials and installation guides
- Basic and advanced workflow examples  
- Format specifications and integration guides
- Troubleshooting and FAQ content
- Target audience: End users learning to use dotnet-prompt

**Technical Specifications** (`specifications/`):
- Detailed implementation specifications for all components
- API specifications and interface contracts
- Technical requirements and design constraints
- Target audience: Implementers, integrators, and technical decision makers

**Developer Documentation** (`developer/`):
- Tool development guides and API documentation
- Configuration guides for development scenarios
- Contributor guidelines and extension patterns
- Target audience: Tool developers and contributors

**Reference Materials** (`reference/`):
- CLI command reference and quick lookup guides
- Configuration option references
- Built-in tool API references
- Target audience: Users needing quick reference information

**Examples and Templates** (`examples/`):
- Working configuration examples in `configurations/`
- Workflow examples organized by category in `workflows/`
- All examples must be functional and tested
- Target audience: Users looking for copy-paste solutions

### Cross-Reference Update Requirements
When moving or creating documentation:

1. **Update Internal Links**: All relative links must reflect new file locations
2. **Update Navigation**: Ensure new content is linked from `docs/README.md`
3. **Update Project README**: Update main project README links if needed
4. **Validate Links**: Test all documentation links after changes

### Common Link Update Patterns
```markdown
# Before restructuring
[CLI Specification](./cli-interface-specification.md)
[Example](../examples/basic-workflow.md)

# After restructuring  
[CLI Specification](./specifications/cli-interface-specification.md)
[Example](../examples/workflows/basic-workflows/basic-workflow.md)
```

### Forbidden Documentation Patterns
- ❌ Specification files in root `docs/` directory
- ❌ Tool documentation mixed with user guides
- ❌ Examples scattered across multiple directory structures
- ❌ Documentation without clear audience targeting
- ❌ Broken internal links or missing navigation
- ❌ Documentation that doesn't follow the established hierarchy

### Documentation Maintenance
- All new documentation must include proper navigation links
- Cross-references must be updated when files are moved
- Examples must be validated and functional
- Navigation index (`docs/README.md`) must be kept current
- Documentation changes require validation of all affected links

## Prompt Directory Organization Standards

The project maintains two distinct prompt-related directories that serve complementary but separate purposes. **Strict separation must be maintained** to ensure clarity and proper project organization.

### Directory Structure Rules

```
prompts/                         # Development and testing resource
├── README.md                    # Developer-focused documentation
├── testing/                     # Test scenarios and validation prompts
├── development/                 # Development workflow prompts
├── integration/                 # Integration testing prompts
└── experimental/                # Prototyping and R&D prompts

docs/examples/                   # User-facing documentation (existing)
├── configurations/              # Configuration file examples
└── workflows/                   # User workflow examples by category
```

### Purpose and Audience Definitions

**`prompts/` Directory - Development Resource:**
- **Purpose**: Development, testing, and contributor workflows
- **Target Audience**: Developers, contributors, maintainers
- **Content Focus**: Functional testing, feature validation, development scenarios
- **Quality Standards**: Must execute successfully and test specific functionality
- **Maintenance**: Updated during development cycles and feature additions

**`docs/examples/` Directory - User Documentation:**
- **Purpose**: User education, tutorials, and production-ready templates
- **Target Audience**: End users learning to use dotnet-prompt
- **Content Focus**: Copy-paste solutions, learning materials, best practices
- **Quality Standards**: Must be functional, tested, and follow documentation standards
- **Maintenance**: Updated with user feedback and feature releases

### Content Guidelines

**✅ `prompts/` Directory Should Contain:**
- Feature testing and validation prompts
- Development workflow automation
- Integration testing scenarios
- Experimental prototypes and R&D work
- Performance benchmarking prompts
- Regression testing workflows
- Tool plugin testing scenarios
- Complex development use cases

**✅ `docs/examples/` Directory Should Contain:**
- Getting started tutorials and workflows
- Production-ready configuration examples
- Best practice implementations
- Common use case solutions
- Integration pattern demonstrations
- Step-by-step learning materials
- User scenario templates

**❌ Anti-Patterns - Do NOT Place In Either Directory:**
- Temporary or personal development files
- Incomplete or broken prompts
- Experimental code without documentation
- Prompts with hardcoded secrets or credentials
- Platform-specific prompts without cross-platform alternatives

### Cross-Pollination Strategy

**Development → User Examples Flow:**
1. **Validation Phase**: Prompts in `prompts/` must be thoroughly tested
2. **Polish Phase**: Convert development prompts to user-friendly versions
3. **Documentation Phase**: Add comprehensive documentation and context
4. **Migration Phase**: Move polished versions to appropriate `docs/examples/` subdirectory
5. **Maintenance Phase**: Maintain both versions with clear purposes

**User Examples → Development Flow:**
1. **Feedback Collection**: User issues and enhancement requests
2. **Test Case Creation**: Create regression tests in `prompts/testing/`
3. **Feature Development**: Develop new capabilities in `prompts/development/`
4. **Integration Testing**: Validate with real scenarios in `prompts/integration/`

### File Naming Conventions

**`prompts/` Directory:**
- Use descriptive, technical naming: `test-semantic-kernel-integration.prompt.md`
- Include category prefixes: `dev-`, `test-`, `integration-`, `experimental-`
- Focus on functionality: `validate-mcp-client-connection.prompt.md`

**`docs/examples/` Directory:**
- Use user-friendly naming: `getting-started-workflow.prompt.md`
- Include difficulty indicators: `basic-`, `advanced-`, `enterprise-`
- Focus on use cases: `analyze-project-structure.prompt.md`

### Quality Standards

**`prompts/` Directory Requirements:**
- Must execute without errors in development environment
- Include comprehensive YAML frontmatter for testing
- Document expected outcomes and validation criteria
- Include performance and resource usage considerations
- Maintain compatibility with CI/CD testing pipelines

**`docs/examples/` Directory Requirements:**
- Must be functional and tested according to documentation standards
- Include clear descriptions and usage instructions
- Follow established documentation hierarchy and linking
- Provide copy-paste ready solutions with minimal modification
- Include troubleshooting guidance for common issues

### Integration with Project Standards

**Alignment with Clean Architecture:**
- `prompts/` supports development and testing layers
- `docs/examples/` supports user interface and documentation layers
- Both maintain separation of concerns and clear boundaries

**Semantic Kernel Integration:**
- Both directories use consistent SK patterns and conventions
- Development prompts test SK functionality thoroughly
- User examples demonstrate SK best practices

**Configuration Hierarchy Respect:**
- Both directories respect CLI → Frontmatter → Project → Global hierarchy
- Development prompts test configuration precedence
- User examples demonstrate proper configuration usage

### Maintenance Responsibilities

**Contributors Must:**
- Place development and testing content in `prompts/`
- Place user-facing examples in `docs/examples/`
- Update both directories when making related changes
- Validate cross-references between directories
- Follow established quality standards for each directory

**Forbidden Practices:**
- ❌ Mixing development and user content in the same directory
- ❌ Placing untested or experimental content in `docs/examples/`
- ❌ Using `docs/examples/` for development testing
- ❌ Creating duplicate content without clear purpose differentiation
- ❌ Breaking established directory organization standards
- ❌ Ignoring cross-reference updates when moving content

---

When contributing to dotnet-prompt, follow these guidelines to ensure consistency with the project's architecture and goals. The emphasis is on leveraging Microsoft's AI ecosystem (Semantic Kernel, Extensions.AI) rather than building custom implementations.
