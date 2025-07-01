---
name: "code-review"
model: "gpt-4o"
tools: ["file-system", "project-analysis"]

config:
  temperature: 0.4
  maxOutputTokens: 5000

input:
  default:
    review_scope: "changed-files"
    include_security: true
    include_performance: true
  schema:
    target_path:
      type: string
      description: "Path to files or directory to review"
      default: "."
    review_scope:
      type: string
      enum: ["all-files", "changed-files", "specific-files"]
      description: "Scope of the code review"
      default: "changed-files"
    include_security:
      type: boolean
      description: "Include security vulnerability analysis"
      default: true
    include_performance:
      type: boolean
      description: "Include performance optimization suggestions"
      default: true
    severity_threshold:
      type: string
      enum: ["low", "medium", "high", "critical"]
      description: "Minimum severity level to report"
      default: "medium"

metadata:
  description: "Automated code review with security and performance analysis"
  author: "dotnet-prompt team"
  version: "1.0.0"
  tags: ["code-review", "security", "performance", "quality"]
---

# Automated Code Review

I need to perform a comprehensive code review of the .NET code at `{{target_path}}` with focus on code quality, security, and performance.

## Code Quality Review

Analyze the codebase for general quality issues:

### Design Patterns and Architecture
- Review adherence to SOLID principles
- Identify proper use of design patterns
- Check for appropriate separation of concerns
- Evaluate dependency injection usage
- Review error handling strategies

### Code Style and Conventions
- Check adherence to C# coding conventions
- Review naming conventions for classes, methods, and variables
- Analyze code organization and structure
- Evaluate comment quality and documentation
- Check for consistent formatting and style

### Best Practices
- Review async/await usage patterns
- Check for proper resource disposal (using statements, IDisposable)
- Evaluate exception handling practices
- Review logging and debugging approaches
- Check for appropriate use of language features

## Security Analysis

{{#if include_security}}
Perform security-focused code review:

### Authentication and Authorization
- Review authentication mechanisms and implementations
- Check authorization logic and access controls
- Analyze session management approaches
- Review password handling and storage

### Data Protection
- Analyze input validation and sanitization
- Review SQL injection prevention measures
- Check for XSS prevention in web applications
- Evaluate data encryption and protection measures

### Security Vulnerabilities
- Identify potential security vulnerabilities
- Check for hardcoded secrets or credentials
- Review file system access patterns
- Analyze network communication security
- Check for information disclosure risks

### Dependency Security
- Review third-party dependencies for known vulnerabilities
- Check for outdated packages with security issues
- Evaluate dependency trust and verification
{{/if}}

## Performance Analysis

{{#if include_performance}}
Review code for performance optimization opportunities:

### Algorithm Efficiency
- Analyze algorithm complexity and efficiency
- Identify potential optimization opportunities
- Review data structure choices
- Check for unnecessary computations or operations

### Memory Management
- Review memory allocation patterns
- Identify potential memory leaks
- Analyze object lifecycle management
- Check for efficient collection usage

### Concurrency and Threading
- Review thread safety implementations
- Analyze async/await patterns for efficiency
- Check for potential deadlocks or race conditions
- Evaluate parallel processing opportunities

### Database and I/O Performance
- Review database query efficiency
- Analyze file I/O operations
- Check for appropriate caching strategies
- Evaluate network communication patterns
{{/if}}

## .NET Specific Considerations

Focus on .NET-specific code review aspects:

### Framework Usage
- Review proper use of .NET Core/.NET 5+ features
- Check for appropriate use of built-in collections
- Analyze LINQ usage for efficiency
- Review Entity Framework or data access patterns

### Configuration and Deployment
- Review configuration management approaches
- Check for environment-specific settings handling
- Analyze logging configuration
- Review dependency injection container setup

### Testing and Testability
- Evaluate unit test coverage and quality
- Review testability of code design
- Check for proper mocking and isolation
- Analyze integration test strategies

## Review Output Format

Provide findings organized by category and severity:

### High Priority Issues
- Security vulnerabilities requiring immediate attention
- Critical performance bottlenecks
- Major architectural concerns
- Breaking changes or compatibility issues

### Medium Priority Issues  
- Code quality improvements
- Minor security enhancements
- Performance optimizations
- Best practice violations

### Low Priority Suggestions
- Style and convention improvements
- Documentation enhancements
- Refactoring opportunities
- Code simplification suggestions

### Recommendations
- Specific action items with code examples
- Priority ranking for addressing issues
- Long-term architectural improvements
- Tool recommendations for ongoing quality assurance

## Code Examples and Fixes

For each identified issue, provide:
- Clear explanation of the problem
- Example of the problematic code (if applicable)
- Suggested fix or improvement with code example
- Explanation of why the change improves the code
- Any potential trade-offs or considerations

Ensure all suggestions are practical, actionable, and follow .NET best practices and conventions.