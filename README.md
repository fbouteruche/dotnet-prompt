# dotnet-prompt

A powerful CLI tool for .NET developers to execute AI-powered workflows using markdown files with YAML frontmatter. Built on Microsoft Semantic Kernel with comprehensive tool integration and workflow composition capabilities.

## 🎯 Overview

dotnet-prompt enables developers to create and execute sophisticated AI workflows that can analyze projects, generate code, run tests, and automate development tasks. It follows the [dotprompt format specification](https://google.github.io/dotprompt/reference/frontmatter/) and integrates seamlessly with the .NET ecosystem.

### Key Features

- **🤖 AI-Powered Workflows**: Execute complex multi-step AI workflows using natural language
- **🔧 Built-in .NET Tools**: Deep integration with .NET project analysis, building, and testing
- **🔌 Extensible Architecture**: Model Context Protocol (MCP) integration for custom tools
- **📝 Standard Format**: Compatible with dotprompt specification for portability
- **🔄 Resume Capability**: Automatically resume interrupted workflows from checkpoints
- **🎛️ Multiple AI Providers**: Support for GitHub Models, OpenAI, Azure OpenAI, Anthropic, and local models

## 🚀 Quick Start

### Installation

```bash
# Install globally as a .NET tool (once published to NuGet)
dotnet tool install -g dotnet-prompt

# Verify installation
dotnet prompt --version
```

### Basic Usage

1. **Create a workflow file** (`analyze-project.prompt.md`):

```yaml
---
model: "gpt-4o"
tools: ["project-analysis", "build-test"]
---

# Project Analysis Workflow

Please analyze the current .NET project and provide a comprehensive report including:
- Project structure and architecture
- Dependencies and potential vulnerabilities  
- Code quality metrics
- Test coverage analysis

Generate documentation and suggest improvements where needed.
```

2. **Execute the workflow**:

```bash
# Basic execution
dotnet prompt run analyze-project.prompt.md

# Dry run (validation only)
dotnet prompt run analyze-project.prompt.md --dry-run

# With verbose output
dotnet prompt run analyze-project.prompt.md --verbose

# With custom context and timeout
dotnet prompt run analyze-project.prompt.md --context ./src --timeout 600
```

### CLI Commands

```bash
# Show help
dotnet prompt --help

# Show version
dotnet prompt --version

# Run a workflow
dotnet prompt run <workflow-file> [options]

# Available run options:
#   --context <path>    Working directory context
#   --dry-run          Validate workflow without execution
#   --timeout <secs>   Execution timeout in seconds
#   --verbose          Enable verbose output
```

### Environment Variables

```bash
# Enable verbose logging
export DOTNET_PROMPT_VERBOSE=true

# Set default timeout
export DOTNET_PROMPT_TIMEOUT=300

# Disable telemetry
export DOTNET_PROMPT_NO_TELEMETRY=true
```

2. **Execute the workflow**:

```bash
dotnet prompt run analyze-project.prompt.md
```

3. **Execute with context**:

```bash
dotnet prompt run analyze-project.prompt.md --context ./src --project MyApp.csproj
```

## 📋 Core Commands

### Workflow Execution
```bash
# Basic execution
dotnet prompt run workflow.prompt.md

# With custom context and parameters
dotnet prompt run workflow.prompt.md --context ./src --parameter environment=staging

# Override AI provider
dotnet prompt run workflow.prompt.md --provider openai --model gpt-4

# Verbose output with timeout
dotnet prompt run workflow.prompt.md --verbose --timeout 600
```

### Dependency Management
```bash
# Install MCP server dependencies
dotnet prompt restore

# Force reinstall with verbose output
dotnet prompt restore --force --verbose
```

### Workflow Discovery & Validation
```bash
# List available workflows
dotnet prompt list --recursive --show-metadata

# Validate workflow syntax and dependencies
dotnet prompt validate workflow.prompt.md --check-dependencies --check-tools
```

### Resume Interrupted Workflows
```bash
# Resume from automatic progress file
dotnet prompt resume workflow.prompt.md

# Resume from custom progress file
dotnet prompt resume workflow.prompt.md --progress ./custom-progress.md
```

### Configuration Management
```bash
# Show current configuration
dotnet prompt config show --effective

# Set global provider
dotnet prompt config set default_provider openai --global

# Initialize project configuration
dotnet prompt init --provider azure
```

## 🏗️ Architecture

dotnet-prompt is built on **Clean Architecture** principles with comprehensive **Semantic Kernel** integration:

```
┌─────────────────────────────────────┐
│           CLI Interface             │
│    (Commands & Option Parsing)      │
├─────────────────────────────────────┤
│        Application Layer            │
│   (SK Orchestration & Use Cases)    │
├─────────────────────────────────────┤
│          Domain Layer               │
│    (Core Business Logic)            │
├─────────────────────────────────────┤
│       Infrastructure Layer          │
│ (AI Providers, MCP, File System)    │
└─────────────────────────────────────┘
```

### Key Technologies
- **Microsoft Semantic Kernel**: AI orchestration and function calling
- **Microsoft.Extensions.AI**: Unified AI provider abstraction
- **System.CommandLine**: Robust CLI functionality
- **MCP (Model Context Protocol)**: Extensible tool ecosystem
- **.NET 8+**: Modern C# and latest language features

## 📁 Workflow Format

Workflows are markdown files with YAML frontmatter following the dotprompt specification:

```yaml
---
# Core Configuration
name: "project-analysis-workflow"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

# Model Settings
config:
  temperature: 0.7
  maxOutputTokens: 4000

# Input Parameters
input:
  schema:
    project_path: 
      type: string
      description: "Path to the .NET project file"
    include_tests:
      type: boolean
      default: true

# dotnet-prompt Extensions
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"

dotnet-prompt.sub-workflows:
  - name: "detailed-analysis"
    path: "./analysis/detailed-analysis.prompt.md"
---

# Natural Language Workflow Content

Analyze the project at `{{project_path}}` and generate comprehensive documentation...
```

## 🔧 Built-in Tools

### Project Analysis Tool
Comprehensive .NET project and solution analysis:
- Project metadata extraction (target frameworks, project type)
- Dependency analysis with vulnerability scanning
- Source code structure and metrics
- Build configuration analysis

```markdown
{{invoke_tool: analyze_project
  project_path: "./MyApp.csproj"
  include_dependencies: true
  include_source_files: true
}}
```

### Build & Test Tool
Execute .NET CLI operations with structured results:
- Build execution with error parsing
- Test running with coverage collection
- Publish operations for deployment

```markdown
{{invoke_tool: build_project
  project_path: "./MyApp.csproj"
  configuration: "Release"
}}

{{invoke_tool: test_project
  project_path: "./MyApp.Tests.csproj"
  collect_coverage: true
}}
```

### File System Tool
Secure file and directory operations:
- Read/write files with encoding support
- Directory listing and creation
- Copy/move operations with safety controls

```markdown
{{invoke_tool: read_file
  file_path: "./appsettings.json"
}}

{{invoke_tool: write_file
  file_path: "./Generated/Config.cs"
  content: "{{generated_code}}"
}}
```

## 🔌 Extensibility

### MCP Server Integration

dotnet-prompt supports Model Context Protocol servers for custom tools:

```json
{
  "mcp_servers": {
    "filesystem-mcp": {
      "command": "npm",
      "args": ["run", "start"],
      "version": "1.0.0"
    },
    "git-mcp": {
      "command": "python",
      "args": ["-m", "git_mcp"],
      "version": "2.1.0"
    }
  }
}
```

### Sub-workflow Composition

Compose complex workflows from reusable components:

```markdown
# Main Workflow

First, perform detailed analysis:
> Execute: ./analysis/project-analysis.prompt.md
> Parameters: project_path="{{project_path}}", depth="comprehensive"

Then generate documentation:
> Execute: ./docs/api-docs.prompt.md
> Parameters: metadata="{{analysis_result}}"
```

## ⚙️ Configuration

Configuration follows a hierarchical merge strategy:

1. **CLI Arguments** (highest priority)
2. **Workflow Frontmatter**
3. **Project Configuration** (`.dotnet-prompt/config.json`)
4. **Global Configuration** (`~/.dotnet-prompt/config.json`)
5. **Default Values**

### Global Configuration Example
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
      "api_key": "xxxxx"
    }
  },
  "default_provider": "github",
  "default_model": "gpt-4o",
  "tool_configuration": {
    "project_analysis": {
      "excluded_directories": ["bin", "obj", ".git"],
      "max_file_size_bytes": 1048576
    }
  }
}
```

## 🔄 Progress & Resume

Workflows automatically create progress files for resumption:

- **Automatic Checkpoints**: Created after each tool execution
- **Conversation State**: Complete AI conversation history preserved
- **Context Restoration**: Full execution context maintained
- **Intelligent Resume**: Validates compatibility before continuing

```bash
# Resume interrupted workflow
dotnet prompt resume long-running-workflow.prompt.md
```

## 🔒 Security & Trust Model

dotnet-prompt operates under a **full trust** security model:

- **Full Permissions**: Can modify any accessible files and execute commands
- **User Responsibility**: No automatic safety restrictions
- **Secure Configuration**: API keys encrypted and stored securely
- **Audit Logging**: Optional comprehensive operation logging

## 🌟 Use Cases

### Code Generation & Analysis
```yaml
---
model: "gpt-4o"
tools: ["project-analysis", "file-system"]
---

