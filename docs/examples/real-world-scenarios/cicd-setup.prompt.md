---
name: "ci-cd-pipeline-setup"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 6000

input:
  schema:
    ci_provider:
      type: string
      enum: ["github-actions", "azure-pipelines", "jenkins", "gitlab-ci"]
      description: "CI/CD provider to configure"
      default: "github-actions"
    deployment_targets:
      type: array
      items:
        type: string
        enum: ["azure", "aws", "docker", "local", "kubernetes"]
      default: ["docker", "azure"]
    enable_security_scanning:
      type: boolean
      description: "Include security scanning in pipeline"
      default: true
    include_performance_tests:
      type: boolean
      description: "Include performance testing"
      default: false
---

# CI/CD Pipeline Setup Workflow

Create a comprehensive CI/CD pipeline tailored for the .NET project with modern DevOps practices.

## Pipeline Configuration
- **CI/CD Provider**: {{ci_provider}}
- **Deployment Targets**: {{#each deployment_targets}}{{this}}{{#unless @last}}, {{/unless}}{{/each}}
- **Security Scanning**: {{#if enable_security_scanning}}Enabled{{else}}Disabled{{/if}}
- **Performance Testing**: {{#if include_performance_tests}}Enabled{{else}}Disabled{{/if}}

## Pipeline Design Process

### 1. Project Analysis for Pipeline Requirements
- Analyze project type and deployment needs
- Identify testing requirements and strategies
- Review current build process and dependencies
- Assess security and compliance requirements

### 2. Environment and Infrastructure Planning
- Define deployment environments (dev, staging, production)
- Plan resource requirements and scaling needs
- Design environment-specific configurations
- Establish security and access controls

## CI/CD Pipeline Components

### Build Stage
```yaml
# Build pipeline configuration will include:
- Code checkout and workspace setup
- .NET SDK installation and version management
- Dependency restoration and caching
- Multi-configuration builds (Debug/Release)
- Build artifact generation and packaging
- Build quality gates and failure handling
```

### Test Stage
```yaml
# Testing pipeline will include:
- Unit test execution with parallel processing
- Integration test execution
- Test result reporting and analysis
- Code coverage collection and reporting
- Test artifact preservation
{{#if include_performance_tests}}
- Performance test execution and benchmarking
- Performance regression detection
{{/if}}
```

{{#if enable_security_scanning}}
### Security Stage
```yaml
# Security pipeline will include:
- Static Application Security Testing (SAST)
- Dependency vulnerability scanning
- Container security scanning (if using containers)
- Security compliance checks
- Security report generation and alerting
```
{{/if}}

### Quality Gates
```yaml
# Quality gates will enforce:
- Minimum test coverage thresholds (80%+)
- Build success requirements
- Security scan approval
- Code quality metrics compliance
- Performance baseline maintenance
```

## Deployment Strategy

{{#contains deployment_targets "docker"}}
### Docker Deployment
- **Containerization**: Create optimized Docker images
- **Multi-stage builds**: Minimize image size and security surface
- **Registry management**: Push to container registry with tagging strategy
- **Security scanning**: Scan container images for vulnerabilities
- **Configuration management**: Environment-specific container configs
{{/contains}}

{{#contains deployment_targets "azure"}}
### Azure Deployment
- **Azure App Service**: Web application deployment configuration
- **Azure Container Instances**: Containerized deployment setup
- **Azure DevOps integration**: Service connections and permissions
- **Infrastructure as Code**: ARM templates or Terraform configurations
- **Monitoring setup**: Application Insights and logging configuration
{{/contains}}

{{#contains deployment_targets "aws"}}
### AWS Deployment
- **Elastic Beanstalk**: Application deployment configuration
- **ECS/Fargate**: Container orchestration setup
- **Lambda**: Serverless deployment (if applicable)
- **CloudFormation**: Infrastructure as Code templates
- **CloudWatch**: Monitoring and logging setup
{{/contains}}

{{#contains deployment_targets "kubernetes"}}
### Kubernetes Deployment
- **Deployment manifests**: Kubernetes YAML configurations
- **Service definitions**: Load balancing and networking
- **ConfigMaps and Secrets**: Configuration management
- **Ingress configuration**: External access and routing
- **Helm charts**: Package management and templating
{{/contains}}

### Environment Management
- **Development**: Automatic deployment on feature branch merges
- **Staging**: Manual approval with automated testing
- **Production**: Gated deployment with rollback capabilities

## Pipeline Implementation

{{#eq ci_provider "github-actions"}}
### GitHub Actions Configuration
Create `.github/workflows/` directory with:

#### `ci.yml` - Continuous Integration
- Build and test workflow
- Multi-OS build matrix (Windows, Linux, macOS)
- Dependency caching for faster builds
- Test result reporting and coverage upload

#### `cd.yml` - Continuous Deployment
- Environment-specific deployment workflows
- Manual approval gates for production
- Rollback capabilities and monitoring
- Deployment status notifications

#### `security.yml` - Security Scanning
{{#if enable_security_scanning}}
- CodeQL analysis for code security
- Dependency vulnerability scanning
- Container security scanning
- Security report generation
{{/if}}
{{/eq}}

{{#eq ci_provider "azure-pipelines"}}
### Azure Pipelines Configuration
Create `azure-pipelines.yml` with:

#### Build Pipeline
- Multi-stage pipeline with build, test, and deploy stages
- Agent pool configuration and resource management
- Variable groups for environment-specific settings
- Artifact publishing and management

#### Release Pipeline
- Environment-specific deployment configurations
- Approval gates and deployment conditions
- Release variable management
- Deployment slot management for Azure App Service
{{/eq}}

{{#eq ci_provider "jenkins"}}
### Jenkins Configuration
Create `Jenkinsfile` with:

#### Pipeline Stages
- Declarative pipeline with comprehensive stages
- Agent configuration and workspace management
- Parallel execution for test and security stages
- Post-build actions and notifications

#### Job Configuration
- Multi-branch pipeline setup
- Environment variable management
- Build triggers and scheduling
- Artifact archival and deployment
{{/eq}}

## Configuration Files Generation

### Environment Configuration
Create environment-specific configuration files:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`
- Environment variable templates
- Secret management configuration

### Infrastructure as Code
{{#contains deployment_targets "azure"}}
- ARM templates for Azure resources
- Bicep files for modern Azure deployments
- Parameter files for different environments
{{/contains}}

{{#contains deployment_targets "aws"}}
- CloudFormation templates
- Terraform configurations
- Parameter store configurations
{{/contains}}

{{#contains deployment_targets "kubernetes"}}
- Kubernetes manifests
- Helm chart templates
- Kustomize configurations
{{/contains}}

### Docker Configuration
{{#contains deployment_targets "docker"}}
- `Dockerfile` with multi-stage build
- `.dockerignore` for build optimization
- `docker-compose.yml` for local development
- Container registry configuration
{{/contains}}

## Monitoring and Observability

### Application Monitoring
- Health check endpoints implementation
- Metrics collection and reporting
- Distributed tracing setup
- Error tracking and alerting

### Infrastructure Monitoring
- Resource utilization monitoring
- Performance baseline establishment
- Automated scaling configuration
- Incident response procedures

## Security and Compliance

### Security Measures
{{#if enable_security_scanning}}
- Automated security scanning integration
- Vulnerability assessment and remediation
- Security policy enforcement
- Compliance reporting and auditing
{{/if}}

### Access Control
- Role-based access control (RBAC) setup
- Service principal configuration
- Secret management and rotation
- Audit logging and compliance

## Documentation and Training

### Pipeline Documentation
Create comprehensive documentation in `docs/devops/`:
- `pipeline-overview.md` - High-level pipeline architecture
- `deployment-guide.md` - Step-by-step deployment instructions
- `troubleshooting.md` - Common issues and solutions
- `security-guidelines.md` - Security practices and compliance

### Team Training Materials
- Pipeline usage guidelines
- Deployment procedures and rollback processes
- Monitoring and alerting setup
- Incident response procedures

## Output Deliverables

### 📁 Pipeline Configuration Files
- Complete CI/CD pipeline configurations
- Environment-specific deployment scripts
- Infrastructure as Code templates
- Container and orchestration configurations

### 📋 Documentation Package
- Pipeline architecture and design decisions
- Deployment procedures and best practices
- Monitoring and maintenance guidelines
- Security and compliance documentation

### 🔧 Automation Scripts
- Deployment automation scripts
- Environment setup and configuration scripts
- Database migration and seeding scripts
- Backup and recovery procedures

### 📊 Monitoring Setup
- Monitoring dashboard configurations
- Alert rules and notification setup
- Performance baseline documentation
- Incident response playbooks

## Validation and Testing

### Pipeline Testing
- Test pipeline configurations in non-production environments
- Validate deployment procedures and rollback capabilities
- Performance test the CI/CD pipeline itself
- Security scan validation and compliance verification

### Documentation Validation
- Verify all instructions are accurate and complete
- Test deployment procedures with fresh environments
- Validate monitoring and alerting configurations
- Confirm security measures are properly implemented

This comprehensive CI/CD setup provides a production-ready deployment pipeline with modern DevOps practices, security integration, and comprehensive monitoring capabilities.