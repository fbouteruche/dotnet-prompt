# MCP Integration Guide

Model Context Protocol (MCP) servers extend dotnet-prompt with additional capabilities beyond the built-in tools. This guide explains how to install, configure, and use MCP servers in your workflows.

## What is MCP?

MCP (Model Context Protocol) is an open standard that allows AI applications to securely connect to external data sources and tools. MCP servers provide specialized capabilities like:

- **Enhanced File Operations**: Advanced file system tools with additional features
- **Git Integration**: Git repository operations and GitHub/GitLab integration  
- **Database Access**: Connect to databases for data analysis and manipulation
- **Web Scraping**: Extract data from websites and APIs
- **Cloud Services**: Integration with AWS, Azure, Google Cloud
- **Development Tools**: Docker, Kubernetes, CI/CD pipeline integration

## Installing MCP Servers

MCP servers are typically distributed as npm packages, Python packages, or standalone executables.

### Popular MCP Servers

#### Filesystem MCP Server
Enhanced file system operations with additional security and features.

```bash
# Install via npm
npm install -g @modelcontextprotocol/server-filesystem

# Verify installation
npx @modelcontextprotocol/server-filesystem --help
```

#### Git MCP Server
Git repository operations and history analysis.

```bash
# Install via npm
npm install -g @modelcontextprotocol/server-git

# Verify installation  
npx @modelcontextprotocol/server-git --help
```

#### GitHub MCP Server
GitHub API integration for issues, PRs, and repository management.

```bash
# Install via npm
npm install -g @modelcontextprotocol/server-github

# Verify installation
npx @modelcontextprotocol/server-github --help
```

#### Database MCP Server
Database connectivity and query execution.

```bash
# Install via Python pip
pip install mcp-server-database

# Verify installation
mcp-database-server --help
```

### Custom MCP Servers

You can also use custom MCP servers built for specific domains or organizational needs. Check with your team or organization for available custom servers.

## Configuring MCP Servers

MCP servers are configured in your workflow frontmatter using the `dotnet-prompt.mcp` field.

### Basic Configuration

```yaml
---
name: "mcp-example"
model: "gpt-4o"
tools: ["project-analysis"]  # Built-in tools still available

dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src", "./docs", "./tests"]
      max_file_size: "10MB"
      backup_enabled: true
---
```

### Server Configuration Options

Each MCP server has its own configuration options. Here are common patterns:

#### Filesystem MCP
```yaml
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src", "./docs"]  # Restrict access
      denied_directories: ["./secrets", "./.git"]  # Explicit deny
      max_file_size: "5MB"
      encoding: "utf-8"
      backup_enabled: true
      compression_enabled: false
```

#### Git MCP
```yaml
dotnet-prompt.mcp:
  - server: "git-mcp"
    version: "2.0.0"
    config:
      repository: "."  # Current directory
      include_history: true
      max_commits: 100
      branch_filter: ["main", "develop", "feature/*"]
```

#### GitHub MCP
```yaml
dotnet-prompt.mcp:
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"  # Environment variable
      default_repo: "owner/repository"
      rate_limit_aware: true
      include_drafts: false
```

#### Database MCP
```yaml
dotnet-prompt.mcp:
  - server: "database-mcp"
    version: "1.5.0"
    config:
      connection_string: "${DATABASE_URL}"
      read_only: true  # Safety setting
      query_timeout: 30
      max_rows: 1000
```

### Environment Variables

Use environment variables for sensitive configuration:

```bash
# Set environment variables
export GITHUB_TOKEN="ghp_xxxxxxxxxxxx"
export DATABASE_URL="Server=localhost;Database=MyApp;Integrated Security=true"

# Run workflow
dotnet prompt run my-workflow.prompt.md
```

### Multiple MCP Servers

You can use multiple MCP servers in the same workflow:

```yaml
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src"]
  - server: "git-mcp"
    version: "2.0.0"
    config:
      repository: "."
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
      default_repo: "myorg/myrepo"
```

## Using MCP Tools in Workflows

Once configured, MCP tools become available to your AI workflows automatically. You reference them using natural language, just like built-in tools.

### File Operations Example

