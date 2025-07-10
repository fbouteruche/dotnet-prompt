---
name: "comprehensive-code-review"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.4
  maxOutputTokens: 5000

input:
  schema:
    review_scope:
      type: string
      enum: ["full", "recent-changes", "specific-area"]
      description: "Scope of code review"
      default: "full"
    focus_areas:
      type: array
      items:
        type: string
        enum: ["security", "performance", "maintainability", "reliability", "documentation"]
      default: ["security", "maintainability", "reliability"]
    generate_fixes:
      type: boolean
      description: "Generate code fixes for identified issues"
      default: false
---

# Comprehensive Code Review Workflow

Perform a thorough code review focusing on quality, security, and maintainability.

## Review Configuration
- **Scope**: {{review_scope}}
- **Focus Areas**: {{#each focus_areas}}{{this}}{{#unless @last}}, {{/unless}}{{/each}}
- **Generate Fixes**: {{#if generate_fixes}}Yes{{else}}No{{/if}}

## Code Review Process

### 1. Pre-Review Analysis
- Build the project to ensure it compiles
- Run existing tests to establish baseline
- Analyze project structure and organization
- Identify key areas for focused review

{{#eq review_scope "full"}}
### 2. Comprehensive Code Analysis
- Review all source files for quality and standards compliance
- Analyze architecture and design patterns
- Check for consistent coding conventions
- Evaluate error handling and logging practices
{{/eq}}

{{#eq review_scope "recent-changes"}}
### 2. Recent Changes Analysis
- Focus on files modified in the last 30 days
- Review new features and bug fixes
- Analyze impact of recent changes on overall codebase
- Check for proper testing of new functionality
{{/eq}}

{{#eq review_scope "specific-area"}}
### 2. Targeted Area Analysis
- Focus on specific components or modules
- Deep dive into critical business logic
- Analyze complex algorithms and data processing
- Review integration points and external dependencies
{{/eq}}

## Focus Area Reviews

{{#contains focus_areas "security"}}
### 🔒 Security Review
- **Authentication & Authorization**: Review access controls and permissions
- **Input Validation**: Check for proper validation and sanitization
- **Data Protection**: Analyze encryption and sensitive data handling
- **Configuration Security**: Review security settings and secrets management
- **Dependency Security**: Check for vulnerable packages and libraries
- **SQL Injection & XSS**: Review for common web vulnerabilities
{{/contains}}

{{#contains focus_areas "performance"}}
### ⚡ Performance Review
- **Algorithm Efficiency**: Analyze computational complexity
- **Database Access**: Review query patterns and optimization
- **Memory Management**: Check for memory leaks and excessive allocations
- **Async Patterns**: Evaluate asynchronous programming usage
- **Caching Strategy**: Review caching implementation and effectiveness
- **Resource Usage**: Analyze CPU and I/O intensive operations
{{/contains}}

{{#contains focus_areas "maintainability"}}
### 🔧 Maintainability Review
- **Code Organization**: Evaluate structure and separation of concerns
- **Naming Conventions**: Check for clear and consistent naming
- **Method Complexity**: Identify overly complex methods needing refactoring
- **Code Duplication**: Find and highlight duplicate code patterns
- **Design Patterns**: Review appropriate use of design patterns
- **Documentation**: Assess code comments and documentation quality
{{/contains}}

{{#contains focus_areas "reliability"}}
### 🛡️ Reliability Review
- **Error Handling**: Review exception handling and error recovery
- **Logging & Monitoring**: Analyze logging practices and observability
- **Resource Management**: Check proper disposal and cleanup
- **Concurrency**: Review thread safety and concurrent access patterns
- **Data Validation**: Ensure robust data validation throughout
- **Graceful Degradation**: Check handling of external service failures
{{/contains}}

{{#contains focus_areas "documentation"}}
### 📚 Documentation Review
- **Code Comments**: Review inline documentation quality
- **API Documentation**: Check public interface documentation
- **README & Guides**: Evaluate user and developer documentation
- **Architecture Documentation**: Review system design documentation
- **Change Documentation**: Check for proper change tracking
{{/contains}}

## Quality Assessment

### 3. Code Quality Metrics
- **Complexity Analysis**: Measure cyclomatic complexity
- **Test Coverage**: Analyze test coverage and quality
- **Code Duplication**: Identify duplicate code blocks
- **Dependency Analysis**: Review coupling and cohesion
- **Technical Debt**: Quantify technical debt and maintenance burden

### 4. Standards Compliance
- **Coding Standards**: Check adherence to team/industry standards
- **Framework Best Practices**: Verify proper framework usage
- **Design Principles**: Evaluate SOLID principles compliance
- **Architectural Patterns**: Review pattern implementation consistency

## Issue Categorization

### Critical Issues (🚨)
- Security vulnerabilities
- Data corruption risks
- Performance bottlenecks
- Reliability concerns

### Important Issues (⚠️)
- Maintainability problems
- Code quality issues
- Standards violations
- Technical debt

### Minor Issues (💡)
- Style inconsistencies
- Documentation gaps
- Optimization opportunities
- Enhancement suggestions

## Report Generation

Create comprehensive code review report `code-review-report.md`:

### 📋 Executive Summary
- Overall code quality assessment (1-10 scale)
- Key findings and recommendations
- Critical issues requiring immediate attention
- Code review summary statistics

### 🎯 Detailed Findings
{{#each focus_areas}}
#### {{this}} Analysis
- Specific findings and evidence
- Risk assessment and impact analysis
- Recommendations for improvement
{{/each}}

### 📊 Quality Metrics
- Code complexity measurements
- Test coverage analysis
- Duplication and maintainability metrics
- Comparison with industry benchmarks

### 🔧 Actionable Recommendations
- Prioritized list of improvements
- Specific code changes needed
- Process improvements suggested
- Training or tooling recommendations

{{#if generate_fixes}}
### 💻 Proposed Code Fixes
- Specific code improvements with examples
- Refactoring suggestions with implementation
- Configuration and setup improvements
- Test additions and enhancements
{{/if}}

### 📈 Improvement Roadmap
- Short-term fixes (next sprint)
- Medium-term improvements (next quarter)
- Long-term architectural changes
- Success metrics and measurement plan

## Additional Outputs

{{#if generate_fixes}}
### Code Fix Examples
Create example files in `./code-review-fixes/`:
- `security-fixes.md` - Security improvement examples
- `performance-optimizations.md` - Performance enhancement examples
- `refactoring-suggestions.md` - Code structure improvements
- `test-improvements.md` - Additional test cases and improvements
{{/if}}

### Quality Improvement Plan
- `quality-improvement-plan.md` - Detailed improvement roadmap
- `standards-checklist.md` - Code review checklist for future use
- `tooling-recommendations.md` - Suggested tools and automation

## Follow-up Actions

### Immediate Actions
- Address critical security and reliability issues
- Fix high-impact performance problems
- Resolve standards violations

### Process Improvements
- Implement automated code quality checks
- Establish regular code review practices
- Create team coding standards documentation
- Set up continuous integration quality gates

This comprehensive code review provides actionable insights for improving code quality, security, and maintainability while establishing practices for ongoing quality management.