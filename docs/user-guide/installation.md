# Installation Guide

Complete installation and setup guide for dotnet-prompt across different platforms and environments.

## System Requirements

### Minimum Requirements
- **.NET 8.0 SDK** or later
- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 18.04+, RHEL 8+)
- **Memory**: 2GB RAM minimum, 4GB recommended
- **Storage**: 500MB free space for installation and cache
- **Internet Connection**: Required for AI provider access and package installation

### Recommended Requirements
- **.NET 8.0 SDK** (latest version)
- **Memory**: 8GB RAM for large project analysis
- **Storage**: 2GB free space for comprehensive workflows
- **Git**: For version control integration workflows
- **Node.js 18+**: For MCP server integration (optional)
- **Docker**: For containerized workflow execution (optional)

## Installation Methods

### Method 1: NuGet Package (Recommended)

Once published to NuGet Gallery:

```bash
# Install globally as .NET tool
dotnet tool install -g dotnet-prompt

# Verify installation
dotnet prompt --version

# Update to latest version
dotnet tool update -g dotnet-prompt
```

### Method 2: GitHub Releases

Download pre-built binaries from GitHub releases:

```bash
# Download latest release (Linux/macOS)
curl -LO https://github.com/fbouteruche/dotnet-prompt/releases/latest/download/dotnet-prompt-linux-x64.tar.gz
tar -xzf dotnet-prompt-linux-x64.tar.gz
sudo mv dotnet-prompt /usr/local/bin/

# Download latest release (Windows)
# Download dotnet-prompt-windows-x64.zip from GitHub releases
# Extract and add to PATH
```

### Method 3: Build from Source

For development or custom builds:

```bash
# Clone repository
git clone https://github.com/fbouteruche/dotnet-prompt.git
cd dotnet-prompt

# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Pack and install tool
dotnet pack src/DotnetPrompt.Cli --configuration Release
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli

# Verify installation
dotnet prompt --version
```

### Method 4: Container Installation

Using Docker for isolated environments:

```bash
# Pull official image (when available)
docker pull ghcr.io/fbouteruche/dotnet-prompt:latest

# Run in container
docker run --rm -v $(pwd):/workspace ghcr.io/fbouteruche/dotnet-prompt:latest run workflow.prompt.md

# Create alias for easier usage
alias dotnet-prompt='docker run --rm -v $(pwd):/workspace ghcr.io/fbouteruche/dotnet-prompt:latest'
```

## Platform-Specific Installation

### Windows

#### Prerequisites
```powershell
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# Or download from https://dotnet.microsoft.com/download/dotnet/8.0
```

#### Installation
```powershell
# Install via .NET tool
dotnet tool install -g dotnet-prompt

# Add to PATH if needed (usually automatic)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Make PATH change permanent
[Environment]::SetEnvironmentVariable("PATH", $env:PATH, [EnvironmentVariableTarget]::User)
```

#### Verification
```powershell
# Test installation
dotnet prompt --version

# Test basic functionality
dotnet prompt run --help
```

### macOS

#### Prerequisites
```bash
# Install .NET 8.0 SDK via Homebrew
brew install dotnet

# Or download from Microsoft
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
```

#### Installation
```bash
# Install via .NET tool
dotnet tool install -g dotnet-prompt

# Add to PATH (add to ~/.zshrc or ~/.bash_profile)
export PATH="$PATH:$HOME/.dotnet/tools"

# Reload shell configuration
source ~/.zshrc  # or ~/.bash_profile
```

#### Verification
```bash
# Test installation
dotnet prompt --version

# Test with sample workflow
echo '---
name: "test"
model: "gpt-4o"
tools: ["file-system"]
---
# Test
Create a test file.' > test.prompt.md

dotnet prompt run test.prompt.md --dry-run
```

### Linux (Ubuntu/Debian)

#### Prerequisites
```bash
# Install .NET 8.0 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Or use snap
sudo snap install dotnet-sdk --classic --channel=8.0
```

#### Installation
```bash
# Install via .NET tool
dotnet tool install -g dotnet-prompt

# Add to PATH (add to ~/.bashrc)
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

#### Verification
```bash
# Test installation
dotnet prompt --version

# Check tool location
which dotnet-prompt
ls -la ~/.dotnet/tools/
```

### Linux (RHEL/CentOS/Fedora)

#### Prerequisites
```bash
# RHEL/CentOS 8+
sudo dnf install dotnet-sdk-8.0

# Fedora
sudo dnf install dotnet-sdk-8.0

