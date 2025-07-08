---
name: "file-operations-workflow"
model: "gpt-4o"
tools: ["file-system"]

config:
  temperature: 0.2
  maxOutputTokens: 2500

input:
  default:
    workspace_dir: "./test-workspace"
    create_structure: true
    test_files: 3
    cleanup_after: false
  schema:
    workspace_dir:
      type: string
      description: "Directory for file operations testing"
      default: "./test-workspace"
    create_structure:
      type: boolean
      description: "Whether to create directory structure"
      default: true
    test_files:
      type: number
      description: "Number of test files to create"
      default: 3
    cleanup_after:
      type: boolean
      description: "Whether to clean up after testing"
      default: false
---

# FileSystem Plugin Testing Workflow

This workflow comprehensively tests the FileSystemPlugin functionality through various file operations.

## Setup Phase

{{#if create_structure}}
**Creating Test Directory Structure**

Create the following directory structure at {{workspace_dir}}:
```
{{workspace_dir}}/
├── src/
│   ├── Controllers/
│   ├── Models/
│   └── Services/
├── tests/
│   ├── Unit/
│   └── Integration/
├── docs/
└── config/
```

Use the file-system plugin to create these directories systematically.
{{/if}}

## File Creation Tests

Create {{test_files}} test files with different content types:

1. **Text File**: `{{workspace_dir}}/README.md`
   ```markdown
   # Test Project
   
   This is a test README file created by the FileSystem plugin.
   Generated on: {{current_timestamp}}
   ```

2. **JSON Configuration**: `{{workspace_dir}}/config/settings.json`
   ```json
   {
     "environment": "test",
     "debug": true,
     "features": {
       "fileOperations": true,
       "validation": true
     }
   }
   ```

3. **C# Source File**: `{{workspace_dir}}/src/Models/TestModel.cs`
   ```csharp
   namespace TestProject.Models
   {
       public class TestModel
       {
           public int Id { get; set; }
           public string Name { get; set; } = string.Empty;
           public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
       }
   }
   ```

## File Reading Tests

Read back the created files to verify content:

1. Read `{{workspace_dir}}/README.md` and verify it contains the timestamp
2. Read `{{workspace_dir}}/config/settings.json` and validate JSON structure
3. Read `{{workspace_dir}}/src/Models/TestModel.cs` and verify C# syntax

## Directory Listing Tests

Test directory listing functionality:

1. List contents of `{{workspace_dir}}/src/` directory
2. List all `.cs` files recursively
3. List all `.json` files with filtering
4. Verify directory structure matches expected layout

## File Modification Tests

Test file update operations:

1. Append new content to `{{workspace_dir}}/README.md`:
   ```markdown
   
   ## Updates
   - Modified on: {{current_timestamp}}
   - Test status: File operations working correctly
   ```

2. Update `{{workspace_dir}}/config/settings.json` to add new property:
   ```json
   "lastModified": "{{current_timestamp}}"
   ```

## Security and Validation Tests

Test security boundaries and validation:

1. **Path Validation**: Attempt to access files outside allowed directories (should fail)
2. **File Size Limits**: Test with files of various sizes
3. **Special Characters**: Test file names with Unicode characters
4. **Concurrent Access**: Test multiple file operations

## Error Handling Tests

Test error scenarios:

1. **Non-existent File**: Try to read a file that doesn't exist
2. **Invalid Path**: Use invalid path characters
3. **Permission Issues**: Test with restricted directories (if applicable)
4. **Disk Space**: Test behavior with large file operations

## Performance Tests

Test performance characteristics:

1. **Bulk Operations**: Create/read/delete multiple files
2. **Large Files**: Test with files of significant size
3. **Deep Directory**: Test with deeply nested directory structures

{{#if cleanup_after}}
## Cleanup Phase

Clean up all test files and directories:

1. Remove all created files in `{{workspace_dir}}/`
2. Remove directory structure
3. Verify cleanup completed successfully

**Note**: Cleanup is enabled - all test artifacts will be removed.
{{else}}
## Preservation Note

Cleanup is disabled - test files will remain at `{{workspace_dir}}/` for manual inspection.
{{/if}}

## Expected Results

This workflow should demonstrate:
- ✅ Successful file creation with various content types
- ✅ Accurate file reading and content verification
- ✅ Proper directory listing and filtering
- ✅ Secure file operations within boundaries
- ✅ Appropriate error handling for invalid operations
- ✅ Good performance characteristics

## Validation

After completing all operations, provide a summary including:
- Number of files successfully created
- Directory structure verification
- Any errors encountered and how they were handled
- Performance observations
- Security boundary validations

Complete this comprehensive FileSystem plugin test and report the results.