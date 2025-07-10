# Basic Workflows

This guide provides simple, practical workflow examples to help you learn the fundamentals of dotnet-prompt. Each example builds on the previous one, teaching core concepts step by step.

## 1. Hello World

The simplest possible workflow - create a file with a greeting.

**File: `hello-world.prompt.md`**
```yaml
---
name: "hello-world"
model: "gpt-4o"
tools: ["file-system"]
---

# Hello World

Create a file called `hello.txt` containing the message "Hello from dotnet-prompt!".
```

**Run it:**
```bash
dotnet prompt run hello-world.prompt.md
```

**What you'll learn:**
- Basic workflow structure
- Using the file-system tool
- Simple natural language instructions

---

## 2. Project Information

Extract basic information about your .NET project.

**File: `project-info.prompt.md`**
```yaml
---
name: "project-info"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3  # Low temperature for factual analysis
---

# Project Information

Analyze the current .NET project and create a summary including:

1. **Project Type**: What kind of project is this? (Console, Web API, Class Library, etc.)
2. **Target Framework**: What .NET version does it target?
3. **Dependencies**: List the main NuGet packages
4. **Structure**: Key directories and files

Save the summary to `project-summary.md` in a clean, readable format.
```

**Run it:**
```bash
dotnet prompt run project-info.prompt.md
```

**What you'll learn:**
- Using the project-analysis tool
- Setting temperature for factual vs creative tasks
- Combining multiple tools in one workflow

---

## 3. Code Quality Check

Perform a basic code quality assessment.

**File: `quality-check.prompt.md`**
```yaml
---
name: "quality-check"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 3000
---

# Code Quality Check

Perform a code quality assessment of the current project:

## Analysis Areas
1. **Code Organization**: Are files and folders well-organized?
2. **Naming Conventions**: Do classes, methods, and variables follow C# conventions?
3. **Dependencies**: Are there any outdated or problematic packages?
4. **Best Practices**: Any obvious violations of .NET best practices?

## Output Format
Create a report called `quality-report.md` with:
- ✅ **Strengths**: What's done well
- ⚠️ **Areas for Improvement**: Issues found
- 💡 **Recommendations**: Specific suggestions
- 📊 **Summary Score**: Overall quality rating (1-10)
```

**Run it:**
```bash
dotnet prompt run quality-check.prompt.md
```

**What you'll learn:**
- Setting output token limits
- Structured output formatting
- Using emojis and markdown for clear reports

---

## 4. Simple Documentation Generator

Generate basic documentation for your project.

**File: `basic-docs.prompt.md`**
```yaml
---
name: "basic-docs"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.7
  maxOutputTokens: 4000

input:
  schema:
    include_setup:
      type: boolean
      description: "Include setup and installation instructions"
      default: true
    include_examples:
      type: boolean
      description: "Include usage examples"
      default: true
---

# Basic Documentation Generator

Generate documentation for the current .NET project:

## Core Documentation
1. **README.md**: Project overview and description
2. **API Overview**: Main classes and their purposes

{{#if include_setup}}
## Setup Instructions
3. **Installation**: How to build and run the project
4. **Prerequisites**: Required tools and dependencies
{{/if}}

{{#if include_examples}}
## Usage Examples
5. **Basic Usage**: Simple examples of how to use the project
6. **Common Scenarios**: Typical use cases
{{/if}}

## Output Files
- Update or create `README.md` in the project root
- Create `docs/api-overview.md` for API documentation

Make the documentation clear, concise, and helpful for developers who are new to the project.
```

**Run it:**
```bash
# With default parameters
dotnet prompt run basic-docs.prompt.md

# Customize the output
dotnet prompt run basic-docs.prompt.md --parameter include_setup=false
```

**What you'll learn:**
- Input parameter schemas
- Conditional content with handlebars syntax
- CLI parameter overrides
- Multi-file output

---

## 5. Build and Test

Build your project and run tests with reporting.

**File: `build-and-test.prompt.md`**
```yaml
---
name: "build-and-test"
model: "gpt-4o"
tools: ["build-test", "file-system"]

config:
  temperature: 0.2  # Very factual for build results
---

# Build and Test Workflow

Execute a complete build and test cycle:

## Build Phase
1. Clean any previous build artifacts
2. Restore NuGet packages
3. Build the project in Release configuration
4. Report any build errors or warnings

## Test Phase
5. Run all unit tests
6. Collect test coverage data (if available)
7. Generate a test report

## Output
Create a build report (`build-report.md`) containing:
- ✅ Build status (success/failure)
- ⚠️ Warnings count and details
- 🧪 Test results summary
- 📊 Code coverage percentage (if available)
- 🕒 Build and test duration

If there are any failures, provide clear guidance on how to fix them.
```

**Run it:**
```bash
dotnet prompt run build-and-test.prompt.md --verbose
```

**What you'll learn:**
- Using the build-test tool
- Sequential workflow steps
- Error handling and reporting
- Setting very low temperature for factual data

---

## 6. Configuration Analysis

Analyze project configuration files and settings.

