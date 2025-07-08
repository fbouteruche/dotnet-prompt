---
name: "dependency-analysis"
model: "gpt-4o"
tools: ["file-system"]
input:
  schema:
    project_path: { type: string }
---

# Dependency Analysis

Analyze the dependencies of the project at {{project_path}}.

Look for and analyze:
1. Package.json, requirements.txt, *.csproj, pom.xml, or similar dependency files
2. Lock files (package-lock.json, yarn.lock, etc.)
3. Dependency versions and potential security concerns
4. Outdated dependencies

{{file_read path="{{project_path}}/package.json"}}
{{file_read path="{{project_path}}/*.csproj"}}
{{file_read path="{{project_path}}/requirements.txt"}}