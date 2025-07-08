---
name: "simple-sk-handlebars"
model: "gpt-4o"
tools: ["file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 1500

input:
  default:
    project_name: "SampleProject"
    author_name: "Developer"
    include_readme: true
  schema:
    project_name:
      type: string
      description: "Name of the project"
      default: "MyProject"
    author_name:
      type: string
      description: "Author name for the project"
      default: "Unknown"
    include_readme:
      type: boolean
      description: "Whether to include README generation"
      default: true
---

# Basic Handlebars Template Test

Hello {{author_name}}! This is a test of SK native Handlebars templating for project {{project_name}}.

## Project Information

- **Project Name**: {{project_name}}
- **Author**: {{author_name}}
- **Date**: {{current_date}}

{{#if include_readme}}
## README Generation

Since include_readme is enabled, I will help you create a comprehensive README file for {{project_name}}.

The README should include:
- Project description
- Installation instructions
- Usage examples
- Contributing guidelines
{{else}}
## Documentation Skipped

README generation has been disabled for this project.
{{/if}}

This template validates that SK Handlebars processing works correctly with:
- Variable substitution: {{project_name}}
- Conditional logic: {{#if include_readme}}...{{/if}}
- Default value handling
- Integration with .prompt.md frontmatter

Complete the analysis and provide feedback on the templating functionality.