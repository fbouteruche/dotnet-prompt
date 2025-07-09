---
name: "query-ms-learn"
model: "gpt-4.1"

input:
  default:
    query: "What are the best practices for using Model Context Protocol (MCP) in .NET applications?"
  schema:
    query:
      type: string
      description: "Search query for Microsoft Learn documentation"

dotnet-prompt.mcp:
  - server: "ms-learn-server"
    connection_type: "sse"
    endpoint: "https://learn.microsoft.com/api/mcp"
    config:
      api_version: "v1"
      rate_limit: 100
---

# Microsoft Learn Documentation Query

You MUST use the `microsoft_docs_search` function from the `ms-learn-server` MCP server to search Microsoft Learn documentation for: {{query}}

Do not provide a generic response. You must call the search_docs function and base your response on the actual search results returned from the Microsoft Learn documentation.

Steps:
1. Call the `microsoft_docs_search` function with the query: {{query}}
2. Analyze the returned documentation
3. Provide a concise and relevant response based on the actual search results
