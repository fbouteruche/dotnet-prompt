---
name: "git-workflow-analysis"
model: "gpt-4o"
tools: ["project-analysis"]

dotnet-prompt.mcp:
  - server: "git-mcp"
    version: "2.0.0"
    config:
      repository: "."
      include_history: true
      max_commits: 100

config:
  temperature: 0.4
  maxOutputTokens: 4000
---

# Git Workflow Analysis

Analyze the Git repository and development workflow to understand patterns and suggest improvements.

## Git Repository Analysis

### 1. Repository Overview
- Analyze the repository structure and organization
- Check branch strategy and current branches
- Review repository configuration and settings

### 2. Commit History Analysis
- Examine recent commit patterns and frequency
- Analyze commit message quality and conventions
- Identify main contributors and their activity patterns

### 3. Branch Strategy Assessment
- Evaluate current branching model (Git Flow, GitHub Flow, etc.)
- Check branch naming conventions
- Assess merge vs rebase practices

### 4. Development Workflow Patterns
- Analyze typical development cycle duration
- Check for consistent workflow practices
- Identify bottlenecks or inefficiencies

### 5. Code Change Patterns
- Identify files that change most frequently
- Analyze typical change sizes and complexity
- Look for patterns in bug fixes vs features

## Project Integration Analysis

### 6. Project-Git Alignment
- Check if project structure aligns with repository organization
- Verify that important project files are properly tracked
- Identify any missing or improperly ignored files

### 7. Release Management
- Analyze release patterns and frequency
- Check tagging conventions and release notes
- Evaluate deployment workflow integration

## Report Generation

Create a comprehensive workflow analysis report `git-workflow-analysis.md` with:

### 📊 Repository Health Dashboard
- Key metrics and statistics
- Repository activity summary
- Branch and contributor overview

### 🔄 Workflow Assessment
- Current development workflow description
- Workflow strengths and weaknesses
- Comparison with industry best practices

### 👥 Team Collaboration Patterns
- Contributor activity and patterns
- Collaboration effectiveness metrics
- Communication patterns through commits

### 🚀 Improvement Recommendations
- Workflow optimization suggestions
- Branch strategy improvements
- Tooling and automation opportunities

### 📋 Action Items
- Specific steps to improve development workflow
- Recommended workflow changes
- Tools and practices to adopt

### 🎯 Best Practices Checklist
- Git workflow best practices compliance
- Areas for improvement
- Training or documentation needs

## Additional Analysis

If the repository shows concerning patterns:
- Identify specific workflow bottlenecks
- Suggest concrete process improvements
- Recommend team practices or tooling changes

Focus on actionable insights that can immediately improve the development workflow and team productivity.