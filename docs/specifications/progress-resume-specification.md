# Progress and Resume System Specification (File-Based)

## Overview

This document defines the progress tracking and resume functionality using simple file-based storage with Semantic Kernel's ChatHistory for conversation state management.

## Status
✅ **COMPLETE** - File-based progress tracking and resume patterns defined

## File-Based Progress Management

### Core Components
- **SK ChatHistory**: Native conversation state management with automatic serialization
- **File System Storage**: Simple JSON files for persistent progress storage
- **Directory Organization**: Structured progress file organization in `.dotnet-prompt/progress/`
- **SK Function Context**: Automatic parameter and execution state tracking

### Progress Storage Architecture

#### File-Based Progress Persistence
```csharp
public class FileProgressManager : IProgressManager
{
    private readonly ILogger<FileProgressManager> _logger;
    private readonly ProgressConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public async Task SaveProgressAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        // Create progress directory if it doesn't exist
        var progressDir = GetProgressDirectory();
        Directory.CreateDirectory(progressDir);
        
        // Create progress file path
        var progressFile = Path.Combine(progressDir, $"{workflowId}.json");
        
        var progressData = new WorkflowProgressFile
        {
            WorkflowMetadata = new WorkflowMetadata
            {
                Id = workflowId,
                FilePath = context.GetVariable<string>("workflow_file") ?? string.Empty,
                WorkflowHash = context.GetVariable<string>("workflow_hash") ?? string.Empty,
                StartedAt = context.StartTime,
                LastCheckpoint = DateTimeOffset.UtcNow,
                Status = "in_progress"
            },
            ChatHistory = chatHistory.Select(msg => new ChatMessage
            {
                Role = msg.Role.ToString(),
                Content = msg.Content,
                Timestamp = DateTimeOffset.UtcNow
            }).ToArray(),
            ExecutionContext = new ExecutionContextData
            {
                CurrentStep = context.CurrentStep,
                Variables = context.Variables,
                ExecutionHistory = context.ExecutionHistory
            }
        };
        
        var json = JsonSerializer.Serialize(progressData, _jsonOptions);
        await File.WriteAllTextAsync(progressFile, json);
        
        _logger.LogInformation("Progress saved to {ProgressFile} for workflow {WorkflowId}", 
            progressFile, workflowId);
    }
    
    public async Task<(WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadProgressAsync(string workflowId)
    {
        var progressFile = Path.Combine(GetProgressDirectory(), $"{workflowId}.json");
        
        if (!File.Exists(progressFile))
        {
            _logger.LogDebug("No progress file found for workflow {WorkflowId}", workflowId);
            return null;
        }
        
        var json = await File.ReadAllTextAsync(progressFile);
        var progressData = JsonSerializer.Deserialize<WorkflowProgressFile>(json, _jsonOptions);
        
        if (progressData == null)
        {
            _logger.LogWarning("Failed to deserialize progress file for workflow {WorkflowId}", workflowId);
            return null;
        }
        
        // Restore ChatHistory
        var chatHistory = new ChatHistory();
        foreach (var msg in progressData.ChatHistory)
        {
            chatHistory.Add(new ChatMessageContent(
                AuthorRole.Parse(msg.Role), 
                msg.Content));
        }
        
        // Restore ExecutionContext
        var context = new WorkflowExecutionContext
        {
            CurrentStep = progressData.ExecutionContext.CurrentStep,
            Variables = progressData.ExecutionContext.Variables,
            ExecutionHistory = progressData.ExecutionContext.ExecutionHistory,
            StartTime = progressData.WorkflowMetadata.StartedAt
        };
        
        return (context, chatHistory);
    }
    
    private string GetProgressDirectory()
    {
        return _config.StorageLocation ?? "./.dotnet-prompt/progress";
    }
}
```

## Progress File Format (File-Based)

