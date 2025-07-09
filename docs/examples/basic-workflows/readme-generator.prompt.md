---
name: "basic-readme-generator"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.6
  maxOutputTokens: 4000

input:
  schema:
    include_badges:
      type: boolean
      description: "Include status badges in README"
      default: true
    include_contributing:
      type: boolean
      description: "Include contributing guidelines section"
      default: false
---

# Basic README Generator

Generate a professional README.md file for the current .NET project.

## README Generation Requirements

Create a comprehensive README.md that includes:

### 1. Project Header
- Project name as main title
- Brief, compelling description of what the project does
{{#if include_badges}}
- Status badges (build status, version, license, etc.)
{{/if}}

### 2. Table of Contents
- Quick navigation to all major sections
- Properly linked to sections below

### 3. Overview Section
- Detailed project description
- Key features and capabilities
- Target audience or use cases

### 4. Getting Started
- Prerequisites (required .NET version, tools, etc.)
- Installation instructions
- Quick start example or basic usage

### 5. Usage Examples
- Code examples showing how to use the project
- Common scenarios and their solutions
- API usage if it's a library

### 6. Project Structure
- Brief explanation of major directories and files
- Architecture overview if applicable

### 7. Configuration
- Configuration options if any exist
- Environment setup requirements
- Connection strings or settings explanations

### 8. Development
- How to build the project locally
- How to run tests
- Development workflow guidelines

{{#if include_contributing}}
### 9. Contributing
- Guidelines for contributors
- How to submit issues and pull requests
- Code standards and review process
{{/if}}

### 10. License and Credits
- License information
- Attribution to dependencies or frameworks used
- Contact information or how to get support

## Quality Standards

The README should be:
- **Professional**: Well-formatted and error-free
- **Comprehensive**: Cover all essential information
- **User-friendly**: Easy to follow for developers at different skill levels
- **Accurate**: Reflect the actual current state of the project
- **Actionable**: Include specific commands and examples that work

## Output

- Update the existing README.md or create a new one
- Use proper Markdown formatting with clear headings
- Include code blocks with appropriate syntax highlighting
- Add a generation notice at the bottom indicating it was created with dotnet-prompt

Make the README compelling and informative enough that developers will want to use or contribute to the project.