# Generate Unit Tests

Analyze the project structure and generate comprehensive unit tests for all public methods in the service layer.
```

### Documentation Generation
```yaml
---
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]
---

# API Documentation Generator

Create comprehensive API documentation including:
- OpenAPI/Swagger specifications
- README with setup instructions
- Code examples and usage patterns
```

### DevOps Automation
```yaml
---
model: "gpt-4o"
tools: ["build-test", "file-system"]
mcp: ["git-mcp", "docker-mcp"]
---

# CI/CD Pipeline Setup

Analyze the project and generate:
- GitHub Actions workflows
- Docker containerization
- Deployment scripts
```

## 🛠️ Development Status

| Component | Status | Description |
|-----------|--------|-------------|
| ✅ Requirements | Complete | Comprehensive product requirements defined |
| ✅ Architecture | Complete | Clean Architecture with SK integration |
| ✅ CLI Interface | Complete | Full command specification |
| ✅ Configuration | Complete | Hierarchical configuration system |
| ✅ Workflow Format | Complete | dotprompt-compatible format |
| 🚧 Built-in Tools | Draft | Core .NET tools specification |
| ✅ MCP Integration | Complete | Model Context Protocol support |
| ✅ Progress/Resume | Complete | Checkpoint and resume capabilities |
| ✅ Error Handling | Complete | Comprehensive error management |

## 📚 Documentation

Comprehensive documentation is available in the `/docs` directory:

- **[Product Requirements](./docs/requirements.md)**: Complete feature specification
- **[Architecture Guide](./docs/architecture.md)**: Technical architecture details
- **[CLI Reference](./docs/cli-interface-specification.md)**: Complete command documentation
- **[Configuration System](./docs/configuration-system-specification.md)**: Configuration hierarchy and options
- **[Workflow Format](./docs/workflow-format-specification.md)**: dotprompt format specification
- **[Built-in Tools](./docs/builtin-tools-api-specification.md)**: Framework and tool specifications
- **[MCP Integration](./docs/mcp-integration-specification.md)**: External tool integration
- **[Progress & Resume](./docs/progress-resume-specification.md)**: Checkpoint system
- **[Error Handling](./docs/error-handling-logging-specification.md)**: Error management and logging

## 🤝 Contributing

dotnet-prompt is designed for extensibility and community contributions:

- **Workflow Templates**: Share reusable workflow patterns
- **MCP Servers**: Create custom tools for specialized domains
- **Built-in Tools**: Contribute core .NET development tools
- **Documentation**: Improve guides and examples

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔨 How to Build and Test

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git

### Building the Project

```bash
# Clone the repository
git clone https://github.com/fbouteruche/dotnet-prompt.git
cd dotnet-prompt

