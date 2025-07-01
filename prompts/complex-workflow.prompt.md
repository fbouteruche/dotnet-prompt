---
name: "complex-workflow"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.6
  maxOutputTokens: 8000
  stopSequences: ["END_WORKFLOW"]

input:
  default:
    project_type: "web-api"
    include_tests: true
    generate_docs: true
    deploy_target: "azure"
  schema:
    project_name:
      type: string
      description: "Name of the .NET project to create or analyze"
    project_type:
      type: string
      enum: ["console", "web-api", "mvc", "blazor", "library", "worker"]
      description: "Type of .NET project"
      default: "web-api"
    target_framework:
      type: string
      enum: ["net6.0", "net7.0", "net8.0"]
      description: ".NET target framework version"
      default: "net8.0"
    include_tests:
      type: boolean
      description: "Include unit and integration tests"
      default: true
    generate_docs:
      type: boolean
      description: "Generate comprehensive documentation"
      default: true
    deploy_target:
      type: string
      enum: ["azure", "aws", "docker", "local"]
      description: "Target deployment platform"
      default: "azure"
    enable_ci_cd:
      type: boolean
      description: "Set up CI/CD pipeline configuration"
      default: true

output:
  format: json
  schema:
    project_created:
      type: boolean
      description: "Whether project was successfully created"
    files_generated:
      type: array
      items: {type: string}
      description: "List of generated files"
    test_results:
      type: object
      properties:
        tests_run: {type: number}
        tests_passed: {type: number}
        coverage_percentage: {type: number}
    deployment_config:
      type: object
      properties:
        platform: {type: string}
        config_files: {type: array, items: {type: string}}

metadata:
  description: "Complex workflow demonstrating advanced features including sub-workflows, conditional logic, and MCP integration"
  author: "dotnet-prompt team"
  version: "2.0.0"
  tags: ["complex", "workflow", "demonstration", "advanced", "ci-cd", "deployment"]

# MCP (Model Context Protocol) Integration
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      root_path: "./{{project_name}}"
      allow_create: true
  - server: "git-mcp"
    version: "2.1.0"
    config:
      repository: "./{{project_name}}"
      auto_commit: true
  - server: "azure-mcp"
    version: "1.5.0"
    config:
      subscription: "${AZURE_SUBSCRIPTION_ID}"
      resource_group: "{{project_name}}-rg"
    condition: "{{deploy_target}} == 'azure'"

# Sub-workflow Composition
dotnet-prompt.sub-workflows:
  - name: "project-setup"
    path: "./workflows/setup/create-project.prompt.md"
    parameters:
      project_name: "{{project_name}}"
      project_type: "{{project_type}}"
      target_framework: "{{target_framework}}"
    condition: "!project_exists({{project_name}})"
    
  - name: "test-setup"
    path: "./workflows/testing/setup-tests.prompt.md"
    parameters:
      project_path: "./{{project_name}}"
      test_framework: "xunit"
    depends_on: ["project-setup"]
    condition: "{{include_tests}}"
    
  - name: "documentation-generation"
    path: "./documentation-generator.prompt.md"
    parameters:
      project_path: "./{{project_name}}"
      output_directory: "./{{project_name}}/docs"
      include_api_docs: true
    depends_on: ["project-setup", "test-setup"]
    condition: "{{generate_docs}}"
    
  - name: "ci-cd-setup"
    path: "./workflows/deployment/setup-cicd.prompt.md"
    parameters:
      project_path: "./{{project_name}}"
      platform: "{{deploy_target}}"
      include_tests: "{{include_tests}}"
    depends_on: ["project-setup"]
    condition: "{{enable_ci_cd}}"

# Progress and Resume Configuration
dotnet-prompt.progress:
  enabled: true
  checkpoint_frequency: "after_each_sub_workflow"
  storage_location: "./.dotnet-prompt/progress/{{project_name}}"
  auto_resume: true
  cleanup_after_success: false

# Error Handling Configuration
dotnet-prompt.error-handling:
  retry_attempts: 3
  backoff_strategy: "exponential"
  timeout_seconds: 600
  continue_on_non_critical_errors: true
  rollback_on_failure: true
---

# Advanced .NET Project Workflow

This is a complex, multi-stage workflow that demonstrates advanced dotnet-prompt capabilities including sub-workflows, conditional execution, MCP integration, and comprehensive project lifecycle management.

## Workflow Overview

I will create and configure a complete {{project_type}} project named "{{project_name}}" with the following capabilities:

