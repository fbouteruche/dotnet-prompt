# File System Tool Specification

## Overview

This document defines the detailed specification for the File System Tool, which provides secure and controlled file system operations for reading, writing, and manipulating files and directories within workflow execution contexts.

## Status
🚧 **DRAFT** - Requires detailed specification

## Purpose

The File System Tool provides essential file and directory operations for AI workflows while maintaining security boundaries and preventing unauthorized access to sensitive system areas. It enables workflows to read configuration files, generate code, create documentation, and manage project artifacts.

## Core Functionality

### File Operations
- Read file contents with encoding detection and conversion
- Write file contents with backup and safety mechanisms
- Copy, move, and delete files with confirmation controls
- File metadata extraction (size, dates, permissions)

### Directory Operations
- List directory contents with filtering and recursion
- Create directory structures
- Directory tree navigation and search
- Bulk file operations within directories

### Security and Safety
- Path validation and sandboxing
- Access control and permission checking
- Audit logging for destructive operations
- Backup creation for file modifications

## Available Operations

### Read File Operation
Read and return file contents with encoding support.

**Input Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `file_path` | string | ✅ | - | Path to file to read |
| `encoding` | string | ❌ | "utf-8" | Text encoding |
| `max_size_mb` | int | ❌ | 10 | Maximum file size limit |
| `max_lines` | int | ❌ | 0 | Maximum lines to read (0 = all) |
| `skip_lines` | int | ❌ | 0 | Lines to skip from beginning |

### Write File Operation
Write content to files with safety mechanisms.

**Input Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `file_path` | string | ✅ | - | Path to file to write |
| `content` | string | ✅ | - | Content to write |
| `encoding` | string | ❌ | "utf-8" | Text encoding |
| `create_backup` | bool | ❌ | true | Create backup before overwrite |
| `overwrite` | bool | ❌ | true | Allow overwriting existing files |

### List Directory Operation
List and filter directory contents.

**Input Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `directory_path` | string | ✅ | - | Path to directory to list |
| `pattern` | string | ❌ | "*" | File pattern filter |
| `recursive` | bool | ❌ | false | Include subdirectories |
| `include_hidden` | bool | ❌ | false | Include hidden files |
| `max_depth` | int | ❌ | 0 | Maximum recursion depth |

### Create Directory Operation
Create directory structures.

**Input Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `directory_path` | string | ✅ | - | Path to directory to create |
| `recursive` | bool | ❌ | true | Create parent directories |

### File Management Operations
Copy, move, and delete files with safety controls.

**Copy File Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `source_path` | string | ✅ | - | Source file path |
| `destination_path` | string | ✅ | - | Destination file path |
| `overwrite` | bool | ❌ | false | Allow overwriting destination |

## Output Formats

### File Content Result
```typescript
interface FileContentResult {
    content: string;
    encoding: string;
    sizeBytes: number;
    lineCount: number;
    lastModified: DateTime;
    checksum: string;
}
```

### Directory Listing Result
```typescript
interface DirectoryListingResult {
    path: string;
    items: FileSystemItem[];
    totalFiles: number;
    totalDirectories: number;
    totalSizeBytes: number;
    truncated: boolean;
}

interface FileSystemItem {
    name: string;
    path: string;
    type: "file" | "directory";
    sizeBytes: number;
    created: DateTime;
    modified: DateTime;
    extension?: string;
    isHidden: boolean;
}
```

### File Operation Result
```typescript
interface FileOperationResult {
    success: boolean;
    operation: string;
    path: string;
    sizeBytes?: number;
    backupPath?: string;
    timestamp: DateTime;
}
```

## Security Model

### Path Resolution Behavior

#### Relative Path Handling
- All relative paths are resolved against the working directory context
- Working directory is determined by CLI `--context` parameter or current execution directory
- Path traversal attempts (`../`) are validated against allowed directory boundaries

#### Absolute Path Validation
- Absolute paths must fall within configured `allowedDirectories`
- System directories are blocked by default unless explicitly configured
- Cross-platform path normalization ensures consistent behavior

#### Example Path Resolution
```typescript
// Working directory: /home/user/project
// Allowed directories: ["/home/user/project"]

"./src/Program.cs"           → "/home/user/project/src/Program.cs" ✅
"../other-project/file.cs"   → "/home/user/other-project/file.cs" ❌ (outside boundary)
"/tmp/temp.json"             → "/tmp/temp.json" ❌ (not in allowed directories)

// With extended configuration:
// Allowed directories: ["/home/user/project", "/shared/templates"]
"/shared/templates/base.cs"  → "/shared/templates/base.cs" ✅
```

### Default Security Boundary
**Working Directory Context**: The file system tool operates within the working directory context by default. The working directory is determined by:
1. CLI `--context` parameter if specified
2. Current directory where `dotnet prompt` executes
3. Configurable via `allowedDirectories` in tool configuration

This provides security by default while maintaining flexibility for legitimate use cases that require broader file system access.

### Path Validation and Sandboxing
- All paths validated against allowed directories (default: working directory)
- Parent directory traversal (`../`) restrictions
- Absolute path validation and conversion
- System directory access prevention
- Relative paths resolved against working directory context

### Access Control Policies
```typescript
interface FileSystemSecurityPolicy {
    allowedDirectories: string[];      // Default: [workingDirectory]
    blockedDirectories: string[];      // Additional restrictions within allowed dirs
    allowedExtensions: string[];       // File type allowlist (empty = all allowed)
    blockedExtensions: string[];       // File type blocklist
    maxFileSize: number;               // Maximum file size in bytes
    maxFilesPerOperation: number;      // Bulk operation limits
    requireConfirmationForDelete: boolean;  // Safety for destructive operations
    enableAuditLogging: boolean;       // Track all file operations
}
```