# Restore dependencies and build all projects
dotnet build

# Build in Release configuration
dotnet build --configuration Release
```

### Running Tests

```bash
# Run all tests (unit + integration)
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run only unit tests
dotnet test tests/DotnetPrompt.UnitTests

# Run only integration tests
dotnet test tests/DotnetPrompt.IntegrationTests
```

### Local Development and Testing

```bash
# 1. Build and pack the CLI tool
dotnet pack src/DotnetPrompt.Cli --configuration Release

# 2. Install the tool locally for testing
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli

# 3. Verify installation
dotnet prompt --version

# 4. Test with sample prompts
dotnet prompt run prompts/hello-world.prompt.md --dry-run
```

### Updating Local Installation

```bash
# Uninstall the current version
dotnet tool uninstall -g DotnetPrompt.Cli

# Rebuild and reinstall
dotnet pack src/DotnetPrompt.Cli --configuration Release
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli
```

### Development Workflow

```bash
# Make changes to the code
# ...

# Run tests to ensure changes don't break existing functionality
dotnet test

# Build and test the CLI locally
dotnet build --configuration Release
dotnet pack src/DotnetPrompt.Cli --configuration Release

# Update local installation to test changes
dotnet tool uninstall -g DotnetPrompt.Cli
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli

# Test your changes with sample prompts
dotnet prompt run prompts/hello-world.prompt.md --verbose
```

### Project Structure

```
src/
├── DotnetPrompt.Cli/          # CLI entry point and commands
├── DotnetPrompt.Core/         # Domain models and interfaces
├── DotnetPrompt.Application/  # Application services and use cases
├── DotnetPrompt.Infrastructure/ # External integrations
└── DotnetPrompt.Shared/       # Shared utilities

