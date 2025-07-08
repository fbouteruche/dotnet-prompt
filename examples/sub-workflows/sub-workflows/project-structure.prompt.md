---
name: "project-structure-analysis"
model: "gpt-4o"
tools: ["file-system"]
input:
  schema:
    project_path: { type: string }
---

# Project Structure Analysis

Analyze the structure of the project at {{project_path}}.

Please examine:
1. Directory structure and organization
2. Key files and their purposes
3. Configuration files present
4. Build system setup

{{file_list path="{{project_path}}" recursive=true max_depth=3}}