**Default Policy:**
```typescript
const defaultPolicy: FileSystemSecurityPolicy = {
    allowedDirectories: [process.cwd()],  // Working directory only
    blockedDirectories: ["bin", "obj", ".git", "node_modules"],
    allowedExtensions: [],  // All extensions allowed by default
    blockedExtensions: [".exe", ".dll", ".so", ".dylib"],  // Binary executables
    maxFileSize: 10 * 1024 * 1024,  // 10MB
    maxFilesPerOperation: 1000,
    requireConfirmationForDelete: true,
    enableAuditLogging: true
};
```

## Clarifying Questions

### 1. File Operation Scope
- What file operations should be supported beyond basic read/write?
- Should there be support for file compression/decompression?
- How should symbolic links and shortcuts be handled?
- Should there be support for file attributes and permissions management?

### 2. Security and Sandboxing
- ✅ **Default security boundary**: Working directory context (CLI `--context` or current directory)
- How should the tool handle requests for system directories? (Block by default, require explicit configuration)
- Should there be configurable security policies per workflow? (Yes, via tool configuration)
- How should sensitive file detection and protection work? (File extension and content-based detection)

### 3. Binary File Handling
- How should binary files be detected and handled?
- Should there be support for reading binary files as base64?
- What size limits should apply to binary file operations?
- Should there be MIME type detection and validation?

### 4. Encoding and Text Processing
- Which text encodings should be supported?
- How should encoding detection work for files without BOM?
- Should there be support for encoding conversion?
- How should malformed text files be handled?

### 5. Large File Handling
- What strategies should be used for large file operations?
- Should there be streaming support for very large files?
- How should memory usage be controlled during file operations?
- Should there be progress reporting for large file operations?

### 6. File System Monitoring
- Should the tool support file system watching/monitoring?
- How should file change notifications be handled?
- Should there be support for detecting file modifications during workflow execution?
- How should concurrent file access be managed?

### 7. Backup and Recovery
- What backup strategies should be implemented?
- How should backup file naming and organization work?
- Should there be automatic cleanup of old backup files?
- How should backup restoration be handled?

### 8. Performance and Caching
- Should there be caching of frequently accessed files?
- How should file metadata caching work?
- What performance optimizations should be implemented?
- How should concurrent file operations be handled?

### 9. Error Handling and Recovery
- How should different types of file system errors be categorized?
- What retry logic should be implemented for transient failures?
- How should partial failures be handled in bulk operations?
- Should there be automatic error recovery mechanisms?

### 10. Integration with Development Tools
- How should the tool integrate with version control systems?
- Should there be support for temporary file management?
- How should generated files be tracked and managed?
- Should there be integration with build output directories?

## Example Usage Scenarios

### Basic File Operations (Working Directory Context)
```markdown
# Read configuration file (relative to working directory)
{{invoke_tool: read_file
  file_path: "./appsettings.json"
  encoding: "utf-8"
}}

# Write file with backup (within working directory)
{{invoke_tool: write_file
  file_path: "./appsettings.Development.json"
  content: "{{updated_config}}"
  create_backup: true
}}
```

### Project File Discovery
```markdown
Find all C# source files for analysis:

{{invoke_tool: list_directory
  directory_path: "./src"
  pattern: "*.cs"
  recursive: true
  max_depth: 10
}}
```

### Code Generation with Directory Structure
```markdown
Generate new source files:

{{invoke_tool: create_directory
  directory_path: "./Generated/Models"
}}

{{invoke_tool: write_file
  file_path: "./Generated/Models/{{class_name}}.cs"
  content: "{{generated_code}}"
  overwrite: false
}}
```

### Extended Access Configuration
```yaml
# dotnet-prompt.yaml - Configure broader access when needed
tool_configuration:
  file_system:
    allowed_directories:
      - "."                           # Working directory (default)
      - "../shared-resources"         # Relative path to shared resources
      - "/opt/templates"              # Absolute path for system templates
      - "${HOME}/dotnet-templates"    # Environment variable expansion
    blocked_directories:
      - "./bin"
      - "./obj"
      - "./.git"
      - "./node_modules"
    allowed_extensions:
      - ".cs"
      - ".json"
      - ".yaml"
      - ".md"
    blocked_extensions:
      - ".exe"
      - ".dll"
      - ".so"
    max_file_size: 10485760  # 10MB
    require_confirmation_for_delete: true
    enable_audit_logging: true
```

## Next Steps

1. ✅ **Define default security boundary** - Working directory context established
2. **Implement path validation and sandboxing logic**
3. **Create configuration integration with dotnet-prompt hierarchy**
4. **Design error handling and recovery mechanisms** 
5. **Implement file system monitoring capabilities** (future enhancement)
6. **Create performance optimization strategies**
7. **Implement the Semantic Kernel plugin interface**

## Implementation Guidance

### Security-First Architecture
```csharp
public class FileSystemPlugin
{
    private readonly FileSystemSecurityPolicy _policy;
    private readonly string _workingDirectory;
    
    private bool IsPathAllowed(string requestedPath)
    {
        var resolvedPath = Path.IsPathRooted(requestedPath) 
            ? requestedPath 
            : Path.Combine(_workingDirectory, requestedPath);
            
        var normalizedPath = Path.GetFullPath(resolvedPath);
        
        return _policy.AllowedDirectories.Any(allowedDir =>
            normalizedPath.StartsWith(Path.GetFullPath(allowedDir), 
                                    StringComparison.OrdinalIgnoreCase));
    }
}
```

### Configuration Integration
- Integrate with dotnet-prompt's 4-level configuration hierarchy
- Support environment variable expansion in paths
- Provide sensible defaults for .NET development workflows
- Enable per-workflow security policy overrides when explicitly configured
