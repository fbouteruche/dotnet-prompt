---
name: "dependency-updater"
model: "gpt-4o"
tools: ["project-analysis", "file-system"]

config:
  temperature: 0.3
  maxOutputTokens: 3500

input:
  schema:
    check_security:
      type: boolean
      description: "Include security vulnerability check"
      default: true
    create_update_script:
      type: boolean
      description: "Create script to apply updates"
      default: true
---

# Dependency Update Analyzer

Analyze project dependencies and create an update plan with security considerations.

## Analysis Tasks

### 1. Current Dependency Inventory
- List all NuGet packages with current versions
- Identify framework references and versions
- Document package sources and configurations

### 2. Update Availability Check
- Research which packages have newer versions available
- Identify major vs minor vs patch updates
- Note any breaking changes in newer versions

{{#if check_security}}
### 3. Security Analysis
- Check for known security vulnerabilities in current packages
- Prioritize security updates
- Identify packages with security advisories
{{/if}}

### 4. Compatibility Assessment
- Check .NET framework compatibility for updates
- Identify potential breaking changes
- Assess update complexity and risk

### 5. Update Prioritization
- Categorize updates by importance:
  - **Critical**: Security fixes and major bugs
  - **Important**: Performance improvements and new features
  - **Optional**: Minor improvements and optimizations

## Report Generation

Create a comprehensive report `dependency-update-plan.md` with:

### 📦 Current Package Inventory
- Complete list of packages with versions
- Package purpose and usage in project
- Last update dates if available

### ⬆️ Available Updates
- Packages with newer versions
- Version changes (major.minor.patch)
- Release notes highlights for significant updates

{{#if check_security}}
### 🚨 Security Considerations
- Packages with known vulnerabilities
- Security advisories and CVE numbers
- Recommended security updates
{{/if}}

### 📋 Update Plan
- Recommended update order
- Risk assessment for each update
- Testing requirements after updates

### ⚠️ Potential Issues
- Breaking changes to watch for
- Compatibility concerns
- Additional work required (code changes, etc.)

{{#if create_update_script}}
### 🔧 Update Commands
- Specific dotnet commands to update packages
- PowerShell/bash script to automate updates
- Rollback commands in case of issues
{{/if}}

### ✅ Verification Steps
- How to verify updates were successful
- Tests to run after updates
- Performance checks to perform

## Additional Outputs

{{#if create_update_script}}
Create update scripts:
- `update-dependencies.ps1` (PowerShell)
- `update-dependencies.sh` (Bash)
- Include error handling and rollback options
{{/if}}

Ensure all recommendations are specific, actionable, and include the exact commands needed to perform updates safely.