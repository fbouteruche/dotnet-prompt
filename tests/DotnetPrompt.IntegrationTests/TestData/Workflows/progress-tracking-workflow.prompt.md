---
name: "progress-tracking-workflow"
model: "gpt-4o"
tools: ["file-system", "project-analysis"]

config:
  temperature: 0.4
  maxOutputTokens: 3500

input:
  default:
    project_path: "."
    analysis_stages: 5
    checkpoint_frequency: 2
    enable_resume: true
  schema:
    project_path:
      type: string
      description: "Path to the project for analysis"
      default: "."
    analysis_stages:
      type: number
      description: "Number of analysis stages to perform"
      default: 5
    checkpoint_frequency:
      type: number
      description: "Save progress every N stages"
      default: 2
    enable_resume:
      type: boolean
      description: "Enable resume functionality"
      default: true
---

# Long-Running Workflow with Progress Tracking

This workflow simulates a long-running analysis process to test file-based progress tracking and resume functionality.

## Workflow Overview

Project: **{{project_path}}**
Total Stages: **{{analysis_stages}}**
Checkpoint Frequency: Every **{{checkpoint_frequency}}** stages
Resume Enabled: **{{enable_resume}}**

This workflow will execute {{analysis_stages}} analysis stages with progress checkpoints to test:
- File-based progress persistence
- SK ChatHistory serialization 
- Workflow interruption and resume scenarios
- Progress file organization and cleanup

## Stage 1: Project Discovery and Initialization

**Status**: Starting comprehensive project analysis...

Initializing analysis for project at {{project_path}}:

1. Scan project directory structure
2. Identify project files and solution structure
3. Initialize analysis workspace
4. Set up progress tracking

**Variables Established**:
- `project_root`: {{project_path}}
- `analysis_start_time`: {{current_timestamp}}
- `total_stages`: {{analysis_stages}}
- `current_stage`: 1

**Progress Check**: This stage establishes the foundation for analysis. Progress should be saved if checkpoint_frequency is 1.

## Stage 2: Dependency Analysis

**Status**: Analyzing project dependencies and references...

Performing detailed dependency analysis:

1. Scan NuGet package references
2. Analyze project-to-project references
3. Identify external dependencies
4. Check for potential vulnerabilities
5. Generate dependency tree

**Intermediate Results**:
- Package count: [To be determined during execution]
- Reference analysis: [To be populated]
- Vulnerability scan: [To be completed]

{{#if (eq checkpoint_frequency 2)}}
**Checkpoint**: Progress should be saved after this stage (stage 2 of {{analysis_stages}}).
{{/if}}

## Stage 3: Code Quality Assessment

**Status**: Evaluating code quality and architectural patterns...

Comprehensive code quality evaluation:

1. Analyze code organization and structure
2. Identify architectural patterns
3. Review error handling implementations
4. Assess code complexity metrics
5. Document public API surface

**Quality Metrics**:
- Code organization score: [TBD]
- Architectural compliance: [TBD]
- Error handling coverage: [TBD]
- API documentation level: [TBD]

## Stage 4: Security and Performance Review

**Status**: Conducting security and performance analysis...

Security and performance assessment:

1. Security vulnerability scanning
2. Authentication/authorization pattern review
3. Data validation and sanitization checks
4. Performance bottleneck identification
5. Resource usage analysis

**Security Findings**:
- Vulnerability count: [TBD]
- Security pattern compliance: [TBD]
- Data handling assessment: [TBD]

{{#if (eq checkpoint_frequency 2)}}
**Checkpoint**: Progress should be saved after this stage (stage 4 of {{analysis_stages}}).
{{/if}}

## Stage 5: Documentation and Recommendations

**Status**: Generating documentation and improvement recommendations...

Final analysis and documentation:

1. Compile comprehensive analysis report
2. Generate improvement recommendations
3. Create architecture documentation
4. Provide implementation guidance
5. Finalize analysis results

**Final Deliverables**:
- Analysis summary report
- Architectural documentation
- Security recommendations
- Performance optimization suggestions
- Implementation roadmap

## Progress Tracking Validation

This workflow tests the following progress tracking scenarios:

### Normal Execution Flow
- Progress saved at checkpoints (every {{checkpoint_frequency}} stages)
- SK ChatHistory preserved at each checkpoint
- Workflow variables maintained across saves
- Execution history tracked properly

### Interruption Scenarios
{{#if enable_resume}}
**Resume Capability Enabled**

If this workflow is interrupted at any stage, it should be resumable using:
```bash
dotnet prompt resume progress-tracking-workflow.prompt.md
```

The resume process should:
1. Load the last saved progress file
2. Restore SK ChatHistory with complete conversation
3. Restore all workflow variables and intermediate results
4. Continue from the last completed stage
5. Maintain execution context and state
{{else}}
**Resume Capability Disabled**

This execution will not support resume functionality. Progress tracking is for monitoring only.
{{/if}}

### Progress File Management
- Progress files created in `.dotnet-prompt/progress/` directory
- JSON serialization of workflow state
- SK ChatHistory properly serialized and restored
- Automatic cleanup of old progress files
- Cross-platform file compatibility

## Expected Progress File Structure

The progress file should contain:

```json
{
  "WorkflowMetadata": {
    "WorkflowId": "progress-tracking-workflow-[hash]",
    "WorkflowName": "progress-tracking-workflow",
    "StartedAt": "[timestamp]",
    "LastSavedAt": "[timestamp]"
  },
  "ExecutionContext": {
    "CurrentStage": "[1-5]",
    "Variables": {
      "project_root": "{{project_path}}",
      "analysis_start_time": "[timestamp]",
      "total_stages": {{analysis_stages}},
      "intermediate_results": { ... }
    },
    "ExecutionHistory": [
      "Stage 1: Project Discovery - COMPLETED",
      "Stage 2: Dependency Analysis - [STATUS]",
      ...
    ]
  },
  "ChatHistory": [
    {
      "Role": "user",
      "Content": "[user messages]"
    },
    {
      "Role": "assistant", 
      "Content": "[assistant responses]"
    }
  ]
}
```

## Completion Criteria

This workflow successfully validates progress tracking if:

1. ✅ Progress files are created at specified checkpoints
2. ✅ SK ChatHistory is correctly serialized and restored
3. ✅ Workflow variables persist across saves/loads
4. ✅ Execution can be resumed from any checkpoint
5. ✅ Progress files are valid JSON and cross-platform compatible
6. ✅ Cleanup functionality removes old progress files appropriately

**Begin comprehensive analysis and progress tracking for {{project_path}}...**