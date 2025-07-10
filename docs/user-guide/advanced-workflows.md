# Advanced Workflows

This guide covers sophisticated workflow patterns including sub-workflow composition, conditional logic, parameter handling, and complex multi-step automation scenarios.

## Sub-workflow Composition

Sub-workflows allow you to break complex tasks into reusable components and compose them into larger workflows.

### Basic Sub-workflow Pattern

**Main workflow: `project-lifecycle.prompt.md`**
```yaml
---
name: "project-lifecycle"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "analysis"
    path: "./workflows/project-analysis.prompt.md"
    parameters:
      depth: "comprehensive"
      include_security: true
  - name: "testing"
    path: "./workflows/test-execution.prompt.md"
    depends_on: ["analysis"]
  - name: "documentation"
    path: "./workflows/doc-generation.prompt.md"
    depends_on: ["analysis", "testing"]

input:
  schema:
    project_path:
      type: string
      default: "."
    skip_tests:
      type: boolean
      default: false
---

# Complete Project Lifecycle

Execute a comprehensive project lifecycle workflow:

## Phase 1: Analysis
First, perform comprehensive project analysis to understand the codebase structure, dependencies, and quality metrics.

## Phase 2: Testing
{{#unless skip_tests}}
Run the complete test suite and generate coverage reports.
{{else}}
Skipping test execution as requested.
{{/unless}}

## Phase 3: Documentation
Generate comprehensive documentation based on the analysis and test results.

Provide a final summary of all completed phases and their outcomes.
```

**Sub-workflow: `workflows/project-analysis.prompt.md`**
```yaml
---
name: "detailed-project-analysis"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

input:
  schema:
    depth:
      type: string
      enum: ["basic", "standard", "comprehensive"]
      default: "standard"
    include_security:
      type: boolean
      default: false
---

# Detailed Project Analysis

Perform {{depth}} analysis of the .NET project:

## Core Analysis
1. Project structure and architecture
2. Dependency analysis and package audit
3. Code quality metrics and patterns

{{#if include_security}}
## Security Analysis
4. Dependency vulnerability scan
5. Code security pattern review
6. Configuration security assessment
{{/if}}

{{#eq depth "comprehensive"}}
## Comprehensive Deep Dive
7. Performance analysis opportunities
8. Maintainability assessment
9. Technical debt identification
10. Refactoring recommendations
{{/eq}}

Save analysis results to `./analysis/` directory with timestamped files.
```

### Conditional Sub-workflow Execution

```yaml
---
name: "adaptive-workflow"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "web-app-analysis"
    path: "./workflows/web-app-specific.prompt.md"
    condition: "{{#eq project_type 'web'}}"
    parameters:
      analyze_controllers: true
      check_security_headers: true
  - name: "library-analysis"
    path: "./workflows/library-specific.prompt.md"  
    condition: "{{#eq project_type 'library'}}"
    parameters:
      check_api_surface: true
      verify_documentation: true
  - name: "console-analysis"
    path: "./workflows/console-specific.prompt.md"
    condition: "{{#eq project_type 'console'}}"

input:
  schema:
    project_type:
      type: string
      enum: ["web", "library", "console", "test"]
      description: "Type of .NET project"
---

# Adaptive Project Analysis

Analyze the project using type-specific workflows based on the project type: {{project_type}}.

The analysis will automatically adapt to use the most appropriate analysis patterns for this type of project.
```

### Parallel Sub-workflow Execution

```yaml
---
name: "parallel-analysis"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "code-analysis"
    path: "./workflows/code-quality.prompt.md"
    parallel_group: "analysis"
  - name: "dependency-audit"
    path: "./workflows/dependency-check.prompt.md"
    parallel_group: "analysis"
  - name: "security-scan"
    path: "./workflows/security-analysis.prompt.md"
    parallel_group: "analysis"
  - name: "performance-analysis"
    path: "./workflows/performance-check.prompt.md"
    parallel_group: "analysis"
  - name: "report-generation"
    path: "./workflows/consolidate-reports.prompt.md"
    depends_on: ["code-analysis", "dependency-audit", "security-scan", "performance-analysis"]
---

# Parallel Analysis Workflow

Execute multiple analysis workflows in parallel for faster completion:

1. **Parallel Analysis Phase**: Run code quality, dependency audit, security scan, and performance analysis simultaneously
2. **Report Consolidation**: Combine all analysis results into a comprehensive report

This approach significantly reduces total execution time while maintaining thorough analysis coverage.
```

