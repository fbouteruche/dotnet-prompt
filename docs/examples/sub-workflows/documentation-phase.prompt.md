---
name: "documentation-phase"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.6
  maxOutputTokens: 4000

input:
  schema:
    doc_types:
      type: array
      items:
        type: string
        enum: ["api", "readme", "architecture", "user-guide", "developer-guide"]
      default: ["readme", "api"]
    update_existing:
      type: boolean
      description: "Update existing documentation instead of replacing"
      default: true
---

# Documentation Generation Phase

Generate comprehensive project documentation based on analysis results.

## Documentation Types: {{doc_types}}

{{#contains doc_types "readme"}}
### 1. README Documentation
- Project overview and description
- Installation and setup instructions
- Usage examples and quick start guide
- API overview and key features
- Contributing guidelines and standards
{{/contains}}

{{#contains doc_types "api"}}
### 2. API Documentation
- Complete API reference with examples
- Method signatures and parameter descriptions
- Return value documentation
- Error handling and status codes
- Authentication and authorization details
{{/contains}}

{{#contains doc_types "architecture"}}
### 3. Architecture Documentation
- System architecture overview
- Component relationships and dependencies
- Data flow and processing patterns
- Design decisions and trade-offs
- Scalability and performance considerations
{{/contains}}

{{#contains doc_types "user-guide"}}
### 4. User Guide
- Step-by-step usage instructions
- Common scenarios and use cases
- Configuration options and settings
- Troubleshooting and FAQ
- Best practices and tips
{{/contains}}

{{#contains doc_types "developer-guide"}}
### 5. Developer Guide
- Development environment setup
- Code organization and standards
- Build and deployment processes
- Testing strategies and guidelines
- Contribution workflow and review process
{{/contains}}

## Documentation Generation Process

### Phase 1: Content Analysis
- Review analysis results from previous phases
- Identify key information for each documentation type
- Gather existing documentation for reference
- Plan documentation structure and organization

### Phase 2: Content Generation
{{#if update_existing}}
- Analyze existing documentation for preservation
- Update and enhance existing content
- Fill gaps in current documentation
- Maintain consistency with existing style
{{else}}
- Generate fresh documentation from scratch
- Create comprehensive new content
- Establish new documentation standards
- Implement modern documentation practices
{{/if}}

### Phase 3: Quality Assurance
- Review documentation for accuracy and completeness
- Validate code examples and instructions
- Check for consistency across all documentation
- Ensure proper formatting and readability

### Phase 4: Integration
- Organize documentation in logical structure
- Create navigation and cross-references
- Generate table of contents and indexes
- Set up documentation maintenance processes

## Output Structure

Create documentation in `./docs/` directory:

{{#contains doc_types "readme"}}
### 📄 `README.md`
- Main project documentation
- Quick start and overview
- Links to detailed documentation
{{/contains}}

{{#contains doc_types "api"}}
### 📚 `docs/api/`
- `api-reference.md` - Complete API documentation
- `examples/` - Code examples and usage patterns
- `integration-guide.md` - Integration instructions
{{/contains}}

{{#contains doc_types "architecture"}}
### 🏗️ `docs/architecture/`
- `system-overview.md` - High-level architecture
- `component-design.md` - Detailed component documentation
- `data-flow.md` - Data processing and flow diagrams
- `decisions/` - Architecture decision records (ADRs)
{{/contains}}

{{#contains doc_types "user-guide"}}
### 👥 `docs/user-guide/`
- `getting-started.md` - Quick start guide
- `configuration.md` - Configuration options
- `troubleshooting.md` - Common issues and solutions
- `advanced-usage.md` - Advanced features and scenarios
{{/contains}}

{{#contains doc_types "developer-guide"}}
### 🛠️ `docs/developer/`
- `development-setup.md` - Environment setup
- `coding-standards.md` - Code style and standards
- `testing-guide.md` - Testing strategies and tools
- `contribution-guide.md` - How to contribute
{{/contains}}

## Documentation Standards

Ensure all documentation:
- ✅ **Accuracy**: Reflects current project state
- ✅ **Completeness**: Covers all necessary topics
- ✅ **Clarity**: Written in clear, accessible language
- ✅ **Examples**: Includes practical, working examples
- ✅ **Navigation**: Well-organized with clear structure
- ✅ **Maintenance**: Includes update procedures and responsibilities

## Quality Checks

### Content Validation
- Verify all code examples compile and run
- Check that instructions produce expected results
- Validate links and references
- Ensure consistency with project capabilities

### Accessibility and Usability
- Use clear headings and structure
- Include table of contents for long documents
- Provide multiple access paths to information
- Consider different user skill levels

## Maintenance Planning

### Documentation Lifecycle
- Create update procedures for each documentation type
- Establish review cycles and responsibilities
- Plan for automation where possible
- Set up feedback collection mechanisms

### Integration with Development
- Link documentation updates to code changes
- Create templates for common documentation tasks
- Establish documentation review process
- Plan for ongoing maintenance and improvement

This documentation phase ensures comprehensive, maintainable documentation that serves both users and developers effectively.