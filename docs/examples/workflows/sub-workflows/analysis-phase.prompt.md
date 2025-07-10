---
name: "analysis-phase"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 3500

input:
  schema:
    analysis_depth:
      type: string
      enum: ["basic", "standard", "comprehensive"]
      default: "standard"
    include_security:
      type: boolean
      default: true
---

# Project Analysis Phase

Comprehensive analysis phase for project lifecycle workflow.

## Analysis Scope: {{analysis_depth}}

{{#eq analysis_depth "basic"}}
### Basic Analysis
- Project structure overview
- Main dependencies review
- Basic code quality indicators
{{/eq}}

{{#eq analysis_depth "standard"}}
### Standard Analysis
- Detailed project structure analysis
- Complete dependency audit
- Code quality metrics
- Build configuration review
{{/eq}}

{{#eq analysis_depth "comprehensive"}}
### Comprehensive Analysis
- Complete project architecture analysis
- In-depth dependency and security review
- Advanced code quality metrics
- Performance analysis opportunities
- Technical debt assessment
{{/eq}}

## Core Analysis Tasks

### 1. Project Structure Assessment
- Analyze project organization and architecture
- Evaluate separation of concerns and layering
- Identify architectural patterns and deviations
- Document key components and their relationships

### 2. Dependency Analysis
- Complete inventory of all dependencies
- Version analysis and update recommendations
- License compatibility review
- Dependency tree analysis for conflicts

{{#if include_security}}
### 3. Security Review
- Security vulnerability scanning
- Dependency security assessment
- Configuration security review
- Code security pattern analysis
{{/if}}

### 4. Code Quality Baseline
- Coding standards compliance assessment
- Code complexity analysis
- Documentation coverage review
- Technical debt identification

{{#eq analysis_depth "comprehensive"}}
### 5. Advanced Analysis
- Performance bottleneck identification
- Scalability assessment
- Maintainability metrics
- Refactoring opportunity analysis
{{/eq}}

## Output Generation

Create analysis outputs in `./analysis/` directory:

### 📋 `project-structure-analysis.md`
- Complete project organization breakdown
- Architecture documentation
- Component relationship mapping

### 📦 `dependency-analysis.json`
- Structured dependency data
- Update recommendations
- Security findings

{{#if include_security}}
### 🔒 `security-assessment.md`
- Security findings and recommendations
- Vulnerability details and remediation steps
- Security best practices compliance
{{/if}}

### 📊 `quality-metrics.json`
- Code quality measurements
- Complexity metrics
- Coverage analysis

### 💡 `improvement-recommendations.md`
- Prioritized improvement suggestions
- Technical debt remediation plan
- Quick wins and long-term improvements

## Analysis Standards

Ensure all analysis:
- ✅ Is objective and data-driven
- ✅ Provides actionable recommendations
- ✅ Includes specific examples and evidence
- ✅ Prioritizes findings by impact and effort
- ✅ Maintains compatibility with existing functionality

This analysis phase provides the foundation for all subsequent improvement phases.