## Advanced Parameter Handling

### Complex Parameter Schemas

```yaml
---
name: "configurable-ci-setup"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

input:
  schema:
    ci_provider:
      type: string
      enum: ["github", "azure", "jenkins", "gitlab"]
      description: "CI/CD provider to configure"
    environments:
      type: array
      items:
        type: object
        properties:
          name:
            type: string
          deployment_target:
            type: string
            enum: ["azure", "aws", "local", "docker"]
          requires_approval:
            type: boolean
            default: false
      default:
        - name: "development"
          deployment_target: "local"
        - name: "staging"
          deployment_target: "azure"
          requires_approval: false
        - name: "production"
          deployment_target: "azure"
          requires_approval: true
    features:
      type: object
      properties:
        code_coverage:
          type: boolean
          default: true
        security_scanning:
          type: boolean
          default: true
        performance_testing:
          type: boolean
          default: false
        deployment_automation:
          type: boolean
          default: true
      default:
        code_coverage: true
        security_scanning: true
---

# Configurable CI/CD Setup

Set up CI/CD pipeline for {{ci_provider}} with the following configuration:

## Environments
{{#each environments}}
### {{name}} Environment
- **Deployment Target**: {{deployment_target}}
- **Requires Approval**: {{#if requires_approval}}Yes{{else}}No{{/if}}
{{/each}}

## Features Enabled
{{#if features.code_coverage}}
- ✅ Code Coverage Collection and Reporting
{{/if}}
{{#if features.security_scanning}}
- ✅ Security Vulnerability Scanning
{{/if}}
{{#if features.performance_testing}}
- ✅ Performance Testing Integration
{{/if}}
{{#if features.deployment_automation}}
- ✅ Automated Deployment Workflows
{{/if}}

Generate the complete CI/CD configuration files and documentation for this setup.
```

### Parameter Validation and Defaults

```yaml
---
name: "project-setup-with-validation"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

input:
  schema:
    project_name:
      type: string
      pattern: "^[a-zA-Z][a-zA-Z0-9._-]*$"
      description: "Valid .NET project name"
    target_framework:
      type: string
      enum: ["net8.0", "net9.0", "netstandard2.0", "netstandard2.1"]
      default: "net8.0"
    project_type:
      type: string
      enum: ["console", "classlib", "web", "webapi", "worker"]
      default: "console"
    include_tests:
      type: boolean
      default: true
    package_references:
      type: array
      items:
        type: object
        properties:
          name:
            type: string
          version:
            type: string
            pattern: "^\\d+\\.\\d+\\.\\d+.*$"
      default: []
    git_initialization:
      type: boolean
      default: true
---

# Project Setup with Validation

Create a new .NET project with the following specifications:

## Project Configuration
- **Name**: {{project_name}}
- **Type**: {{project_type}}
- **Target Framework**: {{target_framework}}
- **Include Tests**: {{#if include_tests}}Yes{{else}}No{{/if}}
- **Initialize Git**: {{#if git_initialization}}Yes{{else}}No{{/if}}

## Package References
{{#if package_references}}
{{#each package_references}}
- {{name}} ({{version}})
{{/each}}
{{else}}
No additional package references specified.
{{/if}}

Validate all parameters, create the project structure, and set up the development environment.
```

## Error Handling and Recovery

### Robust Error Handling

```yaml
---
name: "resilient-build-workflow"
model: "gpt-4o"
tools: ["build-test", "file-system"]

config:
  temperature: 0.2  # Low temperature for factual error reporting
---

# Resilient Build Workflow

Execute a comprehensive build process with error handling and recovery:

## Phase 1: Pre-build Validation
1. **Project File Validation**: Check for valid .csproj files
2. **Dependency Check**: Verify all required packages are available
3. **Environment Validation**: Ensure .NET SDK is available and correct version

If any validation fails, provide specific remediation steps and exit gracefully.

## Phase 2: Clean Build Process
4. **Clean Previous Artifacts**: Remove bin/ and obj/ directories
5. **Restore Packages**: Download and restore NuGet packages
6. **Compile Project**: Build in Release configuration

For each step, if errors occur:
- Capture the exact error message
- Analyze the error type (dependency, syntax, configuration, etc.)
- Provide specific troubleshooting steps
- Suggest automated fixes where possible

## Phase 3: Build Verification
7. **Output Validation**: Verify expected assemblies were created
8. **Dependency Analysis**: Check for runtime dependency issues
9. **Configuration Review**: Validate build configuration

## Phase 4: Error Recovery
If any phase fails:
- Generate a detailed error report
- Provide step-by-step recovery instructions
- Create a recovery script when possible
- Save diagnostic information for further analysis

## Final Output
Create `build-report.md` with:
- ✅ **Success Summary**: What completed successfully
- ❌ **Error Details**: Specific failures with context
- 🔧 **Recovery Actions**: Required steps to fix issues
- 📋 **Next Steps**: Recommended actions
```