**File: `config-analysis.prompt.md`**
```yaml
---
name: "config-analysis"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.4
---

# Configuration Analysis

Analyze the project's configuration and settings:

## Configuration Files
1. **Project Files**: Examine .csproj files for configuration
2. **App Settings**: Check appsettings.json and environment-specific files
3. **Launch Settings**: Review launchSettings.json for development settings
4. **Package Config**: Look at NuGet package configurations

## Analysis Points
- Are sensitive settings properly handled?
- Are there environment-specific configurations?
- Are development vs production settings clearly separated?
- Are there any missing or redundant configurations?

## Security Review
- Check for hardcoded secrets or API keys
- Verify connection strings are parameterized
- Review CORS and security headers configuration

## Output
Create `configuration-analysis.md` with:
- 📋 **Configuration Summary**: What was found
- 🔒 **Security Assessment**: Potential security issues
- ⚙️ **Recommendations**: Configuration improvements
- 📝 **Best Practices**: Suggested configuration patterns
```

**Run it:**
```bash
dotnet prompt run config-analysis.prompt.md
```

**What you'll learn:**
- Security-focused analysis
- Configuration file examination
- Best practices reporting

---

## 7. Dependency Audit

Analyze project dependencies for issues and updates.

**File: `dependency-audit.prompt.md`**
```yaml
---
name: "dependency-audit"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 3500
---

# Dependency Audit

Perform a comprehensive audit of project dependencies:

## Package Analysis
1. **Current Packages**: List all NuGet packages and versions
2. **Update Check**: Identify packages that have newer versions
3. **Vulnerability Scan**: Look for known security vulnerabilities
4. **License Review**: Check package licenses for compatibility

## Dependency Health
5. **Redundancy Check**: Find duplicate or overlapping packages
6. **Usage Analysis**: Identify unused or rarely used packages
7. **Framework Compatibility**: Verify packages work with target framework

## Report Generation
Create `dependency-audit.md` with:
- 📦 **Package Inventory**: Complete list with versions
- ⬆️ **Update Recommendations**: Suggested package updates
- 🚨 **Security Alerts**: Any vulnerabilities found
- 🧹 **Cleanup Suggestions**: Packages to remove or consolidate
- 📋 **Action Plan**: Prioritized list of dependency actions

Include specific commands for updating packages where applicable.
```

**Run it:**
```bash
dotnet prompt run dependency-audit.prompt.md
```

**What you'll learn:**
- Comprehensive dependency analysis
- Security vulnerability assessment
- Actionable recommendations
- Command generation for fixes

---

## 8. Simple Refactoring Suggestions

Identify opportunities for code improvements.

**File: `refactoring-suggestions.prompt.md`**
```yaml
---
name: "refactoring-suggestions"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.6
  maxOutputTokens: 4000

input:
  schema:
    focus_area:
      type: string
      description: "Area to focus refactoring analysis on"
      enum: ["performance", "maintainability", "readability", "all"]
      default: "all"
---

# Refactoring Suggestions

Analyze the codebase and suggest refactoring opportunities:

## Analysis Focus: {{focus_area}}

{{#eq focus_area "performance"}}
Focus on performance improvements:
- Identify inefficient algorithms or data structures
- Look for unnecessary allocations or boxing
- Check for async/await usage patterns
{{/eq}}

{{#eq focus_area "maintainability"}}
Focus on maintainability improvements:
- Look for code duplication
- Identify large methods or classes
- Check for proper separation of concerns
{{/eq}}

{{#eq focus_area "readability"}}
Focus on readability improvements:
- Check naming conventions
- Look for complex conditional logic
- Identify magic numbers or strings
{{/eq}}

{{#eq focus_area "all"}}
Comprehensive analysis covering:
- Code structure and organization
- Performance optimization opportunities
- Maintainability improvements
- Readability enhancements
{{/eq}}

## Output
Generate `refactoring-suggestions.md` with:
- 🎯 **Priority Refactorings**: Most impactful changes
- 📝 **Code Examples**: Before/after snippets where helpful
- 🔧 **Implementation Steps**: How to make the changes
- ⏱️ **Effort Estimates**: Time investment for each suggestion
- 📊 **Impact Assessment**: Expected benefits of each change
```

**Run it:**
```bash
# General refactoring analysis
dotnet prompt run refactoring-suggestions.prompt.md

# Focus on specific area
dotnet prompt run refactoring-suggestions.prompt.md --parameter focus_area=performance
```

**What you'll learn:**
- Enum parameter types
- Conditional content with comparisons
- Code improvement analysis
- Effort estimation

---

## Running the Examples

### Prerequisites
Make sure you have dotnet-prompt installed and configured:

```bash
# Check installation
dotnet prompt --version

# Set up a test project if needed
mkdir test-project
cd test-project
dotnet new console
```

### Try Each Example
1. Copy any example to a `.prompt.md` file
2. Run it: `dotnet prompt run filename.prompt.md`
3. Check the generated output files
4. Modify the workflow and run again

### Tips for Learning
- Start with `hello-world.prompt.md` and work your way up
- Use `--dry-run` to validate workflows without executing
- Use `--verbose` to see detailed execution information
- Experiment with different parameter values
- Look at the generated files to understand the output format

## Next Steps

Ready for more advanced scenarios?

- **[Advanced Workflows](./advanced-workflows.md)**: Complex multi-step scenarios
- **[MCP Integration](./mcp-integration.md)**: Using external tools and services
- **[Sub-workflow Composition](./advanced-workflows.md#sub-workflow-composition)**: Building reusable workflow components
- **[Real-world Examples](../examples/workflows/real-world-scenarios/)**: Production-ready workflow patterns

## Troubleshooting

Having issues? Check the [troubleshooting guide](./troubleshooting.md) for common problems and solutions.