# Or manual installation
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
```

#### Installation
```bash
# Follow same steps as Ubuntu
dotnet tool install -g dotnet-prompt

# Add to PATH
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc
```

## Initial Configuration

### Quick Setup

Initialize with basic configuration:

```bash
# Initialize configuration
dotnet prompt init

# Set up GitHub Models provider (recommended)
dotnet prompt config set providers.github.token "${GITHUB_TOKEN}" --global
dotnet prompt config set default_provider github --global

# Verify configuration
dotnet prompt config show
```

### Provider Setup

#### GitHub Models (Recommended)
```bash
# Get token from https://github.com/settings/tokens
export GITHUB_TOKEN="ghp_xxxxxxxxxxxx"

# Configure provider
dotnet prompt config set providers.github.token "${GITHUB_TOKEN}" --global
dotnet prompt config set providers.github.endpoint "https://models.inference.ai.azure.com" --global
dotnet prompt config set default_provider github --global
dotnet prompt config set default_model gpt-4o --global
```

#### OpenAI
```bash
# Get API key from https://platform.openai.com/api-keys
export OPENAI_API_KEY="sk-xxxxxxxxxxxx"

# Configure provider
dotnet prompt config set providers.openai.api_key "${OPENAI_API_KEY}" --global
dotnet prompt config set default_provider openai --global
dotnet prompt config set default_model gpt-4 --global
```

#### Azure OpenAI
```bash
# Set up Azure credentials
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_API_KEY="xxxxxxxxxxxx"

# Configure provider
dotnet prompt config set providers.azure.endpoint "${AZURE_OPENAI_ENDPOINT}" --global
dotnet prompt config set providers.azure.api_key "${AZURE_OPENAI_API_KEY}" --global
dotnet prompt config set providers.azure.deployment_name "gpt-4" --global
dotnet prompt config set default_provider azure --global
```

#### Anthropic
```bash
# Get API key from https://console.anthropic.com/
export ANTHROPIC_API_KEY="sk-ant-xxxxxxxxxxxx"