```yaml
---
name: "enhanced-file-operations"
model: "gpt-4o"

dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src", "./docs"]
      backup_enabled: true
---

# Enhanced File Operations

Using the enhanced filesystem capabilities:

1. **Backup Current Files**: Create backups of all .cs files in ./src
2. **Bulk Rename**: Rename all test files to follow naming convention  
3. **File Analysis**: Analyze file sizes and suggest compression for large files
4. **Duplicate Detection**: Find and report duplicate files
5. **Permission Review**: Check file permissions and suggest improvements

Generate a comprehensive file operations report.
```

### Git Integration Example

```yaml
---
name: "git-analysis"
model: "gpt-4o"

dotnet-prompt.mcp:
  - server: "git-mcp"
    version: "2.0.0"
    config:
      repository: "."
      include_history: true
      max_commits: 50
---

# Git Repository Analysis

Analyze the Git repository to understand:

1. **Recent Activity**: What changes have been made recently?
2. **Commit Patterns**: Who are the main contributors and their patterns?
3. **Branch Strategy**: What branching model is being used?
4. **File Evolution**: Which files change most frequently?
5. **Code Churn**: Are there areas with high modification rates?

Create a development workflow report with recommendations.
```

### GitHub Integration Example

```yaml
---
name: "github-project-analysis"
model: "gpt-4o"

dotnet-prompt.mcp:
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
      default_repo: "myorg/myproject"
---

# GitHub Project Analysis

Analyze the GitHub project comprehensively:

## Repository Health
1. **Issue Management**: Open issues, response times, labeling
2. **Pull Request Workflow**: PR patterns, review process, merge practices
3. **Release Management**: Release frequency, changelog quality
4. **Community Engagement**: Contributors, discussions, documentation

## Development Insights
5. **Code Review Quality**: Review thoroughness and feedback patterns
6. **CI/CD Status**: Build success rates, test coverage trends
7. **Security**: Security advisories, dependency updates
8. **Documentation**: README quality, wiki usage, API docs

Generate a project health dashboard with actionable recommendations.
```

### Combined MCP and Built-in Tools

```yaml
---
name: "comprehensive-project-audit"
model: "gpt-4o"
tools: ["project-analysis", "build-test", "file-system"]

dotnet-prompt.mcp:
  - server: "git-mcp"
    version: "2.0.0"
  - server: "github-mcp"
    version: "2.1.0"
    config:
      token: "${GITHUB_TOKEN}"
---

# Comprehensive Project Audit

Combine built-in tools with MCP servers for complete analysis:

## Technical Analysis (Built-in Tools)
1. **.NET Project Analysis**: Structure, dependencies, configuration
2. **Build and Test**: Current build status and test coverage
3. **Code Quality**: Static analysis and best practices review

## Repository Analysis (Git MCP)
4. **Version Control**: Commit history, branching patterns, file evolution

## Platform Analysis (GitHub MCP)  
5. **Project Management**: Issues, PRs, releases, community health
6. **DevOps Integration**: CI/CD pipelines, security, automation

## Output
Create a comprehensive audit report combining all analysis results with:
- Executive summary
- Technical recommendations  
- Process improvements
- Security considerations
- Performance optimization opportunities
```

## MCP Server Management

### Global MCP Configuration

Create a global configuration file at `~/.dotnet-prompt/mcp-servers.json`:

```json
{
  "servers": {
    "filesystem-mcp": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-filesystem"],
      "version": "1.0.0",
      "timeout": 30000
    },
    "git-mcp": {
      "command": "npx", 
      "args": ["@modelcontextprotocol/server-git"],
      "version": "2.0.0",
      "timeout": 30000
    },
    "github-mcp": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-github"],
      "version": "2.1.0",
      "timeout": 30000
    }
  },
  "default_timeout": 30000,
  "auto_discovery": true
}
```

### Project-Level MCP Configuration

For team projects, create `.dotnet-prompt/mcp-servers.json` in your project:

```json
{
  "servers": {
    "custom-company-tools": {
      "command": "docker",
      "args": ["run", "--rm", "company/mcp-tools"],
      "version": "1.0.0"
    },
    "database-tools": {
      "command": "python",
      "args": ["-m", "company_mcp.database"],
      "version": "2.0.0"
    }
  }
}
```

### Server Discovery

Check available MCP servers:

```bash
# List configured servers
dotnet prompt mcp list

# Check server status
dotnet prompt mcp status

# Test server connection
dotnet prompt mcp test filesystem-mcp
```

