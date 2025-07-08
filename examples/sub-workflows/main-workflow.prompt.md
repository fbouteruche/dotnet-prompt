---
name: "main-analysis-workflow"
model: "gpt-4o"
tools: ["sub-workflow", "file-system"]
input:
  schema:
    project_path: { type: string, description: "Path to the project to analyze" }
---

# Main Project Analysis Workflow

This workflow demonstrates sub-workflow composition by orchestrating multiple analysis steps.

## Step 1: Basic Analysis
First, let's run a basic project structure analysis:

{{execute_sub_workflow path="./sub-workflows/project-structure.prompt.md" parameters="{\"project_path\": \"{{project_path}}\"}"}}

## Step 2: Dependency Analysis
Now let's analyze the project dependencies:

{{execute_sub_workflow path="./sub-workflows/dependency-analysis.prompt.md" parameters="{\"project_path\": \"{{project_path}}\"}" inheritContext=true}}

## Step 3: Summary
Based on the analysis above, provide a comprehensive summary of the project including:
- Project structure insights
- Key dependencies and their purposes
- Recommendations for improvements