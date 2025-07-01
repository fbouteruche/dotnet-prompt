---
name: "documentation-generator"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 6000

input:
  default:
    include_api_docs: true
    include_examples: true
    format: "markdown"
  schema:
    project_path:
      type: string
      description: "Path to the .NET project root directory"
      default: "."
    output_directory:
      type: string
      description: "Directory where documentation will be generated"
      default: "./docs"
    include_api_docs:
      type: boolean
      description: "Generate API documentation for public interfaces"
      default: true
    include_examples:
      type: boolean
      description: "Include code examples in documentation"
      default: true
    format:
      type: string
      enum: ["markdown", "html", "docfx"]
      description: "Output format for documentation"
      default: "markdown"

metadata:
  description: "Generate comprehensive documentation for .NET projects"
  author: "dotnet-prompt team"
  version: "1.1.0"
  tags: ["documentation", "api", "readme", "examples"]
---

# .NET Project Documentation Generator

I need to generate comprehensive, professional documentation for the .NET project at `{{project_path}}`.

## Project Overview Analysis

First, analyze the project to understand its purpose and structure:
- Read the project file(s) to understand the project type and configuration
- Identify the main purpose and target audience of the project
- Determine the key features and capabilities provided
- Understand the project's dependencies and technology stack

## README Generation

Create a comprehensive README.md file that includes:

### Project Header
- Clear project title and brief description
- Build status badges (placeholder for CI/CD integration)
- License and version information
- Key technology stack indicators

### Quick Start Section
- Prerequisites and system requirements
- Installation instructions (NuGet, dotnet tool, etc.)
- Simple usage example to get users started immediately
- Link to more detailed documentation

### Features Overview
- List of key features and capabilities
- What problems the project solves
- Target audience and use cases
- Brief comparison with alternatives if applicable

### Installation and Setup
- Detailed installation instructions for different scenarios
- Environment setup requirements
- Configuration options and examples
- Troubleshooting common installation issues

### Usage Guide
- Basic usage examples with code snippets
- Common scenarios and how to handle them
- Configuration options and their effects
- Best practices and recommendations

### Contributing Guidelines
- How to set up development environment
- Coding standards and conventions
- Testing requirements
- Pull request process

## API Documentation

{{#if include_api_docs}}
Generate detailed API documentation:

### Public Interface Documentation
- Document all public classes, interfaces, and methods
- Include parameter descriptions and return value information
- Provide usage examples for complex APIs
- Document any exceptions that may be thrown

### Code Examples
{{#if include_examples}}
- Create practical, real-world usage examples
- Show complete, runnable code snippets
- Include examples for common use cases
- Demonstrate best practices and patterns
{{/if}}

### Configuration Reference
- Document all configuration options and settings
- Provide examples of different configuration scenarios
- Explain the impact of different configuration choices
- Include troubleshooting for configuration issues
{{/if}}

## Architecture Documentation

Create architecture documentation that explains:
- High-level system architecture and design decisions
- Component relationships and dependencies
- Data flow and processing patterns
- Extension points and customization options
- Performance characteristics and limitations

## Development Documentation

Generate developer-focused documentation:

### Development Setup
- Required development tools and versions
- How to clone and build the project locally
- Running tests and validation procedures
- Debugging tips and techniques

### Project Structure
- Explanation of directory organization
- Purpose of each major component or module
- Coding conventions and style guidelines
- File naming and organization patterns

### Testing Guide
- Testing philosophy and approach
- How to run different types of tests
- Writing new tests and test best practices
- Test coverage expectations and tools

## Deployment Documentation

Create deployment and operations documentation:
- Deployment procedures and requirements
- Environment-specific configuration
- Monitoring and logging recommendations
- Backup and recovery procedures
- Performance tuning guidelines

## Output Organization

Save all documentation to `{{output_directory}}` in {{format}} format:

```
{{output_directory}}/
├── README.md                 # Main project README
├── docs/
│   ├── api/                 # API documentation
│   ├── guides/              # User and developer guides  
│   ├── examples/            # Code examples and tutorials
│   ├── architecture/        # Architecture documentation
│   └── deployment/          # Deployment and operations
└── CONTRIBUTING.md          # Contribution guidelines
```

## Quality Assurance

Ensure all generated documentation:
- Uses consistent formatting and style
- Contains accurate and up-to-date information
- Includes working code examples (validated against project)
- Follows documentation best practices
- Is accessible to the target audience
- Contains proper cross-references and navigation

Provide a summary of all generated files and any recommendations for maintaining the documentation.