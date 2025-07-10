---
name: "enhanced-file-operations"
model: "gpt-4o"
tools: ["project-analysis"]

dotnet-prompt.mcp:
  - server: "filesystem-mcp"
    version: "1.0.0"
    config:
      allowed_directories: ["./src", "./docs", "./tests"]
      max_file_size: "10MB"
      backup_enabled: true
      compression_enabled: true

config:
  temperature: 0.5
  maxOutputTokens: 3500

input:
  schema:
    operation_type:
      type: string
      enum: ["cleanup", "organization", "analysis", "migration"]
      description: "Type of file operation to perform"
      default: "analysis"
    create_backups:
      type: boolean
      description: "Create backups before making changes"
      default: true
---

# Enhanced File Operations Workflow

Perform advanced file system operations using enhanced MCP filesystem capabilities.

## Operation Type: {{operation_type}}

{{#eq operation_type "cleanup"}}
## File Cleanup Operations

### 1. Duplicate File Detection
- Scan for duplicate files across the project
- Compare file content, not just names
- Identify candidates for removal or consolidation

### 2. Unused File Identification
- Find files that aren't referenced in project files
- Identify orphaned assets or resources
- Locate temporary or backup files that can be cleaned

### 3. Large File Analysis
- Identify unusually large files that might need compression
- Find binary files that shouldn't be in source control
- Locate log files or data files that could be archived

### 4. Cleanup Execution
{{#if create_backups}}
- Create backups before any deletions
{{/if}}
- Remove identified duplicate and unused files
- Compress large files where appropriate
- Generate cleanup summary report
{{/eq}}

{{#eq operation_type "organization"}}
## File Organization Operations

### 1. Structure Analysis
- Analyze current file organization patterns
- Identify inconsistencies in naming or placement
- Check for proper separation of concerns in file structure

### 2. Reorganization Plan
- Suggest improved file organization structure
- Identify files that should be moved or renamed
- Plan directory structure improvements

### 3. Implementation
{{#if create_backups}}
- Create full backup of current structure
{{/if}}
- Execute file moves and renames according to plan
- Update any references to moved files
- Validate that reorganization doesn't break functionality

### 4. Documentation Updates
- Update documentation to reflect new structure
- Create file organization guidelines for team
- Generate migration guide for developers
{{/eq}}

{{#eq operation_type "analysis"}}
## File System Analysis

### 1. Comprehensive File Inventory
- Catalog all files with metadata (size, type, modification dates)
- Analyze file type distribution and usage patterns
- Identify key files and their relationships

### 2. Access Pattern Analysis
- Identify frequently modified files
- Find files that haven't been touched recently
- Analyze file dependencies and relationships

### 3. Storage Optimization Opportunities
- Identify files suitable for compression
- Find redundant or duplicate content
- Suggest archival candidates

### 4. Security and Compliance Review
- Check for sensitive files in inappropriate locations
- Verify file permissions and access controls
- Identify potential security risks
{{/eq}}

{{#eq operation_type "migration"}}
## File Migration Operations

### 1. Migration Planning
- Analyze current file structure and target structure
- Identify all files that need to be moved or transformed
- Plan migration order to minimize disruption

### 2. Compatibility Checking
- Verify that target locations are appropriate
- Check for naming conflicts or restrictions
- Ensure all references can be updated

### 3. Migration Execution
{{#if create_backups}}
- Create comprehensive backup before migration
{{/if}}
- Execute file moves with progress tracking
- Update all file references and links
- Validate migration success

### 4. Post-Migration Validation
- Verify all files migrated successfully
- Test that project still builds and functions
- Update documentation and configuration files
{{/eq}}

## Project Integration

### Project File Updates
- Update project files if file locations changed
- Modify build scripts and configuration as needed
- Ensure all references are updated correctly

### Documentation Generation
- Create file organization documentation
- Generate change logs for major reorganizations
- Update developer onboarding guides if structure changed

## Reporting

Create a comprehensive report `enhanced-file-operations-report.md` with:

### 📋 Operation Summary
- Type of operation performed
- Files affected and changes made
- Backup locations and restoration instructions

### 📊 File System Metrics
- Before and after file counts and sizes
- Storage space saved or reorganized
- File type distribution analysis

### 🔍 Key Findings
- Important discoveries about file organization
- Potential issues identified and resolved
- Optimization opportunities implemented

### ⚠️ Warnings and Considerations
- Any potential issues or risks identified
- Files or operations that require manual attention
- Recommendations for ongoing file management

### 🎯 Recommendations
- Best practices for maintaining file organization
- Suggested policies or guidelines for team
- Tools or automation opportunities

{{#if create_backups}}
### 💾 Backup Information
- Location of backup files
- Instructions for restoration if needed
- Backup verification and integrity checks
{{/if}}

## Safety Measures

- Always verify backup integrity before making changes
- Test changes in a safe environment when possible
- Provide clear rollback instructions for all operations
- Document all changes for audit trail

Focus on safe, reversible operations that improve project organization and maintainability.