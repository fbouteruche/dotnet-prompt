---
name: "comprehensive-project-lifecycle"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.sub-workflows:
  - name: "initial-analysis"
    path: "./analysis-phase.prompt.md"
    parameters:
      analysis_depth: "comprehensive"
      include_security: true
  - name: "quality-improvement"
    path: "./quality-phase.prompt.md"
    depends_on: ["initial-analysis"]
    parameters:
      focus_areas: ["code-quality", "performance", "security"]
  - name: "testing-setup"
    path: "./testing-phase.prompt.md"
    depends_on: ["quality-improvement"]
    parameters:
      test_types: ["unit", "integration"]
      coverage_target: 80
  - name: "documentation-generation"
    path: "./documentation-phase.prompt.md"
    depends_on: ["initial-analysis", "quality-improvement"]
    parameters:
      doc_types: ["api", "readme", "architecture"]
  - name: "final-validation"
    path: "./validation-phase.prompt.md"
    depends_on: ["testing-setup", "documentation-generation"]

config:
  temperature: 0.5
  maxOutputTokens: 4000

input:
  schema:
    project_type:
      type: string
      enum: ["web", "library", "console", "service"]
      description: "Type of .NET project"
      default: "web"
    include_deployment:
      type: boolean
      description: "Include deployment preparation"
      default: false
---

# Comprehensive Project Lifecycle Workflow

Execute a complete project lifecycle workflow with multiple specialized phases.

## Project Context
- **Project Type**: {{project_type}}
- **Include Deployment**: {{#if include_deployment}}Yes{{else}}No{{/if}}

## Workflow Overview

This workflow executes a comprehensive project lifecycle through specialized sub-workflows:

### Phase 1: Initial Analysis
Execute comprehensive project analysis to understand the current state, including:
- Project structure and architecture assessment
- Dependency analysis and security review
- Code quality baseline establishment
- Technical debt identification

### Phase 2: Quality Improvement
Based on the analysis results, implement quality improvements:
- Code quality enhancements
- Performance optimizations
- Security hardening
- Refactoring opportunities

### Phase 3: Testing Setup
Establish comprehensive testing infrastructure:
- Unit test creation and enhancement
- Integration test setup
- Test coverage improvement
- Test automation configuration

### Phase 4: Documentation Generation
Create comprehensive project documentation:
- API documentation generation
- README and user guides
- Architecture documentation
- Developer onboarding materials

### Phase 5: Final Validation
Validate all improvements and ensure project readiness:
- Build verification and testing
- Documentation quality check
- Security and compliance validation
- Performance benchmarking

{{#if include_deployment}}
### Phase 6: Deployment Preparation
Prepare the project for deployment:
- Configuration management setup
- Deployment scripts and documentation
- Environment-specific configurations
- Monitoring and logging setup
{{/if}}

## Success Criteria

Each phase must complete successfully before the next phase begins. The workflow will:

✅ **Maintain Quality**: Ensure no regressions are introduced
✅ **Preserve Functionality**: Validate that all changes maintain existing functionality
✅ **Improve Metrics**: Achieve measurable improvements in code quality and test coverage
✅ **Generate Documentation**: Produce comprehensive, up-to-date documentation
✅ **Validate Results**: Confirm all improvements through testing and validation

## Error Handling

If any phase fails:
- Document the failure point and specific errors
- Provide recovery recommendations
- Generate partial results from completed phases
- Create a remediation plan for failed components

## Final Output

Upon completion, generate:
- **Executive Summary**: High-level overview of all improvements made
- **Technical Report**: Detailed analysis of changes and their impact
- **Metrics Dashboard**: Before/after comparisons of key quality metrics
- **Next Steps Guide**: Recommendations for ongoing maintenance and improvement

The workflow ensures comprehensive project improvement while maintaining stability and functionality.