# Configure provider
dotnet prompt config set providers.anthropic.api_key "${ANTHROPIC_API_KEY}" --global
dotnet prompt config set default_provider anthropic --global
dotnet prompt config set default_model claude-3-sonnet --global
```

### Verification Test

Test your setup with a simple workflow:

```bash
# Create test workflow
cat > hello-test.prompt.md << 'EOF'
---
name: "installation-test"
model: "gpt-4o"
tools: ["file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 500
---

# Installation Test

Create a file called `installation-test.txt` with the following content:
- Current date and time
- Confirmation that dotnet-prompt is working correctly
- System information summary

This is a test to verify that dotnet-prompt is installed and configured properly.
EOF

# Run test workflow
dotnet prompt run hello-test.prompt.md

# Check results
cat installation-test.txt
```

## MCP Server Setup (Optional)

Model Context Protocol servers extend dotnet-prompt capabilities:

### Prerequisites
```bash
# Install Node.js (for npm-based MCP servers)
# macOS
brew install node

# Ubuntu/Debian
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Windows
winget install OpenJS.NodeJS
```

### Popular MCP Servers

#### Filesystem MCP Server
```bash
# Install enhanced filesystem server
npm install -g @modelcontextprotocol/server-filesystem

# Test installation
npx @modelcontextprotocol/server-filesystem --help
```

#### Git MCP Server
```bash
# Install Git integration server
npm install -g @modelcontextprotocol/server-git

# Test installation
npx @modelcontextprotocol/server-git --help
```

#### GitHub MCP Server
```bash
# Install GitHub API server
npm install -g @modelcontextprotocol/server-github

# Test installation
npx @modelcontextprotocol/server-github --help
```

### MCP Server Configuration

Configure MCP servers in dotnet-prompt:

```bash
# Configure MCP servers globally
dotnet prompt config set mcp_servers.filesystem-mcp.command npx --global
dotnet prompt config set mcp_servers.filesystem-mcp.args '["@modelcontextprotocol/server-filesystem"]' --global

# Test MCP server connectivity
dotnet prompt mcp status
dotnet prompt mcp test filesystem-mcp
```

## Development Environment Setup

For contributors and advanced users:

### Development Prerequisites
```bash
# Install development tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-reportgenerator-globaltool

# Install testing tools
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package FluentAssertions
```

### IDE Setup

#### Visual Studio Code
```bash
# Install recommended extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension humao.rest-client

# Open project
code dotnet-prompt/
```

#### Visual Studio
```bash
# Open solution file
start dotnet-prompt/DotnetPrompt.sln
```

#### JetBrains Rider
```bash
# Open solution
rider dotnet-prompt/DotnetPrompt.sln
```

### Build and Test

```bash
# Navigate to project directory
cd dotnet-prompt

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run integration tests
dotnet test tests/DotnetPrompt.IntegrationTests

# Format code
dotnet format

# Generate test coverage report
dotnet test --collect:"XPlat Code Coverage"
dotnet tool run reportgenerator -reports:TestResults/*/coverage.cobertura.xml -targetdir:coverage-report
```

## Troubleshooting Installation

### Common Issues

#### .NET SDK Not Found
```bash
# Check .NET installation
dotnet --version
dotnet --list-sdks

# If not found, install .NET 8.0 SDK
# Follow platform-specific instructions above
```

#### Tool Installation Fails
```bash
# Clear tool cache
dotnet tool uninstall -g dotnet-prompt
dotnet nuget locals all --clear

# Reinstall
dotnet tool install -g dotnet-prompt

# If still failing, install from source
git clone https://github.com/fbouteruche/dotnet-prompt.git
cd dotnet-prompt
dotnet pack src/DotnetPrompt.Cli --configuration Release
dotnet tool install --global --add-source ./src/DotnetPrompt.Cli/bin/Release DotnetPrompt.Cli
```

#### PATH Issues
```bash
# Windows PowerShell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Windows Command Prompt
set PATH=%PATH%;%USERPROFILE%\.dotnet\tools

# macOS/Linux Bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc

# macOS Zsh
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
source ~/.zshrc
```

#### Permission Issues (Linux/macOS)
```bash
# Fix tool directory permissions
chmod +x ~/.dotnet/tools/dotnet-prompt

# If global installation fails, try user installation
dotnet tool install --tool-path ~/.local/bin dotnet-prompt
echo 'export PATH="$PATH:$HOME/.local/bin"' >> ~/.bashrc
```

#### Configuration Issues
```bash
# Reset configuration
rm -rf ~/.dotnet-prompt/config.json
dotnet prompt init

# Validate configuration
dotnet prompt config validate

# Check configuration sources
dotnet prompt config show --sources
```

### Getting Help

If you encounter issues not covered here:

1. **Check Documentation**: Review the [troubleshooting guide](./troubleshooting.md)
2. **Search Issues**: Check [GitHub Issues](https://github.com/fbouteruche/dotnet-prompt/issues)
3. **Create Issue**: Report new issues with:
   - Operating system and version
   - .NET SDK version (`dotnet --version`)
   - Installation method used
   - Complete error messages
   - Steps to reproduce

## Post-Installation Setup

### Create First Workflow

```bash
# Create your first workflow
mkdir my-workflows
cd my-workflows

cat > analyze-project.prompt.md << 'EOF'
---
name: "analyze-project"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 3000
---

# Project Analysis

Analyze the current .NET project and provide:

1. **Project Overview**: Type, framework, and purpose
2. **Dependencies**: Key packages and their roles
3. **Structure**: Important directories and files
4. **Recommendations**: Suggestions for improvement

Save the analysis to `project-analysis-report.md`.
EOF

# Run the workflow
dotnet prompt run analyze-project.prompt.md
```

### Configure for Team Use

For team environments:

```bash
# Create project-level configuration
mkdir .dotnet-prompt
cat > .dotnet-prompt/config.json << 'EOF'
{
  "default_provider": "github",
  "default_model": "gpt-4o",
  "tool_configuration": {
    "project_analysis": {
      "excluded_directories": ["bin", "obj", ".git", "node_modules"],
      "check_vulnerabilities": true
    },
    "build_test": {
      "default_configuration": "Release",
      "collect_coverage": true,
      "coverage_threshold": 80
    }
  }
}
EOF

# Add to version control
git add .dotnet-prompt/config.json
git commit -m "Add dotnet-prompt team configuration"
```

### Set Up CI/CD Integration

For automated workflows in CI/CD:

```yaml
# .github/workflows/dotnet-prompt.yml
name: dotnet-prompt workflows
on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install dotnet-prompt
        run: dotnet tool install -g dotnet-prompt
      
      - name: Run project analysis
        run: dotnet prompt run workflows/ci-analysis.prompt.md
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: analysis-results
          path: analysis-*.md
```

Congratulations! You now have dotnet-prompt installed and configured. Start exploring the [user guide](../user-guide/getting-started.md) to learn how to create powerful AI-driven workflows.