### Progress File Structure (`{workflow-id}.json`)
```json
{
  "workflow_metadata": {
    "id": "workflow_project-analysis_20250708_143022_1234",
    "file_path": "./project-analysis.prompt.md",
    "workflow_hash": "abc123def456...",
    "started_at": "2025-07-08T14:30:22Z",
    "last_checkpoint": "2025-07-08T14:35:45Z",
    "status": "in_progress"
  },
  "chat_history": [
    {
      "role": "user",
      "content": "Analyze the project structure and dependencies for ./MyApp.csproj",
      "timestamp": "2025-07-08T14:30:25Z"
    },
    {
      "role": "assistant", 
      "content": "I'll analyze your project structure and dependencies. Let me start by examining the project file.",
      "timestamp": "2025-07-08T14:30:30Z"
    },
    {
      "role": "assistant",
      "content": null,
      "function_calls": [
        {
          "function_name": "ProjectAnalysis.analyze_project",
          "parameters": {
            "project_path": "./MyApp.csproj",
            "include_dependencies": true
          },
          "call_id": "call_abc123"
        }
      ],
      "timestamp": "2025-07-08T14:30:32Z"
    },
    {
      "role": "tool",
      "content": "{\"project_type\": \"console\", \"target_framework\": \"net8.0\", \"dependencies\": [...]}",
      "tool_call_id": "call_abc123",
      "timestamp": "2025-07-08T14:32:15Z"
    }
  ],
  "execution_context": {
    "current_step": 2,
    "variables": {
      "project_path": "./MyApp.csproj",
      "analysis_results": {
        "project_type": "console",
        "target_framework": "net8.0",
        "dependencies": [...]
      }
    },
    "execution_history": [
      {
        "step_name": "ProjectAnalysis.analyze_project",
        "step_type": "function",
        "start_time": "2025-07-08T14:30:32Z",
        "end_time": "2025-07-08T14:32:15Z",
        "success": true,
        "error_message": null
      }
    ]
  }
}
```

### Directory Structure
```
.dotnet-prompt/
├── progress/
│   ├── workflow_project-analysis_20250708_143022_1234.json
│   ├── workflow_documentation_20250708_150000_5678.json
│   └── cleanup_metadata.json
├── cache/
│   └── [tool result caching - separate from progress]
└── config.yaml
```

### File Naming Convention
- **Pattern**: `{workflow-id}.json`
- **Workflow ID**: `workflow_{workflow-name}_{timestamp}_{context-hash}`
- **Examples**:
  - `workflow_project-analysis_20250708_143022_1234.json`
  - `workflow_complex-build_20250708_160000_abcd.json`