## Security Considerations

### Access Control

MCP servers can have powerful capabilities. Always configure appropriate restrictions:

```yaml
dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    config:
      allowed_directories: ["./src", "./docs"]  # Restrict access
      denied_directories: ["./.git", "./secrets"]  # Explicit denials
      read_only: false  # Allow writes only when needed
      backup_enabled: true  # Enable safety backups
```

### Credential Management

- Use environment variables for sensitive data
- Never hardcode tokens or passwords in workflow files
- Use least-privilege access principles
- Regularly rotate credentials

```yaml
# ✅ Good - using environment variables
config:
  token: "${GITHUB_TOKEN}"
  database_url: "${DATABASE_URL}"

# ❌ Bad - hardcoded credentials
config:
  token: "ghp_xxxxxxxxxxxx"
  password: "mypassword123"
```

### Network Security

- Use HTTPS for all external connections
- Validate SSL certificates
- Implement appropriate timeouts
- Monitor network activity

## Troubleshooting MCP Integration

### Common Issues

#### Server Not Found
```
Error: MCP server 'filesystem-mcp' not found
```

**Solutions:**
1. Check if the server is installed: `npm list -g @modelcontextprotocol/server-filesystem`
2. Verify the server name in your workflow matches the installed name
3. Check the global MCP configuration

#### Connection Timeout
```
Error: MCP server connection timeout after 30000ms
```

**Solutions:**
1. Increase timeout in configuration
2. Check server health: `dotnet prompt mcp test server-name`
3. Verify network connectivity
4. Check server logs for errors

#### Permission Denied
```
Error: Access denied to directory './restricted'
```

**Solutions:**
1. Check `allowed_directories` configuration
2. Verify file system permissions
3. Remove path from `denied_directories` if needed
4. Use relative paths consistently

#### Invalid Configuration
```
Error: Invalid MCP server configuration for 'github-mcp'
```

**Solutions:**
1. Validate configuration syntax
2. Check required vs optional fields
3. Verify environment variables are set
4. Review server documentation

### Debugging MCP Issues

Enable verbose logging:

```bash
# Run with verbose MCP logging
dotnet prompt run workflow.prompt.md --verbose --mcp-debug

# Check MCP server logs
dotnet prompt mcp logs filesystem-mcp

# Test MCP server connectivity
dotnet prompt mcp diagnose
```

## Best Practices

### Configuration Management
1. **Use Environment Variables**: For sensitive configuration data
2. **Document Dependencies**: List required MCP servers in project README
3. **Version Pinning**: Specify exact server versions for reproducibility
4. **Test Configurations**: Validate MCP setup before deployment

### Workflow Design
1. **Graceful Degradation**: Handle missing MCP servers appropriately
2. **Error Handling**: Provide clear error messages for MCP failures
3. **Performance**: Consider MCP server startup time in workflow design
4. **Security**: Apply principle of least privilege

### Team Collaboration
1. **Shared Configuration**: Use project-level MCP configuration files
2. **Documentation**: Document MCP server requirements and setup
3. **Onboarding**: Include MCP setup in team onboarding guides
4. **Standards**: Establish team standards for MCP server usage

## MCP Server Development

Interested in creating custom MCP servers? Check the [MCP specification](https://modelcontextprotocol.io/) and existing server implementations for guidance.

### Creating a Custom Server

Basic TypeScript/Node.js MCP server structure:

```typescript
import { McpServer } from '@modelcontextprotocol/sdk';

const server = new McpServer({
  name: 'my-custom-server',
  version: '1.0.0'
});

// Add tool definitions
server.addTool({
  name: 'my_custom_tool',
  description: 'Custom tool for specific domain',
  inputSchema: {
    type: 'object',
    properties: {
      input: { type: 'string' }
    }
  }
});

// Implementation
server.setToolHandler('my_custom_tool', async (params) => {
  // Custom logic here
  return { result: 'Custom tool result' };
});

server.start();
```

## Next Steps

- **[Advanced Workflows](./advanced-workflows.md)**: Complex scenarios using MCP integration
- **[Real-world Examples](../examples/workflows/mcp-workflows/)**: Production MCP workflow patterns
- **[Built-in Tools Reference](../reference/built-in-tools.md)**: Compare with built-in capabilities
- **[Troubleshooting](./troubleshooting.md)**: More debugging guidance