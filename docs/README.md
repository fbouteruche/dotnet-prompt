# dotnet-prompt Documentation

Welcome to the comprehensive documentation for **dotnet-prompt** - a powerful CLI tool for .NET developers to execute AI-powered workflows using markdown files with YAML frontmatter.

## Quick Navigation by User Type

### 👤 End Users & Developers
Start here if you want to use dotnet-prompt for your development workflows:

- **[Getting Started](./user-guide/getting-started.md)** - Complete setup tutorial and first workflow
- **[Installation Guide](./user-guide/installation.md)** - Platform-specific installation instructions  
- **[Basic Workflows](./user-guide/basic-workflows.md)** - Simple examples for learning
- **[Advanced Workflows](./user-guide/advanced-workflows.md)** - Complex multi-step scenarios
- **[Dotprompt Format](./user-guide/dotprompt-format.md)** - Complete format specification
- **[MCP Integration](./user-guide/mcp-integration.md)** - External tool integration guide
- **[Troubleshooting](./user-guide/troubleshooting.md)** - Common issues and solutions

### 🔧 CLI Reference
Quick reference for commands and configuration:

- **[CLI Commands](./reference/cli-commands.md)** - Complete command documentation
- **[Configuration Options](./reference/configuration-options.md)** - Configuration hierarchy and options
- **[Built-in Tools](./reference/built-in-tools.md)** - Framework and tool specifications

### 🏗️ Architects & Technical Decision Makers
Understanding the system design and capabilities:

- **[Architecture Guide](./architecture.md)** - Technical architecture details and design decisions
- **[Product Requirements](./requirements.md)** - Complete feature specification and roadmap

### 📋 Technical Specifications
Detailed specifications for implementers and integrators:

- **[CLI Interface Specification](./specifications/cli-interface-specification.md)** - Complete command interface
- **[Workflow Format Specification](./specifications/workflow-format-specification.md)** - dotprompt format details
- **[Workflow Orchestrator Specification](./specifications/workflow-orchestrator-specification.md)** - Execution engine
- **[Configuration System Specification](./specifications/configuration-system-specification.md)** - Configuration hierarchy
- **[MCP Integration Specification](./specifications/mcp-integration-specification.md)** - External tool integration
- **[Workflow Resume System Specification](./specifications/workflow-resume-system-specification.md)** - Resume-only state tracking system
- **[Sub-workflow Composition Specification](./specifications/sub-workflow-composition-specification.md)** - Workflow composition
- **[Error Handling & Logging Specification](./specifications/error-handling-logging-specification.md)** - Error management
- **[Built-in Tools API Specification](./specifications/builtin-tools-api-specification.md)** - Framework and tool APIs

### 🛠️ Tool Developers & Contributors
Documentation for extending dotnet-prompt:

- **[Project Analysis Tool](./developer/project-analysis-tool.md)** - .NET project analysis capabilities
- **[Build & Test Tool](./developer/build-test-tool.md)** - Build and test execution
- **[File System Tool](./developer/file-system-tool.md)** - Secure file operations
- **[File System Tool Examples](./developer/file-system-tool-examples.md)** - Usage examples and patterns
- **[Configuration Guide](./developer/configuration.md)** - Development configuration

### 📁 Examples & Templates
Ready-to-use examples organized by type:

#### Configuration Examples
- **[Minimal Configuration](./examples/configurations/minimal-config.yaml)** - Basic setup
- **[Project Configuration](./examples/configurations/project-config.yaml)** - Project-specific config
- **[Global Configuration](./examples/configurations/global-config.yaml)** - Comprehensive global setup

#### Workflow Examples
- **[Basic Workflows](./examples/workflows/basic-workflows/)** - Simple examples for learning
  - Hello World, Project Summary, Quality Check, README Generator, Dependency Updates
- **[MCP Workflows](./examples/workflows/mcp-workflows/)** - External tool integration examples
  - Enhanced File Operations, Git Analysis, GitHub Health Checks
- **[Real-world Scenarios](./examples/workflows/real-world-scenarios/)** - Production-ready workflows
  - API Documentation, CI/CD Setup, Code Review Automation
- **[Sub-workflows](./examples/workflows/sub-workflows/)** - Workflow composition examples
  - Analysis Phase, Documentation Phase, Main Lifecycle
- **[MCP Integration Example](./examples/workflows/mcp-integration-example.prompt.md)** - Standalone MCP example
- **[Root Sub-workflows](./examples/workflows/root-sub-workflows/)** - Additional composition examples

## Documentation Organization

This documentation is organized into clear categories:

- **user-guide/** - End-user documentation for getting started and using dotnet-prompt
- **reference/** - Quick reference materials for CLI commands and configuration
- **specifications/** - Technical specifications for implementers and integrators  
- **developer/** - Documentation for tool developers and contributors
- **examples/** - Working examples and templates organized by type
  - **configurations/** - Configuration file examples
  - **workflows/** - Workflow examples organized by complexity and use case

## Contributing to Documentation

When adding new documentation:

1. **User Guides** - Place in `user-guide/` for end-user facing content
2. **Technical Specifications** - Place in `specifications/` for implementer documentation  
3. **Developer Documentation** - Place in `developer/` for contributor and extension content
4. **Examples** - Place in appropriate `examples/` subdirectory
5. **Reference Materials** - Place in `reference/` for quick lookup content

## Getting Help

- **Issues**: Report bugs and feature requests on [GitHub Issues](https://github.com/fbouteruche/dotnet-prompt/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/fbouteruche/dotnet-prompt/discussions)
- **Quick Start**: Begin with the [Getting Started Guide](./user-guide/getting-started.md)

---

**Built with ❤️ for the .NET community**