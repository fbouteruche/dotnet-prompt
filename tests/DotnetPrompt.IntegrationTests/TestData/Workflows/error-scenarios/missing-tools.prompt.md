---
name: "missing-tools"
model: "gpt-4o"
tools: ["nonexistent-plugin", "another-invalid-tool", "file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 2000
---

# Missing Tools Test

This workflow references tools that don't exist to test tool validation error handling.

## Referenced Tools

This workflow declares the following tools:
- `nonexistent-plugin` - ❌ This plugin does not exist
- `another-invalid-tool` - ❌ This is also invalid
- `file-system` - ✅ This plugin exists and should be valid

## Expected Validation Behavior

The validation system should:

1. **Identify Missing Tools**: Detect that `nonexistent-plugin` and `another-invalid-tool` are not available
2. **Provide Clear Error Messages**: List the specific tools that could not be found
3. **Suggest Alternatives**: If possible, suggest similar or available tool names
4. **Exit Appropriately**: Return WorkflowValidationError (3) exit code

## Error Message Expectations

The error output should include:
- Names of the invalid tools
- List of available tools for reference
- Clear indication that validation failed due to missing tools
- Suggestion to check tool configuration or spelling

## Valid Tools Available

For reference, the following tools should be available:
- `file-system` - File operations plugin
- `project-analysis` - .NET project analysis plugin
- `sub-workflow` - Sub-workflow execution plugin

## Test Content

This workflow content should never be processed due to tool validation failures.
The system should fail early during the validation phase when it cannot resolve the required tools.

If this content is being processed, it indicates that tool validation is not working properly.