### Retry Logic and Fallbacks

```yaml
---
name: "network-resilient-workflow"
model: "gpt-4o"
tools: ["project-analysis", "build-test"]

dotnet-prompt.mcp:
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
      retry_attempts: 3
      retry_delay: 5000
      timeout: 30000
---

# Network-Resilient Workflow

Execute operations with network dependencies using retry logic:

## Primary Operations
1. **GitHub Repository Analysis**: Fetch repository metadata, issues, and PRs
2. **Dependency Updates**: Check for package updates using online sources
3. **Security Vulnerability Check**: Query vulnerability databases

## Retry Strategy
For each network operation:
- **Attempt 1**: Normal execution with standard timeout
- **Attempt 2**: Extended timeout if first attempt fails
- **Attempt 3**: Fallback to cached data if available

## Fallback Operations
If network operations fail completely:
- Use local Git repository data instead of GitHub API
- Analyze local packages.config/project files for dependency info
- Generate warnings about incomplete analysis due to network issues

## Error Handling
For each failure:
- Log the specific network error
- Record which operations completed vs failed
- Provide offline alternatives where possible
- Generate a partial report with clear indicators of missing data

Always provide value even when some operations fail.
```

## Performance Optimization

### Large Project Handling

```yaml
---
name: "large-project-optimizer"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

input:
  schema:
    max_files_per_batch:
      type: integer
      default: 100
      description: "Maximum files to process in each batch"
    exclude_patterns:
      type: array
      items:
        type: string
      default: ["bin/**", "obj/**", "node_modules/**", ".git/**"]
    parallel_processing:
      type: boolean
      default: true
---

# Large Project Analysis Optimizer

Efficiently analyze large projects using batching and optimization:

## Performance Configuration
- **Batch Size**: {{max_files_per_batch}} files per analysis batch
- **Parallel Processing**: {{#if parallel_processing}}Enabled{{else}}Disabled{{/if}}
- **Excluded Patterns**: {{#each exclude_patterns}}{{this}}{{#unless @last}}, {{/unless}}{{/each}}

## Analysis Strategy
1. **Project Discovery**: Identify all project files and estimate scope
2. **Intelligent Filtering**: Skip generated files, build artifacts, and dependencies
3. **Batch Processing**: Process files in manageable chunks
4. **Progressive Results**: Provide interim results as analysis progresses
5. **Memory Management**: Clear intermediate results to manage memory usage

## Optimization Techniques
- Prioritize critical files (project files, main source code)
- Use file size and modification date heuristics
- Skip binary files and large data files automatically
- Implement smart caching for repeated analysis

## Progress Reporting
Provide regular progress updates including:
- Files processed vs total files
- Current batch being analyzed
- Estimated time remaining
- Memory usage statistics

Generate analysis results incrementally to provide value even if the full analysis takes significant time.
```

### Streaming and Incremental Processing

```yaml
---
name: "streaming-analysis"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.4
  stream_results: true  # Enable streaming output
---

# Streaming Project Analysis

Perform real-time streaming analysis with incremental results:

## Streaming Strategy
Process the project incrementally and provide results as analysis progresses:

### Phase 1: Quick Overview (< 30 seconds)
- Project type and basic structure
- High-level dependency overview
- Immediate critical issues (if any)

### Phase 2: Detailed Analysis (< 2 minutes)  
- Complete dependency analysis
- Code quality metrics
- Build configuration review
- Test coverage assessment

### Phase 3: Deep Insights (< 5 minutes)
- Security vulnerability analysis
- Performance optimization opportunities
- Refactoring recommendations
- Documentation gaps

### Phase 4: Comprehensive Report
- Consolidated findings
- Prioritized recommendations
- Action plan with timelines
- Next steps and follow-up tasks

## Incremental Output
For each phase, immediately output:
1. **Summary**: Key findings from this phase
2. **Critical Issues**: Any urgent problems discovered
3. **Quick Wins**: Easy improvements that can be made now
4. **Progress Update**: What's been analyzed and what's remaining

This approach ensures you get valuable insights quickly while the comprehensive analysis continues in the background.
```

