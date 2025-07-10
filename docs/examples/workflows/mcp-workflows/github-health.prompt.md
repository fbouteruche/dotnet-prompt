---
name: "github-project-health"
model: "gpt-4o"
tools: ["project-analysis"]

dotnet-prompt.mcp:
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
      default_repo: "owner/repository"
      rate_limit_aware: true

config:
  temperature: 0.4
  maxOutputTokens: 5000

input:
  schema:
    include_community_metrics:
      type: boolean
      description: "Include community health metrics"
      default: true
    analyze_recent_activity:
      type: boolean
      description: "Focus on recent activity (last 30 days)"
      default: true
---

# GitHub Project Health Assessment

Analyze GitHub repository health, community engagement, and project management effectiveness.

## Repository Health Analysis

### 1. Repository Metadata
- Repository description, topics, and visibility settings
- License, contributing guidelines, and code of conduct presence
- Repository statistics (stars, forks, watchers)

### 2. Issue Management
- Open vs closed issue ratio
- Issue response times and resolution patterns
- Issue labeling and categorization effectiveness
- Bug report quality and triage process

### 3. Pull Request Workflow
- PR creation and merge patterns
- Code review process effectiveness
- PR response times and discussion quality
- Merge conflict frequency and resolution

### 4. Release Management
- Release frequency and consistency
- Release notes quality and completeness
- Version numbering and semantic versioning compliance
- Deployment and distribution process

{{#if include_community_metrics}}
### 5. Community Health
- Contributor diversity and engagement levels
- New contributor onboarding effectiveness
- Documentation quality and accessibility
- Community guidelines and support resources
{{/if}}

## Project Management Analysis

### 6. Development Workflow
- Branch protection rules and policies
- CI/CD integration and effectiveness
- Code quality gates and automation
- Security scanning and dependency management

{{#if analyze_recent_activity}}
### 7. Recent Activity Assessment (Last 30 Days)
- Commit frequency and contributor activity
- Issue and PR creation/resolution rates
- Release activity and feature delivery
- Community engagement trends
{{/if}}

### 8. Technical Debt Indicators
- Long-running open issues and PRs
- Stale branches and abandoned work
- Security alerts and dependency updates
- Code scanning alerts and resolution

## Integration with Project Analysis

### 9. Repository-Code Alignment
- Compare GitHub project settings with actual codebase
- Verify repository organization matches project structure
- Check for consistency between documentation and implementation

## Report Generation

Create a comprehensive project health report `github-project-health.md` with:

### 🎯 Executive Summary
- Overall project health score (1-10)
- Key strengths and areas for improvement
- Critical issues requiring immediate attention

### 📊 Health Metrics Dashboard
- Issue resolution rates and trends
- PR merge times and success rates
- Release frequency and quality metrics
- Community engagement indicators

### 🔄 Workflow Effectiveness
- Development process assessment
- Code review quality and efficiency
- Release management effectiveness
- Automation and tooling utilization

{{#if include_community_metrics}}
### 👥 Community Health
- Contributor engagement and diversity
- New contributor experience
- Documentation and support quality
- Community growth trends
{{/if}}

### 🚨 Critical Issues
- Immediate problems requiring attention
- Security concerns and dependency issues
- Process bottlenecks and inefficiencies
- Community or contributor concerns

### 💡 Improvement Recommendations
- Specific actionable improvements
- Process optimizations and automation opportunities
- Community engagement enhancements
- Technical debt reduction strategies

### 📋 Action Plan
- Prioritized list of improvements
- Responsible parties and timelines
- Success metrics and measurement approaches
- Follow-up review schedule

## Additional Outputs

If concerning patterns are identified:
- Create detailed remediation plans
- Suggest specific GitHub features or integrations to adopt
- Recommend team practices or policy changes
- Provide templates for improved processes (issue templates, PR templates, etc.)

Focus on practical, implementable recommendations that will improve project health and team productivity.