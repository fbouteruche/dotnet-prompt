# Progress and Resume System Specification (SK-Powered)

## Overview

This document defines the progress tracking and resume functionality leveraging Semantic Kernel's ChatHistory, conversation state management, and Vector Store persistence capabilities.

## Status
✅ **COMPLETE** - SK-based progress tracking and resume patterns defined

## SK-Based Progress Management

### Core Components
- **SK ChatHistory**: Native conversation state management with automatic serialization
- **SK Vector Store**: Persistent storage for conversation state and workflow metadata
- **SK Conversation State**: Built-in checkpoint and resume capabilities
- **SK Function Context**: Automatic parameter and execution state tracking

### Progress Storage Architecture

#### SK ChatHistory Persistence
```csharp
public class SkProgressManager : IProgressManager
{
    private readonly IVectorStore _vectorStore;
    private readonly IChatHistory _chatHistory;
    private readonly ILogger<SkProgressManager> _logger;
    
    public async Task SaveProgressAsync(string workflowId, WorkflowExecutionContext context)
    {
        // Use SK Vector Store for conversation persistence
        var collection = _vectorStore.GetCollection<string, WorkflowProgress>("workflow_progress");
        
        var progressRecord = new WorkflowProgress
        {
            Id = workflowId,
            ChatHistoryJson = JsonSerializer.Serialize(_chatHistory),
            ExecutionContext = context,
            LastCheckpoint = DateTimeOffset.UtcNow,
            Status = WorkflowStatus.InProgress
        };
        
        await collection.UpsertAsync(progressRecord);
        _logger.LogInformation("Progress saved for workflow {WorkflowId} at checkpoint {Timestamp}", 
            workflowId, progressRecord.LastCheckpoint);
    }
    
    public async Task<WorkflowExecutionContext?> LoadProgressAsync(string workflowId)
    {
        var collection = _vectorStore.GetCollection<string, WorkflowProgress>("workflow_progress");
        var progressRecord = await collection.GetAsync(workflowId);
        
        if (progressRecord == null) return null;
        
        // Restore SK ChatHistory from persisted state
        var chatHistory = JsonSerializer.Deserialize<ChatHistory>(progressRecord.ChatHistoryJson);
        await RestoreChatHistoryAsync(chatHistory);
        
        return progressRecord.ExecutionContext;
    }
}
```

## Progress File Format (SK-Enhanced)

### Enhanced Progress Structure (`sk-progress.json`)
```json
{
  "workflow_metadata": {
    "id": "workflow-abc123",
    "file_path": "./workflow.prompt.md",
    "started_at": "2025-06-30T14:30:00Z",
    "last_checkpoint": "2025-06-30T14:35:22Z",
    "status": "in_progress",
    "sk_kernel_version": "1.0.0"
  },
  "sk_chat_history": {
    "messages": [
      {
        "role": "user",
        "content": "[Original workflow content]",
        "timestamp": "2025-06-30T14:30:05Z"
      },
      {
        "role": "assistant", 
        "content": "I'll help you with that project analysis.",
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
        "timestamp": "2025-06-30T14:30:10Z"
      },
      {
        "role": "tool",
        "content": "{\"project_type\": \"console\", \"target_framework\": \"net8.0\"}",
        "tool_call_id": "call_abc123",
        "timestamp": "2025-06-30T14:32:15Z"
      }
    ]
  },
  "sk_execution_state": {
    "current_function_step": 2,
    "completed_functions": ["ProjectAnalysis.analyze_project"],
    "pending_functions": ["BuildTest.run_tests", "FileSystem.create_files"],
    "function_results": {
      "call_abc123": {
        "function": "ProjectAnalysis.analyze_project",
        "result": { "project_type": "console", "target_framework": "net8.0" },
        "execution_time_ms": 2150,
        "success": true
      }
    },
    "conversation_variables": {
      "project_path": "./MyApp.csproj",
      "analysis_result": { "project_type": "console" }
    }
  },
  "sk_configuration": {
    "kernel_settings": {
      "execution_settings": {
        "function_choice_behavior": "auto",
        "temperature": 0.7,
        "max_tokens": 4000
      }
    },
    "active_plugins": ["ProjectAnalysis", "BuildTest", "FileSystem"],
    "ai_service": {
      "provider": "github", 
      "model": "gpt-4o"
    }
  }
}
```

## State Serialization

### What Gets Saved
- Complete conversation history
- Tool execution results
- Workflow configuration and parameters
- Execution context and variables
- Error states and retry attempts

### Checkpoint Strategy
- When checkpoints are created
- How often state is persisted
- Storage location and naming

## Resume Logic

### Resume Process
1. Load progress file
2. Validate workflow compatibility
3. Restore conversation state
4. Continue from last checkpoint

### State Restoration
How the execution context is restored and continued.

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
- Where should progress files be stored?
- How should progress file naming work?
- Should there be automatic cleanup of old progress files?
- How should progress files be backed up or versioned?
- Should there be progress file compression?

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
- How should large progress files be handled efficiently?
- What is the strategy for progress file compression?
- How should memory usage be managed during state restoration?
- Should there be lazy loading of progress state?
- How should progress file corruption be detected and handled?

## Next Steps

1. Define the exact progress file format and schema
2. Implement state serialization and deserialization
3. Create checkpoint and resume logic
4. Design progress file management system
5. Implement error recovery strategies
6. Create progress tracking and reporting features
