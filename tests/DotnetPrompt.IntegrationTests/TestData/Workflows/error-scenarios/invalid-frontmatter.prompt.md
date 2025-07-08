---
name: "invalid-frontmatter"
model: "gpt-4o"
tools: ["file-system"]
invalid_field: "this should cause validation error"
config:
  temperature: 0.5
  maxOutputTokens: 2000
  invalid_config_field: "also invalid"
tools_typo: ["invalid-tool-name"]  # This should be "tools" not "tools_typo"
---

# Invalid Frontmatter Test

This workflow intentionally contains invalid frontmatter to test validation error handling.

## Validation Errors

This workflow should trigger the following validation errors:

1. **Invalid Top-Level Field**: `invalid_field` is not a recognized frontmatter property
2. **Invalid Config Field**: `invalid_config_field` in the config section is invalid
3. **Misspelled Tools Property**: `tools_typo` should be `tools`
4. **Tool Validation**: The tool `invalid-tool-name` does not exist

## Expected Behavior

When processed by the CLI, this workflow should:
- ❌ Fail validation during parsing
- ❌ Return appropriate exit code (WorkflowValidationError = 3)
- ❌ Provide clear error messages about validation issues
- ❌ Not proceed to execution phase

## Test Content

Even though this workflow has invalid frontmatter, the content should still be parseable.
This tests that the parser can handle malformed frontmatter gracefully and provide useful error messages.

The workflow execution should never reach this content processing stage due to frontmatter validation failures.