---
name: "project-summary"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 2500

input:
  schema:
    include_dependencies:
      type: boolean
      description: "Include detailed dependency analysis"
      default: true
---

# Project Summary Generator

Generate a comprehensive summary of the current .NET project.

## Analysis Requirements

Analyze the current .NET project and create a summary including:

### 1. Project Overview
- Project name and type (Console, Web API, Class Library, etc.)
- Target framework version
- Primary purpose and functionality

### 2. Structure Analysis
- Key directories and their purposes
- Main source files and their roles
- Configuration files present

### 3. Dependencies
{{#if include_dependencies}}
- NuGet packages used and their purposes
- Framework dependencies
- Any notable dependency patterns or concerns
{{else}}
- Basic package count and framework information
{{/if}}

### 4. Development Notes
- Build configuration summary
- Any immediate observations about code organization
- Recommendations for new developers joining the project

## Output Format

Save the summary as `PROJECT-SUMMARY.md` in the project root with:
- Clear headings for each section
- Bullet points for easy reading
- Professional tone suitable for team documentation
- Include generation date and dotnet-prompt version info at the bottom

The summary should be comprehensive enough for a new team member to understand the project quickly.