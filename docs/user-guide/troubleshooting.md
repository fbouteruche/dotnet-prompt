# Troubleshooting Guide

Common issues, solutions, and debugging techniques for dotnet-prompt workflows.

## Installation and Setup Issues

### Tool Installation Problems

#### Issue: dotnet tool install fails
```
error: Failed to restore package 'DotnetPrompt.Cli'
```

**Solutions:**
1. **Check .NET SDK version**:
   ```bash
   dotnet --version  # Should be 8.0 or later
   ```

2. **Clear NuGet cache**:
   ```bash
   dotnet nuget locals all --clear
   ```

3. **Install from source**:
   ```bash
   git clone https://github.com/fbouteruche/dotnet-prompt.git
   cd dotnet-prompt
   dotnet pack src/DotnetPrompt.Cli --configuration Release
   dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli
   ```

#### Issue: Command not found after installation
```
bash: dotnet prompt: command not found
```

**Solutions:**
1. **Verify installation**:
   ```bash
   dotnet tool list -g | grep dotnet-prompt
   ```

2. **Check PATH environment**:
   ```bash
   echo $PATH | grep -i dotnet
   ```

3. **Restart shell** or reload environment variables

4. **Manual PATH setup** (if needed):
   ```bash
   export PATH="$PATH:$HOME/.dotnet/tools"
   ```

### Configuration Issues

#### Issue: Provider authentication fails
```
Error: Authentication failed for provider 'openai'
```

**Solutions:**
1. **Verify API key**:
   ```bash
   dotnet prompt config get providers.openai.api_key
   ```

2. **Set correct API key**:
   ```bash
   dotnet prompt config set providers.openai.api_key "sk-your-key-here" --global
   ```

3. **Check environment variables**:
   ```bash
   echo $OPENAI_API_KEY
   echo $GITHUB_TOKEN
   ```

4. **Test connection**:
   ```bash
   dotnet prompt run hello-world.prompt.md --provider openai --dry-run
   ```

#### Issue: Configuration file not found
```
Error: Configuration file not found at expected location
```

**Solutions:**
1. **Initialize configuration**:
   ```bash
   dotnet prompt init
   ```

2. **Check configuration hierarchy**:
   ```bash
   dotnet prompt config show --sources
   ```

3. **Manually create config**:
   ```bash
   mkdir -p ~/.dotnet-prompt
   echo '{"default_provider": "github"}' > ~/.dotnet-prompt/config.json
   ```

## Workflow Execution Issues

### Workflow Parsing Problems

#### Issue: YAML frontmatter syntax error
```
Error: Invalid YAML frontmatter: mapping values are not allowed here
```

**Solutions:**
1. **Validate YAML syntax**:
   - Check indentation (use spaces, not tabs)
   - Ensure proper key-value format
   - Validate quotes and special characters

2. **Use YAML validator online** or:
   ```bash
   python -c "import yaml; yaml.safe_load(open('workflow.prompt.md').read().split('---')[1])"
   ```

3. **Common YAML issues**:
   ```yaml
   # ❌ Wrong - missing quotes around value with special characters
   name: my-workflow:test
   
   # ✅ Correct - quotes around special characters
   name: "my-workflow:test"
   
   # ❌ Wrong - inconsistent indentation
   tools:
     - "file-system"
       - "project-analysis"
   
   # ✅ Correct - consistent indentation
   tools:
     - "file-system"
     - "project-analysis"
   ```

#### Issue: Unknown tool specified
```
Error: Tool 'my-custom-tool' not available
```

**Solutions:**
1. **Check available tools**:
   ```bash
   dotnet prompt validate workflow.prompt.md --check-tools
   ```

2. **Verify tool name spelling**:
   - Built-in tools: `project-analysis`, `build-test`, `file-system`, `sub-workflow`
   - Check MCP server configuration for external tools

3. **Fix tool declaration**:
   ```yaml
   # ❌ Wrong - typo in tool name
   tools: ["project-analisys"]
   
   # ✅ Correct - proper tool name
   tools: ["project-analysis"]
   ```

### Runtime Execution Problems

#### Issue: Workflow timeout
```
Error: Workflow execution timeout after 300 seconds
```

**Solutions:**
1. **Increase timeout**:
   ```bash
   dotnet prompt run workflow.prompt.md --timeout 600
   ```

2. **Set environment variable**:
   ```bash
   export DOTNET_PROMPT_TIMEOUT=600
   dotnet prompt run workflow.prompt.md
   ```

3. **Optimize workflow**:
   - Break into smaller sub-workflows
   - Reduce scope or complexity
   - Use parallel processing where possible

#### Issue: File access denied
```
Error: Access denied to file '/restricted/path'
```

**Solutions:**
1. **Check working directory context**:
   ```bash
   dotnet prompt run workflow.prompt.md --context ./src
   ```

