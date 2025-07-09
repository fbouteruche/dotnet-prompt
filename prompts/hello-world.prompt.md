---
name: "hello-world"
model: "gpt-4o"
tools: ["file_write"]
config:
  temperature: 0.7
  maxOutputTokens: 500
metadata:
  description: "Simple hello world greeting workflow"
  author: "dotnet-prompt team"
  version: "1.0.0"
  tags: ["hello", "example", "basic"]
---

# Hello World Workflow

Write me an hello world poem and use the `file_write` tool to save it to `./hello-world.txt`.
The poem should be creative and engaging, showcasing the beauty of a simple greeting.