```

## State Serialization

### What Gets Saved
- **Complete conversation history**: All user, assistant, and tool messages
- **Tool execution results**: Function call results and intermediate outputs
- **Workflow configuration**: Model settings, provider, and execution parameters
- **Execution context**: Current step, variables, and execution history
- **Workflow metadata**: File path, hash, timestamps, and status

### Checkpoint Strategy
- **Automatic checkpoints**: Created after each successful tool execution
- **Manual checkpoints**: Can be triggered via CLI commands
- **Progress frequency**: Configurable via `checkpoint_frequency` setting
- **Storage location**: Configurable via `storage_location` setting (default: `./.dotnet-prompt/progress/`)
- **File management**: Automatic cleanup of old progress files based on retention policy

### File Management
- **Atomic writes**: Progress files written atomically to prevent corruption
- **Backup creation**: Previous progress file backed up before updates
- **Cleanup policies**: Configurable retention (default: 7 days for completed workflows)
- **Compression**: Optional gzip compression for large progress files
- **Validation**: JSON schema validation on load to detect corruption

## Resume Logic

### Resume Process
1. **Load progress file**: Read and validate the JSON progress file
2. **Validate workflow compatibility**: Compare workflow hash for significant changes
3. **Restore conversation state**: Reconstruct SK ChatHistory from saved messages
4. **Restore execution context**: Rebuild variables, step counter, and execution history
5. **Continue from last checkpoint**: Resume workflow execution from the last successful step

### State Restoration
- **ChatHistory reconstruction**: Convert saved messages back to SK ChatHistory format
- **Variable restoration**: Restore all workflow variables and intermediate results
- **Context validation**: Ensure restored context is consistent and complete
- **Error recovery**: Handle cases where restoration fails gracefully
- **Compatibility checks**: Validate that current workflow version is compatible with saved progress

## Clarifying Questions

### 1. Progress File Format
- What is the exact format for progress files?
- How should conversation history be serialized?
- What metadata should be included in progress files?
- How should binary data or large outputs be handled?
- Should progress files be human-readable or optimized for parsing?

### 2. State Persistence Strategy
- When should checkpoints be created automatically?
- Should users be able to create manual checkpoints?
- How often should progress be saved during long operations?
- What triggers a progress file update?
- Should there be incremental vs full state saves?

### 3. Resume Logic
- How should the tool validate that a workflow can be resumed?
- What happens if the workflow file has changed since the last run?
- How should parameter changes be handled during resume?
- Should there be conflict resolution for modified workflows?
- How should the tool handle missing dependencies during resume?

### 4. Conversation State Management
- How should the complete conversation history be maintained?
- What is the format for storing AI messages and tool calls?
- How should token usage and costs be tracked across resumes?
- Should there be conversation compression or summarization?
- How should conversation context windows be managed?

### 5. Tool State and Results
- How should tool execution results be cached and reused?
- Should completed tool calls be re-executed on resume?
- How should tool state be validated on resume?
- What happens if tool dependencies have changed?
- Should there be tool result invalidation strategies?

### 6. Error Handling and Recovery
- How should different types of failures be categorized?
- What retry logic should be implemented on resume?
- How should the tool handle partial failures?
- Should there be automatic recovery strategies?
- How should users be notified of resume conflicts?

### 7. Multi-workflow Resume
- How should sub-workflow progress be tracked?
- Should sub-workflows have their own progress files?
- How should parent-child workflow relationships be maintained?
- What happens if a sub-workflow fails during resume?
- Should there be batch resume capabilities?

### 8. Progress File Management
- **Storage location**: Default `.dotnet-prompt/progress/`, configurable via `storage_location`
- **File naming**: `{workflow-id}.json` pattern with unique workflow identifiers
- **Automatic cleanup**: Configurable retention policies for old progress files
- **File organization**: Structured directory layout for easy management
- **Backup and recovery**: Progress file backup strategies and corruption recovery
- **Compression**: Optional compression for large progress files
- **Permissions**: File system permissions and access control

### 9. User Experience
- How should users be informed about available resume options?
- Should there be progress visualization or reporting?
- How should users control checkpoint frequency?
- Should there be resume conflict resolution UI?
- How should progress file debugging work?

### 10. Security and Privacy
- What sensitive information might be in progress files?
- How should credentials be handled in progress state?
- Should progress files be encrypted?
- How should progress files be shared between team members?
- What audit logging is needed for resume operations?

### 11. Performance Considerations
- **File I/O optimization**: Efficient reading/writing of progress files
- **JSON serialization**: Optimized serialization for large conversation histories
- **Memory management**: Efficient loading and processing of progress data
- **Lazy loading**: Load progress data on-demand to reduce startup time
- **Corruption detection**: Fast validation of progress file integrity
- **Concurrent access**: File locking and concurrent access patterns
- **Large file handling**: Streaming and chunked processing for large progress files

## Next Steps

1. **Implement FileProgressManager**: Create file-based progress manager replacing the current in-memory implementation
2. **Define progress file schema**: Create JSON schema for progress file validation
3. **Implement file management**: Create utilities for cleanup, backup, and recovery
4. **Add resume functionality**: Implement workflow resume from progress files
5. **Create progress CLI commands**: Add commands for listing, cleaning, and managing progress files
6. **Add configuration options**: Implement storage location and retention policy configuration
7. **Implement error recovery**: Handle progress file corruption and incomplete states
8. **Add progress reporting**: Create utilities for progress visualization and debugging