- ✅ Project creation and setup
- ✅ Testing infrastructure ({{#if include_tests}}enabled{{else}}disabled{{/if}})
- ✅ Documentation generation ({{#if generate_docs}}enabled{{else}}disabled{{/if}})
- ✅ CI/CD pipeline setup for {{deploy_target}}
- ✅ Security and performance best practices
- ✅ Production-ready configuration

## Phase 1: Project Foundation

### Project Creation and Structure
{{#if project_exists}}
Analyzing existing project at "./{{project_name}}"...
{{else}}
Creating new {{project_type}} project with these specifications:
- **Project Name**: {{project_name}}
- **Project Type**: {{project_type}}
- **Target Framework**: {{target_framework}}
- **Location**: "./{{project_name}}"

> Execute Sub-workflow: project-setup
> This will create the project structure, configure dependencies, and set up the basic architecture.
{{/if}}

### Development Environment Setup
Configure the development environment with:
- EditorConfig for consistent coding style
- Directory.Build.props for solution-wide settings
- .gitignore with appropriate .NET exclusions
- Development and production configuration files
- Logging and monitoring setup

## Phase 2: Quality Assurance Foundation

{{#if include_tests}}
### Testing Infrastructure Setup
> Execute Sub-workflow: test-setup
> Parameters:
> - test_framework: "xunit"
> - include_integration_tests: true
> - include_performance_tests: true

This will establish:
- Unit testing project with xUnit framework
- Integration testing infrastructure
- Test data management and fixtures
- Code coverage reporting setup
- Performance and load testing foundation
{{else}}
### Testing Infrastructure
Testing is disabled for this project. Consider enabling tests for production applications.
{{/if}}

### Code Quality Tools
Configure code quality and analysis tools:
- Enable nullable reference types
- Configure static code analysis (Roslyn analyzers)
- Set up code formatting and style enforcement
- Configure security scanning tools
- Implement code metrics collection

## Phase 3: Documentation and Knowledge Management

{{#if generate_docs}}
### Comprehensive Documentation
> Execute Sub-workflow: documentation-generation
> Parameters:
> - include_api_docs: true
> - include_examples: true
> - format: "markdown"
> - include_architecture_diagrams: true

This will generate:
- Complete API documentation
- User guides and tutorials
- Architecture documentation
- Developer onboarding guides
- Deployment and operations documentation
{{else}}
### Basic Documentation
Generating minimal documentation including README and basic setup instructions.
{{/if}}

## Phase 4: DevOps and Deployment

{{#if enable_ci_cd}}
### CI/CD Pipeline Configuration
> Execute Sub-workflow: ci-cd-setup
> Parameters:
> - platform: "{{deploy_target}}"
> - enable_automated_testing: {{include_tests}}
> - enable_security_scanning: true
> - enable_performance_monitoring: true

Setting up {{deploy_target}} deployment pipeline with:
- Automated build and test execution
- Security vulnerability scanning
- Code quality gates
- Automated deployment to staging
- Production deployment with approval gates
{{else}}
### Manual Deployment Setup
Configuring manual deployment procedures and documentation.
{{/if}}

### Infrastructure as Code
{{#if deploy_target == "azure"}}
Creating Azure Resource Manager (ARM) templates or Bicep files for:
- App Service or Container Apps configuration
- Database setup (if applicable)
- Application Insights monitoring
- Key Vault for secrets management
- Network security groups and policies
{{else if deploy_target == "aws"}}
Creating AWS CloudFormation templates for:
- EC2 or ECS deployment configuration
- RDS database setup (if applicable)
- CloudWatch monitoring
- Secrets Manager configuration
- VPC and security group setup
{{else if deploy_target == "docker"}}
Creating Docker configuration:
- Multi-stage Dockerfile for optimal image size
- Docker Compose for local development
- Kubernetes manifests for orchestration
- Container security best practices
{{/if}}

## Phase 5: Security and Compliance

### Security Implementation
Implement comprehensive security measures:
- Authentication and authorization setup
- Input validation and sanitization
- Secure configuration management
- HTTPS enforcement and certificate management
- Security headers and policies

### Compliance and Monitoring
Set up monitoring and compliance tools:
- Application performance monitoring
- Security event logging
- Compliance reporting setup
- Health checks and heartbeat monitoring
- Error tracking and alerting

## Phase 6: Validation and Testing

### Project Validation
Validate the complete project setup:
- Build verification across all configurations
- Test execution and coverage validation
- Security scan execution
- Performance baseline establishment
- Documentation completeness check

### Quality Gates
Ensure all quality gates are met:
- Code coverage thresholds (minimum 80%)
- Security vulnerability assessment
- Performance benchmarks
- Documentation completeness
- Deployment readiness checklist

## Final Output Summary

Upon completion, provide a comprehensive summary including:

```json
{
  "project_created": true,
  "project_name": "{{project_name}}",
  "project_type": "{{project_type}}",
  "target_framework": "{{target_framework}}",
  "files_generated": [
    "List of all generated files and configurations"
  ],
  "sub_workflows_executed": [
    "project-setup",
    "test-setup",
    "documentation-generation", 
    "ci-cd-setup"
  ],
  "test_results": {
    "tests_run": 0,
    "tests_passed": 0,
    "coverage_percentage": 0
  },
  "deployment_config": {
    "platform": "{{deploy_target}}",
    "config_files": ["List of deployment configuration files"]
  },
  "next_steps": [
    "Recommended next actions for project development"
  ],
  "maintenance_tasks": [
    "Ongoing maintenance and monitoring recommendations"
  ]
}
```

### Success Criteria Checklist
- [ ] Project builds successfully
- [ ] All tests pass (if testing enabled)
- [ ] Documentation is complete and accurate
- [ ] CI/CD pipeline is functional
- [ ] Security measures are properly configured
- [ ] Deployment configuration is ready
- [ ] Monitoring and logging are operational

END_WORKFLOW