2. **Verify file permissions**:
   ```bash
   ls -la path/to/file
   chmod 644 path/to/file  # If needed
   ```

3. **Review file system tool configuration**:
   ```json
   {
     "tool_configuration": {
       "file_system": {
         "working_directory_only": true,
         "allowed_extensions": [".cs", ".json", ".md"],
         "denied_paths": [".git", "bin", "obj"]
       }
     }
   }
   ```

#### Issue: Build failures in workflow
```
Error: Build failed - The project file could not be evaluated
```

**Solutions:**
1. **Check project file validity**:
   ```bash
   dotnet build  # Test build manually
   ```

2. **Verify .NET SDK version**:
   ```bash
   dotnet --version
   cat global.json  # Check if specific version required
   ```

3. **Restore packages explicitly**:
   ```bash
   dotnet restore
   dotnet prompt run workflow.prompt.md
   ```

4. **Use verbose output**:
   ```bash
   dotnet prompt run workflow.prompt.md --verbose
   ```

## MCP Integration Issues

### MCP Server Connection Problems

#### Issue: MCP server not found
```
Error: MCP server 'filesystem-mcp' not found
```

**Solutions:**
1. **Check server installation**:
   ```bash
   npm list -g @modelcontextprotocol/server-filesystem
   ```

2. **Install missing server**:
   ```bash
   npm install -g @modelcontextprotocol/server-filesystem
   ```

3. **Verify server configuration**:
   ```bash
   dotnet prompt mcp list
   dotnet prompt mcp status filesystem-mcp
   ```

4. **Test server connectivity**:
   ```bash
   dotnet prompt mcp test filesystem-mcp --verbose
   ```

#### Issue: MCP server timeout
```
Error: MCP server connection timeout after 30000ms
```

**Solutions:**
1. **Increase timeout in workflow**:
   ```yaml
   dotnet-prompt.mcp:
     - server: "filesystem-mcp"
       config:
         timeout: 60000  # 60 seconds
   ```

2. **Check server health**:
   ```bash
   dotnet prompt mcp status
   ```

3. **Review server logs**:
   ```bash
   dotnet prompt mcp logs filesystem-mcp --follow
   ```

4. **Restart server**:
   ```bash
   dotnet prompt mcp stop filesystem-mcp
   dotnet prompt mcp start filesystem-mcp
   ```

### MCP Configuration Issues

#### Issue: Invalid MCP server configuration
```
Error: Invalid configuration for MCP server 'github-mcp'
```

**Solutions:**
1. **Check required environment variables**:
   ```bash
   echo $GITHUB_TOKEN
   ```

2. **Validate configuration format**:
   ```yaml
   dotnet-prompt.mcp:
     - server: "github-mcp"
       version: "2.1.0"
       config:
         token: "${GITHUB_TOKEN}"  # Environment variable
         default_repo: "owner/repo"
   ```

3. **Review server documentation** for required configuration fields

## Performance Issues

### Slow Workflow Execution

#### Issue: Workflows taking too long to complete
```
Workflow execution is unusually slow
```

**Solutions:**
1. **Enable verbose logging** to identify bottlenecks:
   ```bash
   dotnet prompt run workflow.prompt.md --verbose
   ```

2. **Profile tool usage**:
   - Check if project-analysis is scanning too many files
   - Verify build-test isn't running excessive tests
   - Review file-system operations for large file handling

3. **Optimize workflow scope**:
   ```yaml
   # Limit project analysis scope
   dotnet-prompt.tool-config:
     project_analysis:
       excluded_directories: ["bin", "obj", ".git", "node_modules"]
       max_file_size_bytes: 1048576  # 1MB limit
   ```

4. **Use parallel processing**:
   ```yaml
   dotnet-prompt.sub-workflows:
     - name: "task1"
       path: "./task1.prompt.md"
       parallel_group: "group1"
     - name: "task2"
       path: "./task2.prompt.md"
       parallel_group: "group1"
   ```

### Memory Issues

#### Issue: Out of memory during large project analysis
```
Error: Insufficient memory to complete operation
```

**Solutions:**
1. **Increase memory limits** (if running in containers):
   ```bash
   docker run -m 4g dotnet-prompt-image
   ```

2. **Reduce analysis scope**:
   ```yaml
   tools: ["project-analysis"]
   # Add configuration to limit scope
   ```

3. **Process in batches**:
   - Split large projects into smaller analysis chunks
   - Use sub-workflows for different project areas
   - Implement streaming for large file operations

## AI Provider Issues

### Model Access Problems

#### Issue: Model not available
```
Error: Model 'gpt-4o' not available for provider 'openai'
```

**Solutions:**
1. **Check model availability**:
   ```bash
   dotnet prompt config show
   ```

2. **Use supported model**:
   ```yaml
   # Check provider documentation for available models
   model: "gpt-3.5-turbo"  # Alternative model
   ```

3. **Verify provider configuration**:
   ```bash
   dotnet prompt config get providers.openai
   ```