## Integration Patterns

### External Service Integration

```yaml
---
name: "multi-service-integration"
model: "gpt-4o"
tools: ["project-analysis", "build-test"]

dotnet-prompt.mcp:
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
  - server: "jira-mcp"
    version: "1.0.0"
    config:
      url: "${JIRA_URL}"
      token: "${JIRA_TOKEN}"
  - server: "slack-mcp"
    version: "1.2.0"
    config:
      webhook_url: "${SLACK_WEBHOOK}"
---

# Multi-Service Integration Workflow

Coordinate across multiple external services for comprehensive project management:

## Service Integration Points
1. **GitHub**: Repository analysis, PR management, issue tracking
2. **Jira**: Project management, sprint planning, task tracking
3. **Slack**: Team communication, status updates, notifications

## Workflow Coordination
### Development Workflow Sync
- Fetch current sprint from Jira
- Analyze GitHub PRs related to sprint tasks  
- Generate development status report
- Post summary to Slack development channel

### Release Coordination
- Check GitHub release readiness (all PRs merged, tests passing)
- Update Jira tickets with release information
- Create release notes based on closed tickets
- Notify teams via Slack about release status

### Issue Management
- Sync GitHub issues with Jira tickets
- Update progress based on PR status
- Escalate blocked items to appropriate Slack channels
- Generate weekly status reports for stakeholders

## Error Handling
If any service is unavailable:
- Continue with available services
- Log service outages and impacts
- Provide partial reports with clear service status indicators
- Queue operations for retry when services recover

Generate comprehensive status report showing integration health and workflow efficiency.
```

### Database Integration Workflow

```yaml
---
name: "database-integration-analysis"
model: "gpt-4o"
tools: ["project-analysis"]

dotnet-prompt.mcp:
  - server: "database-mcp"
    version: "1.5.0"
    config:
      connection_string: "${DATABASE_URL}"
      read_only: true
      query_timeout: 30
---

# Database Integration Analysis

Analyze how the .NET application integrates with its database:

## Code-Database Mapping
1. **Entity Framework Analysis**: Examine DbContext classes and entity mappings
2. **Migration Review**: Check migration files for schema evolution
3. **Query Performance**: Analyze LINQ queries for optimization opportunities
4. **Data Access Patterns**: Review repository and service layer implementations

## Database Schema Analysis
5. **Schema Structure**: Analyze current database schema
6. **Index Optimization**: Check for missing or redundant indexes
7. **Constraint Validation**: Verify foreign keys and constraints
8. **Data Quality**: Check for orphaned records or data inconsistencies

## Integration Health Check
9. **Connection String Security**: Verify secure credential handling
10. **Error Handling**: Review database exception handling patterns
11. **Transaction Management**: Analyze transaction scope and patterns
12. **Performance Monitoring**: Check for slow queries and bottlenecks

## Recommendations
Generate actionable recommendations for:
- Database performance optimization
- Code quality improvements
- Security enhancements
- Monitoring and alerting setup

Create comprehensive database integration report with specific improvement suggestions.
```

## Workflow Orchestration Patterns

### Event-Driven Workflows

