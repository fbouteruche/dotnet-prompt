# .NET Prompt Tool - Product Requirements Document

## 1. Product Vision

### 1.1 Overview
A flexible CLI tool for .NET developers and DevOps engineers to execute agentic AI workflows with maximum customization and composability. The tool enables running AI workflows defined in markdown files with YAML frontmatter, supporting tool calling and complex multi-step operations.

### 1.2 Problem Statement
Existing AI tools lack the flexibility needed by power users who want to:
- Define complex, multi-step AI workflows
- Integrate deeply with .NET project structure and tooling
- Compose workflows from reusable sub-components
- Have full control over AI model selection and tool integration
- Execute workflows with full modification permissions

### 1.3 Target Users
- **Primary**: Users of the `dotnet` CLI (developers + DevOps engineers)
- **Initial Focus**: AI power users who need maximum flexibility
- **Future**: AI beginners with built-in workflow templates

## 2. Core Architecture

### 2.1 Workflow Format
- **File Format**: Markdown files with YAML frontmatter (following [dotprompt format specification](https://google.github.io/dotprompt/reference/frontmatter/))
- **Extension Convention**: `.prompt.md`
- **Frontmatter**: YAML configuration for model selection, tool requirements, execution parameters
- **Content**: Natural language prompts with embedded instructions for AI agents

### 2.2 Tool Integration
- **Built-in Tools**: .NET-aware tools leveraging dotnet CLI knowledge and project structure
- **Custom Tools**: [Model Context Protocol (MCP) Server](https://modelcontextprotocol.info/specification/draft/server/) integration for extensible tool ecosystem
- **Tool Isolation**: Sub-workflows define their own tool dependencies independently

### 2.3 Workflow Composition
- **Single File Execution**: One markdown file = one workflow execution
- **Sub-workflow Support**: Main workflows can reference and execute sub-workflows
- **Parameter Passing**: Sub-workflows accept parameters from parent workflows
- **Context Inheritance**: Sub-workflows inherit execution context but maintain independent tool definitions

### 2.4 Distribution Model
- **Global .NET Tool**: `dotnet tool install -g dotnet-prompt`
- **Cross-platform**: Support Windows, macOS, Linux
- **Version Management**: Standard .NET tool versioning and update mechanisms

## 3. Functional Requirements

### 3.1 Core Commands

#### 3.1.1 Workflow Execution
```bash
dotnet prompt run workflow.prompt.md                    # Use current folder as context
dotnet prompt run workflow.prompt.md --context ./src    # Specify context directory
dotnet prompt run workflow.prompt.md --project MyApp.csproj # Target specific project
dotnet prompt run workflow.prompt.md --provider openai  # Override AI provider
dotnet prompt run workflow.prompt.md --verbose          # Enable verbose output
```

#### 3.1.2 Dependency Management
```bash
dotnet prompt restore                                    # Install MCP server dependencies
```

#### 3.1.3 Workflow Discovery and Validation
```bash
dotnet prompt list                                       # Show available workflows in current directory
dotnet prompt validate workflow.prompt.md               # Check syntax and dependencies
```

#### 3.1.4 Resume Functionality
```bash
dotnet prompt resume workflow.prompt.md                 # Resume from progress.md in current folder
dotnet prompt resume workflow.prompt.md --progress ./custom-progress.md
```

### 3.2 Built-in Tools (MVP)

#### 3.2.1 Project Analysis Tool
- Read and parse .NET project files (.csproj, .sln)
- Analyze project dependencies and package references
- Extract project metadata (target framework, project type)
- Discover project structure and file organization
- Read source code files with .NET syntax awareness

#### 3.2.2 Build & Test Tool
- Execute `dotnet build` commands with parameter passing
- Run `dotnet test` with filtering and output capture
- Execute `dotnet publish` operations
- Capture and parse build/test output for workflow decisions
- Handle build errors and provide structured feedback

### 3.3 Model Selection and Provider Integration

#### 3.3.1 Provider Support
- **GitHub Models**: Default provider with GitHub CLI authentication integration
- **OpenAI**: Direct API integration with configurable endpoints
- **Azure OpenAI**: Azure-specific endpoints with region support
- **Anthropic**: Claude model family support
- **Local Models**: Support for locally hosted models (default: Ollama)
- **Extensible**: Plugin architecture for additional providers

#### 3.3.2 Model Configuration Hierarchy
1. **CLI Provider Override**: `--provider` flag specifies runtime provider
2. **Frontmatter Model**: `model:` field in workflow YAML frontmatter
3. **Local Configuration**: `.dotnet-prompt/config.json` in working directory
4. **Global Configuration**: `~/.dotnet-prompt/config.json` in user profile

#### 3.3.3 Provider Resolution Logic
- No `--provider` flag: Default to GitHub Models
- Sub-workflows inherit parent workflow provider
- Model validation at both runtime and validation command execution
- GitHub CLI authentication integration with clear error messages

### 3.4 MCP Server Integration

#### 3.4.1 Dependency Declaration
- Workflows declare MCP server requirements in frontmatter `mcp:` array
- `dotnet prompt restore` reads metadata and generates `mcp.json` configuration
- Support for MCP server discovery and connection management

#### 3.4.2 Tool Execution
- Dynamic tool registration from connected MCP servers
- Tool call routing to appropriate MCP server instances
- Error handling and retry logic for MCP server communication

### 3.5 Configuration Management

#### 3.5.1 Global Configuration (`~/.dotnet-prompt/config.json`)
```json
{
  "providers": {
    "github": {
      "endpoint": "https://models.inference.ai.azure.com",
      "token": "github_pat_xxxxx"
    },
    "openai": {
      "api_key": "sk-xxxxx"
    },
    "azure": {
      "endpoint": "https://myinstance.openai.azure.com",
      "api_key": "xxxxx",
      "api_version": "2024-02-01"
    },
    "local": {
      "endpoint": "http://localhost:11434",
      "provider": "ollama"
    }
  },
  "default_provider": "github",
  "default_model": "gpt-4o",
  "verbose": false
}
```

#### 3.5.2 Local Configuration (`.dotnet-prompt/`)
- `mcp.json`: Generated MCP server configurations  
- `config.json`: Local overrides for providers and models in working directory
- Workflow template storage for directory-specific reusable workflows

#### 3.5.3 Workflow Configuration (Frontmatter)
```yaml
---
model: "gpt-4o"
temperature: 0.7
max_tokens: 2000
tools:
  - name: "dotnet-analyzer"
  - name: "file-operations"
mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
  - server: "git-mcp"
    version: "2.1.0"
---
```

### 3.6 Resume and Progress Management

#### 3.6.1 Progress Persistence
- Automatic generation of `progress.md` file on workflow failure
- Complete conversation history preservation
- Tool call results and intermediate state capture
- Workflow execution context preservation

#### 3.6.2 Resume Functionality
- Resume from automatically generated progress files
- Support for custom progress file locations
- Continuation of conversation state with full context
- Incremental progress tracking for long-running workflows

## 4. Non-Functional Requirements

### 4.1 Security and Trust Model
- **Full Trust Execution**: Workflows can modify any accessible files and execute any commands
- **User Responsibility**: No automatic safety mechanisms or permission restrictions
- **MCP Tool Assumptions**: Assume developers understand capabilities of MCP tools they include
- **Authentication Security**: Secure storage of API keys and tokens in configuration files

### 4.2 Performance Requirements
- **Startup Time**: Tool initialization under 2 seconds
- **Workflow Parsing**: Frontmatter and markdown parsing under 500ms for typical workflows
- **Tool Call Latency**: Minimal overhead for built-in tool execution
- **Memory Usage**: Efficient handling of large codebases and long conversations

### 4.3 User Experience
- **Output Mode**: Quiet by default, showing tool calls and errors only
- **Verbose Mode**: `--verbose` flag enables detailed execution logging
- **Error Messages**: Clear, actionable error messages with suggested solutions
- **Progress Indication**: Real-time feedback for long-running operations

### 4.4 Compatibility
- **.NET Versions**: Support .NET 8.0 and later
- **Operating Systems**: Windows, macOS, Linux
- **Terminal Compatibility**: PowerShell, bash, zsh, cmd
- **File System**: Cross-platform path handling and file operations

## 5. Success Metrics

### 5.1 Primary Metrics
- **Installation Count**: Number of `dotnet tool install` executions
- **Community Contributions**: User-contributed workflow templates and MCP servers
- **Workflow Complexity**: Average number of tool calls per workflow execution
- **Resume Usage**: Frequency of workflow resumption indicating complex workflow adoption

### 5.2 Quality Metrics
- **Error Rate**: Percentage of workflow executions that fail due to tool issues
- **Authentication Success**: GitHub CLI integration success rate
- **Provider Reliability**: Model provider connection and execution success rates

## 6. Launch Strategy

### 6.1 Release Phases
1. **Internal Release**: Microsoft and GitHub teams for validation and feedback
2. **Limited Public Alpha**: Selected power users and early adopters
3. **Public Release**: Full .NET community availability through NuGet

### 6.2 Documentation Deliverables
- **Workflow Authoring Guide**: Comprehensive guide to markdown + frontmatter syntax
- **Built-in Tool Reference**: Complete API documentation for Project Analysis and Build & Test tools
- **MCP Integration Examples**: Sample workflows demonstrating custom tool integration
- **Best Practices Guide**: Recommendations for complex workflow design and composition

### 6.3 Example Workflows (MVP)
- **Unit Test Generation**: Analyze code and generate comprehensive unit tests
- **API Documentation**: Extract API endpoints and generate OpenAPI/Swagger documentation
- **Code Quality Analysis**: Analyze code patterns and suggest improvements with automated fixes

## 7. Future Roadmap

### 7.1 Additional Built-in Tools (Post-MVP)
- **Code Operations**: Generate, modify, and refactor source code files
- **Package Management**: Add/remove NuGet packages with dependency analysis
- **Git Integration**: Commit changes, branch management, and automated PR creation

### 7.2 Advanced Features
- **Workflow Templates**: Built-in templates for common development tasks
- **Interactive Mode**: Step-by-step workflow execution with user confirmation
- **Parallel Execution**: Concurrent sub-workflow execution for improved performance
- **Workflow Sharing**: Community marketplace for workflow templates

### 7.3 Enterprise Features
- **Team Configuration**: Shared configuration management for development teams
- **Audit Logging**: Detailed execution logs for compliance and debugging
- **Integration APIs**: REST APIs for CI/CD pipeline integration
- **Custom Provider Support**: Enterprise-specific AI model provider integration

## 8. Technical Constraints

### 8.1 Dependencies
- **.NET Runtime**: Requires .NET 8.0 or later for execution
- **GitHub CLI**: Required for default GitHub Models provider authentication
- **MCP Servers**: External dependency for custom tool functionality
- **AI Provider APIs**: Network connectivity required for model access

### 8.2 Limitations
- **Offline Mode**: Fully supported through local providers (default: Ollama)
- **File Size**: Large file processing limited by AI model context windows
- **Concurrent Execution**: Single workflow execution per tool instance
- **Platform Specific**: Some MCP servers may have platform-specific requirements

## 9. Risk Assessment

### 9.1 Technical Risks
- **AI Model Availability**: Dependency on external AI service reliability
- **MCP Server Compatibility**: Risk of breaking changes in MCP server implementations
- **Authentication Changes**: GitHub CLI authentication flow modifications

### 9.2 Mitigation Strategies
- **Provider Fallbacks**: Multiple AI provider support reduces single-point-of-failure
- **Version Pinning**: MCP server version specifications in workflow frontmatter
- **Error Recovery**: Robust resume functionality handles temporary failures
- **Documentation**: Clear setup instructions reduce user configuration issues

---

*This requirements document represents the comprehensive product specification for the .NET Prompt Tool as defined through collaborative product planning sessions.*