#### Issue: Rate limiting
```
Error: Rate limit exceeded for API requests
```

**Solutions:**
1. **Implement retry logic** (automatic in newer versions)

2. **Reduce request frequency**:
   - Use lower temperature for deterministic outputs
   - Batch operations where possible
   - Implement caching for repeated requests

3. **Switch providers temporarily**:
   ```bash
   dotnet prompt run workflow.prompt.md --provider github
   ```

## Progress and Resume Issues

### Progress File Problems

#### Issue: Cannot resume workflow
```
Error: Progress file incompatible with current workflow
```

**Solutions:**
1. **Check progress file compatibility**:
   ```bash
   dotnet prompt resume workflow.prompt.md --force
   ```

2. **Start fresh if needed**:
   ```bash
   rm workflow.progress.md
   dotnet prompt run workflow.prompt.md
   ```

3. **Validate workflow changes**:
   - Ensure workflow structure hasn't changed significantly
   - Check that tools and parameters are still compatible

#### Issue: Progress file corruption
```
Error: Cannot parse progress file
```

**Solutions:**
1. **Backup and remove corrupted file**:
   ```bash
   mv workflow.progress.md workflow.progress.md.backup
   dotnet prompt run workflow.prompt.md
   ```

2. **Clean all progress files**:
   ```bash
   dotnet prompt clean --progress
   ```

## Debugging Techniques

### Verbose Logging

Enable comprehensive logging for troubleshooting:

```bash
# Maximum verbosity
export DOTNET_PROMPT_VERBOSE=true
dotnet prompt run workflow.prompt.md --verbose

# MCP-specific debugging
dotnet prompt run workflow.prompt.md --verbose --mcp-debug
```

### Dry Run Testing

Test workflows without execution:

```bash
# Validate syntax and configuration
dotnet prompt run workflow.prompt.md --dry-run

# Check tool availability
dotnet prompt validate workflow.prompt.md --check-tools --check-dependencies

# Strict validation
dotnet prompt validate workflow.prompt.md --strict
```

### Step-by-Step Debugging

Break complex workflows into smaller parts:

1. **Test individual tools**:
   ```yaml
   # Simple test workflow
   ---
   name: "debug-project-analysis"
   model: "gpt-4o"
   tools: ["project-analysis"]
   ---
   
   # Test Project Analysis
   
   Just analyze the project structure and report findings.
   ```

2. **Test parameter handling**:
   ```bash
   dotnet prompt run workflow.prompt.md --parameter test_param=value --dry-run
   ```

3. **Isolate sub-workflows**:
   Run sub-workflows independently to identify issues

### Log Analysis

Review logs for patterns and issues:

```bash
# Check system logs (Linux/macOS)
journalctl -u dotnet-prompt

# Review MCP server logs
dotnet prompt mcp logs server-name --lines 100

# Application logs (if configured)
tail -f ~/.dotnet-prompt/logs/dotnet-prompt.log
```

## Getting Help

### Community Resources

1. **GitHub Issues**: Report bugs and request features
   - Repository: https://github.com/fbouteruche/dotnet-prompt
   - Include reproduction steps and error messages

2. **Discussions**: Ask questions and share workflows
   - Use GitHub Discussions for general questions
   - Share successful workflow patterns

3. **Documentation**: Check comprehensive guides
   - [User Guide](../user-guide/) for usage instructions
   - [Reference](../reference/) for detailed API information

### Diagnostic Information

When reporting issues, include:

```bash
# Version information
dotnet prompt --version

# Configuration details
dotnet prompt config show --sources

# Tool availability
dotnet prompt validate workflow.prompt.md --check-tools

# MCP server status
dotnet prompt mcp status

# Environment information
dotnet --info
```

### Professional Support

For enterprise users:
- Dedicated support channels
- Custom workflow development
- Training and best practices consulting
- Priority issue resolution

Contact information and support tiers available in the main repository documentation.

## Prevention Best Practices

### Workflow Design

1. **Start Simple**: Begin with basic workflows and add complexity gradually
2. **Use Validation**: Always validate workflows before deployment
3. **Test Incrementally**: Test changes in isolation before integration
4. **Document Assumptions**: Clear parameter descriptions and requirements

### Environment Management

1. **Version Pinning**: Specify exact versions for reproducibility
2. **Environment Isolation**: Use separate environments for development and production
3. **Configuration Management**: Use environment variables for sensitive data
4. **Regular Updates**: Keep tools and dependencies current

### Monitoring and Maintenance

1. **Regular Testing**: Periodically test workflows to ensure they still work
2. **Log Monitoring**: Set up log monitoring for production workflows
3. **Performance Tracking**: Monitor execution times and resource usage
4. **Error Alerting**: Set up alerts for workflow failures

This troubleshooting guide covers the most common issues and provides systematic approaches to diagnosing and resolving problems with dotnet-prompt workflows.