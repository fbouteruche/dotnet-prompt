---
name: "mcp-integration-example"
model: "github/gpt-4o"
tools: ["file_read", "example_tool"]

dotnet-prompt.mcp:
  # Local MCP server example (stdio communication)
  - server: "filesystem-mcp"
    version: "1.0.0"
    connection_type: "stdio"
    config:
      root_path: "./examples"
  
  # Remote MCP server example (SSE over HTTPS)
  - server: "analytics-api"
    version: "2.0.0"
    connection_type: "sse"
    endpoint: "https://api.example.com/mcp/v2"
    auth_token: "${ANALYTICS_API_TOKEN}"
    headers:
      "X-Client-ID": "dotnet-prompt"
      "X-API-Version": "v2"
    timeout: "30s"
    config:
      feature_flags: ["advanced_analytics", "real_time_data"]

dotnet-prompt.progress:
  enabled: true
  checkpoint_frequency: "after_each_tool"
---

# MCP Integration Demonstration

This workflow demonstrates the Model Context Protocol (MCP) integration in dotnet-prompt, showcasing both local (stdio) and remote (SSE over HTTPS) MCP server connections.

## Available Tools

The following tools are available in this workflow:

### Built-in Tools
- **file_read**: Read files from the local filesystem (FileSystemPlugin)

### Local MCP Server Tools
- **example_tool**: Example tool from the local filesystem MCP server

### Remote MCP Server Tools
- **remote_tool**: Example tool from the remote analytics API MCP server

## Workflow Execution

Please analyze the project structure and provide insights using both local file operations and remote analytics capabilities.

1. First, read the project configuration files to understand the structure
2. Use the MCP analytics tools to provide project insights
3. Generate a comprehensive analysis report

Start by reading the main project configuration file and then use the available MCP tools to enhance the analysis.