```yaml
---
name: "event-driven-ci-cd"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

input:
  schema:
    trigger_event:
      type: string
      enum: ["push", "pull_request", "release", "schedule", "manual"]
      description: "Event that triggered this workflow"
    branch_name:
      type: string
      description: "Git branch name"
    environment:
      type: string
      enum: ["development", "staging", "production"]
      default: "development"
---

# Event-Driven CI/CD Workflow

Execute appropriate actions based on the triggering event: {{trigger_event}}

{{#eq trigger_event "push"}}
## Push Event Handler
Branch: {{branch_name}}

### Actions for Push
1. **Fast Validation**: Quick syntax and build checks
2. **Unit Tests**: Run fast unit test suite
3. **Code Quality Gates**: Basic linting and formatting checks
4. **Notification**: Update development status dashboard

{{#eq branch_name "main"}}
### Additional Actions for Main Branch
5. **Integration Tests**: Run comprehensive test suite
6. **Security Scan**: Perform security analysis
7. **Deploy to Development**: Auto-deploy to dev environment
{{/eq}}
{{/eq}}

{{#eq trigger_event "pull_request"}}
## Pull Request Event Handler
Target Branch: {{branch_name}}

### PR Validation Actions
1. **Comprehensive Testing**: Full test suite execution
2. **Code Review Automation**: Generate automated code review
3. **Security Analysis**: Complete security vulnerability scan
4. **Performance Impact**: Analyze performance implications
5. **Documentation Check**: Verify documentation updates
6. **Review Request**: Generate summary for human reviewers
{{/eq}}

{{#eq trigger_event "release"}}
## Release Event Handler
Environment: {{environment}}

### Release Preparation
1. **Final Validation**: Complete test suite and quality checks
2. **Security Audit**: Comprehensive security review
3. **Performance Baseline**: Establish performance benchmarks
4. **Documentation Update**: Generate release documentation
5. **Deployment Preparation**: Prepare deployment artifacts

{{#eq environment "production"}}
### Production Release Actions
6. **Blue-Green Validation**: Verify deployment strategy
7. **Rollback Plan**: Prepare rollback procedures
8. **Monitoring Setup**: Configure production monitoring
9. **Stakeholder Notification**: Inform stakeholders of release
{{/eq}}
{{/eq}}

{{#eq trigger_event "schedule"}}
## Scheduled Maintenance Workflow
### Maintenance Actions
1. **Dependency Updates**: Check for package updates
2. **Security Patches**: Apply available security patches
3. **Performance Analysis**: Trending analysis and optimization
4. **Cleanup Tasks**: Remove old artifacts and temporary files
5. **Health Checks**: Comprehensive system health validation
6. **Reporting**: Generate maintenance summary report
{{/eq}}

Execute the appropriate workflow actions and provide detailed execution results.
```

### Multi-Stage Pipeline

```yaml
---
name: "multi-stage-pipeline"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "validate"
    path: "./pipeline/validate.prompt.md"
    stage: "validation"
  - name: "build"
    path: "./pipeline/build.prompt.md"
    stage: "build"
    depends_on: ["validate"]
  - name: "test"
    path: "./pipeline/test.prompt.md"
    stage: "test"
    depends_on: ["build"]
  - name: "security"
    path: "./pipeline/security.prompt.md"
    stage: "security"
    depends_on: ["test"]
  - name: "deploy-dev"
    path: "./pipeline/deploy-dev.prompt.md"
    stage: "deploy"
    depends_on: ["security"]
    condition: "{{#eq branch 'develop'}}"
  - name: "deploy-staging"
    path: "./pipeline/deploy-staging.prompt.md"
    stage: "deploy"
    depends_on: ["security"]
    condition: "{{#eq branch 'release/*'}}"
  - name: "deploy-prod"
    path: "./pipeline/deploy-prod.prompt.md"
    stage: "deploy"
    depends_on: ["security"]
    condition: "{{#eq branch 'main'}}"

input:
  schema:
    branch:
      type: string
      description: "Current git branch"
    commit_sha:
      type: string
      description: "Commit SHA being processed"
---

# Multi-Stage Deployment Pipeline

Execute multi-stage pipeline for branch: {{branch}} ({{commit_sha}})

## Pipeline Stages
The pipeline will execute the following stages in order:

### 1. Validation Stage
- Code syntax validation
- Project file validation
- Basic dependency checks

### 2. Build Stage
- Clean build process
- Artifact generation
- Build verification

### 3. Test Stage
- Unit test execution
- Integration test execution
- Test coverage analysis

### 4. Security Stage
- Vulnerability scanning
- Code security analysis
- Dependency security audit

### 5. Deployment Stage
{{#eq branch "develop"}}
- **Target**: Development Environment
- **Strategy**: Direct deployment with immediate validation
{{/eq}}
{{#contains branch "release/"}}
- **Target**: Staging Environment
- **Strategy**: Blue-green deployment with validation gates
{{/contains}}
{{#eq branch "main"}}
- **Target**: Production Environment
- **Strategy**: Canary deployment with rollback capability
{{/eq}}

Each stage must pass completely before proceeding to the next stage. Generate comprehensive pipeline execution report with stage-by-stage results.
```

## Next Steps

With these advanced patterns, you can create sophisticated automation workflows:

- **[Real-world Examples](../examples/workflows/real-world-scenarios/)**: See these patterns in production scenarios
- **[MCP Integration](./mcp-integration.md)**: Enhance workflows with external tools
- **[Troubleshooting](./troubleshooting.md)**: Debug complex workflow issues
- **[CLI Reference](../reference/cli-commands.md)**: Advanced CLI options for complex workflows