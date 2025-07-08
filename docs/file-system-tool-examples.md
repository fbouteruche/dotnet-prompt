# File System Tool - Usage Examples

This document demonstrates the enhanced File System Tool (FileSystemPlugin) functionality with working directory context and security.

## Basic File Operations

### Reading Files
```yaml
# File read function
{{invoke_tool: file_read
  filePath: "./config/settings.json"
  encoding: "utf-8"
}}
```

### Writing Files
```yaml
# File write function with directory creation
{{invoke_tool: file_write
  filePath: "./output/result.txt"
  content: "Generated content"
  createDirectories: true
  overwrite: true
}}
```

### Checking File Existence
```yaml
# File existence check
{{invoke_tool: file_exists
  filePath: "./data/input.csv"
}}
```

## Directory Operations

### Creating Directories
```yaml
# Directory creation
{{invoke_tool: create_directory
  directoryPath: "./output/reports"
}}
```

### Listing Directory Contents
```yaml
# List all files and directories
{{invoke_tool: list_directory
  directoryPath: "./src"
  pattern: "*"
  recursive: false
  includeHidden: false
}}

# List only C# source files recursively
{{invoke_tool: list_directory
  directoryPath: "./src"
  pattern: "*.cs"
  recursive: true
  maxDepth: 10
}}
```

## File Management

### Copying Files
```yaml
# Copy file with safety controls
{{invoke_tool: copy_file
  sourcePath: "./templates/template.txt"
  destinationPath: "./output/generated.txt"
  overwrite: false
}}
```

### Getting File Information
```yaml
# Get detailed file metadata
{{invoke_tool: get_file_info
  filePath: "./data/large-file.json"
}}
```

## Security Model

### Working Directory Context
The file system tool operates within a working directory context for security:

1. **CLI `--context` parameter** (highest priority)
2. **Current directory** where `dotnet prompt` executes
3. **Configurable via allowedDirectories** in tool configuration

### Security Policies
```json
{
  "FileSystem": {
    "AllowedDirectories": ["./data", "./output", "./temp"],
    "BlockedDirectories": ["bin", "obj", ".git", "node_modules"],
    "AllowedExtensions": [],
    "BlockedExtensions": [".exe", ".dll", ".so", ".dylib"],
    "MaxFileSizeBytes": 10485760,
    "MaxFilesPerOperation": 1000,
    "RequireConfirmationForDelete": true,
    "EnableAuditLogging": true
  }
}
```

## Advanced Usage Scenarios

### Project Analysis Workflow
```yaml
# 1. List project structure
{{invoke_tool: list_directory
  directoryPath: "."
  pattern: "*.csproj"
  recursive: true
}}

# 2. Read project file
{{invoke_tool: file_read
  filePath: "./MyProject.csproj"
}}

# 3. Create analysis output
{{invoke_tool: create_directory
  directoryPath: "./analysis"
}}

# 4. Write analysis results
{{invoke_tool: file_write
  filePath: "./analysis/project-structure.md"
  content: "# Project Analysis\n\n{{analysis_results}}"
  createDirectories: true
}}
```

### Code Generation Workflow
```yaml
# 1. Create output directories
{{invoke_tool: create_directory
  directoryPath: "./Generated/Models"
}}

# 2. Generate and write model files
{{invoke_tool: file_write
  filePath: "./Generated/Models/{{model_name}}.cs"
  content: "{{generated_code}}"
  createDirectories: false
  overwrite: false
}}

# 3. Copy template files
{{invoke_tool: copy_file
  sourcePath: "./Templates/BaseModel.cs"
  destinationPath: "./Generated/Models/BaseModel.cs"
  overwrite: true
}}
```

## Error Handling

The file system tool includes comprehensive error handling:

- **Security violations**: Access outside allowed directories
- **File not found**: Missing source files
- **Permission denied**: Insufficient file system permissions  
- **Size limits**: Files exceeding maximum size
- **Path validation**: Invalid or malformed paths

All errors are wrapped in `KernelException` with descriptive messages for workflow error handling.