tests/
├── DotnetPrompt.UnitTests/    # Unit tests for core logic
└── DotnetPrompt.IntegrationTests/ # End-to-end CLI tests

prompts/                       # Sample workflow files for testing
docs/                         # Comprehensive documentation
```

### Troubleshooting

**Tool installation issues:**
```bash
# Check if tool is already installed
dotnet tool list -g

# Clear NuGet cache if needed
dotnet nuget locals all --clear

# Ensure you're using the correct source path
ls src/DotnetPrompt.Cli/bin/Release/
```

**Build issues:**
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Restore packages explicitly
dotnet restore
```

## 🚧 Implementation Roadmap

### Phase 1: Foundation (MVP)
- [x] Core domain models and CLI structure
- [x] Configuration hierarchy implementation
- [x] Workflow parsing and validation
- [ ] Basic execution engine

### Phase 2: Core Functionality  
- [ ] GitHub Models provider implementation
- [ ] Built-in tools (Project Analysis, Build & Test, File System)
- [ ] Progress tracking and resume
- [ ] Basic workflow execution

### Phase 3: Extensibility
- [ ] Additional AI providers (OpenAI, Azure, Anthropic, Local)
- [ ] MCP server integration
- [ ] Sub-workflow composition
- [ ] Advanced error handling

### Phase 4: Polish & Optimization
- [ ] Performance optimizations
- [ ] Comprehensive testing
- [ ] Documentation and examples
- [ ] Community feedback integration

---

**Built with ❤️ for the .NET community**
