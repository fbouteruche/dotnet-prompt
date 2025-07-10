---
name: "hello-world"
model: "gpt-4o"
tools: ["file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 1000
---

# Hello World

Create a simple greeting file to demonstrate basic dotnet-prompt functionality.

## Task
Create a file called `hello.txt` in the current directory with the following content:
```
Hello from dotnet-prompt! 

This file was created by an AI-powered workflow.
Generated on: [current date and time]

dotnet-prompt makes it easy to automate development tasks using natural language.
```

Make sure to include the actual current date and time in the file.