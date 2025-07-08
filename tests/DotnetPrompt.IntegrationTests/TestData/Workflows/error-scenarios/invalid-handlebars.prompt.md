---
name: "invalid-handlebars"
model: "gpt-4o"
tools: ["file-system"]

config:
  temperature: 0.5
  maxOutputTokens: 2000

input:
  default:
    project_name: "TestProject"
    valid_variable: "This should work"
---

# Invalid Handlebars Template Test

This workflow contains invalid Handlebars syntax to test template validation and error handling.

## Valid Template Section

First, let's test valid Handlebars syntax:
- Project Name: {{project_name}}
- Valid Variable: {{valid_variable}}

## Invalid Handlebars Syntax Tests

The following sections contain intentionally invalid Handlebars syntax:

### 1. Unclosed Block Helper
{{#if project_name}}
This if block is never closed properly - missing {{/if}}

This should cause a template parsing error.

### 2. Invalid Helper Syntax
{{#invalidhelper parameter}}
This helper doesn't exist and should cause an error.
{{/invalidhelper}}

### 3. Malformed Variable Reference
{{project_name.invalid.property.chain.that.doesnt.exist}}

### 4. Nested Block Errors
{{#if valid_variable}}
  {{#each undefined_array}}
    {{#if nested_condition}}
    This has improperly nested and unclosed blocks
  {{/each}}
{{/if}}

### 5. Invalid Expressions
{{#if (and (or) missing_function(param1, param2))}}
Invalid boolean logic and function calls
{{/if}}

## Expected Error Handling

The Handlebars template engine should:

1. **Detect Syntax Errors**: Identify unclosed blocks, invalid helpers, and malformed expressions
2. **Provide Error Location**: Indicate where in the template the error occurs
3. **Graceful Degradation**: Handle errors without crashing the entire workflow
4. **Clear Error Messages**: Provide understandable error descriptions

## Recovery Expectations

Depending on the SK Handlebars implementation:
- **Strict Mode**: Template should fail to render with clear error messages
- **Lenient Mode**: Invalid sections might be skipped with warnings
- **Partial Rendering**: Valid sections might render while invalid sections are omitted

## Valid Content After Errors

This section should render correctly if the template engine supports partial rendering:

Thank you for testing the Handlebars error handling with {{project_name}}.
The valid_variable contains: {{valid_variable}}

## Test Validation

This workflow tests:
- ✅ Template syntax validation
- ✅ Error reporting and location identification  
- ✅ Graceful handling of template errors
- ✅ Partial rendering capabilities (if supported)
- ✅ Recovery from template parsing failures