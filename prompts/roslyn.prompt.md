---
name: "code-review"
model: "azure/gpt-4.1"
tools: ["list_directory", "analyze_with_roslyn", "file_write"]
---

Use `list_directory` to explore the project structure and find *.sln files. Use `analyze_with_roslyn` with `semantic_depth` parameter set to `Standard` to analyze each solution.
Use `file_write` to save the raw output of the analysis to a file named `roslyn_analysis_results.txt`.

