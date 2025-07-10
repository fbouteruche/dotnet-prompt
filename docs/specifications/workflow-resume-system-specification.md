# Workflow Resume System Specification

## Overview

This specification defines a comprehensive workflow resume system that enables intelligent AI workflow resumption using file-based storage and Semantic Kernel's enterprise features. The system captures essential state information **solely to support the resume command** without any progress estimation.

## Status
🚧 **IN DEVELOPMENT** - Unified resume-only system (no progress estimation)

## Design Philosophy: Resume-Only State Tracking

### Core Principles
- **Resume-Focused**: State tracking exists solely to support the `resume` command
- **No Progress Estimation**: Zero attempt to calculate or estimate workflow advancement
- **Minimal State Capture**: Only essential information needed for intelligent resumption
- **SK Telemetry Consumption**: Leverage existing Semantic Kernel telemetry rather than producing additional data
- **File-Based Storage**: Simple JSON files for persistent resume state storage
- **SK ChatHistory Integration**: Native conversation state management with automatic serialization

### What This System Does
- ✅ **Captures conversation context** for intelligent AI resumption
- ✅ **Tracks completed tool executions** to avoid duplication
- ✅ **Preserves workflow state** for seamless continuation
- ✅ **Validates resume compatibility** when workflow content changes
- ✅ **Provides context-rich resume messages** to help AI understand previous work
- ✅ **Manages file-based persistence** with atomic writes and corruption detection

### What This System Does NOT Do
- ❌ **Progress estimation or calculation**
- ❌ **Workflow advancement percentages**
- ❌ **Step counting or completion metrics**
- ❌ **Additional telemetry production**
- ❌ **Complex state analysis or insights**

## Problem Statement

The current state tracking system in `SemanticKernelOrchestrator` has fundamental limitations for AI workflow resumption:

### Current Issues
1. **Static Step Counting**: `CurrentStep` assumes discrete, predictable steps but AI workflows are fluid
2. **Missing AI Context**: No capture of AI reasoning, decision-making, or conversation context needed for resume
3. **Function-Level Only**: Only tracks tool executions, missing the broader AI conversation state
4. **Poor Resume Experience**: "Resuming from step 0" provides no meaningful context to the AI for continuation
5. **No Workflow State**: Can't reconstruct the AI's understanding of what work has been completed

### AI Workflow Resume Requirements
- AI models need **conversation context** to understand previous work and decisions
- **Tool execution history** is required to avoid repeating completed work
- **Accumulated context** must be preserved to maintain AI's understanding
- **Resume messages** must provide meaningful continuation points for the AI
- **Compatibility validation** prevents broken resume attempts when workflow content changes

## Resume State Model

### Core Resume State
```csharp
public class AIWorkflowResumeState
{
    // Core workflow identification
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowFilePath { get; set; } = string.Empty;
    public string OriginalWorkflowHash { get; set; } = string.Empty;
    public string OriginalWorkflowContent { get; set; } = string.Empty;
    
    // Timing information
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    
    // Current workflow state (for context only, not progress)
    public string CurrentPhase { get; set; } = "understanding";
    public string CurrentStrategy { get; set; } = string.Empty;
    
    // Completed work tracking
    public List<CompletedTool> CompletedTools { get; set; } = new();
    
    // Conversation context
    public List<ChatMessage> ChatHistory { get; set; } = new();
    
    // Accumulated context
    public ContextEvolution ContextEvolution { get; set; } = new();
    
    // Available tools for validation
    public List<string> AvailableTools { get; set; } = new();
}

public class CompletedTool
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Result { get; set; }
    public DateTimeOffset ExecutedAt { get; set; }
    public bool Success { get; set; }
    public string? AIReasoning { get; set; }  // Why the AI chose this tool
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;  // user, assistant, tool
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? ToolCallId { get; set; }  // For function calls and responses
    public List<FunctionCall>? FunctionCalls { get; set; }  // For assistant messages with tool calls
}

public class FunctionCall
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string CallId { get; set; } = string.Empty;
}

public class ContextEvolution
{
    public Dictionary<string, object> CurrentContext { get; set; } = new();
    public List<string> KeyInsights { get; set; } = new();
    public List<ContextChange> Changes { get; set; } = new();
}

public class ContextChange
{
    public DateTimeOffset Timestamp { get; set; }
    public string Key { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public string Source { get; set; } = string.Empty;  // Which tool or process created this change
    public string Reasoning { get; set; } = string.Empty;
}
```

### Working Resume State (Runtime)
```csharp
public class WorkflowExecutionContext
{
    public int CurrentStep { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<ExecutionHistoryEntry> ExecutionHistory { get; set; } = new();
    public DateTimeOffset StartTime { get; set; }
}

public class ExecutionHistoryEntry
{
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;  // "function", "user_input", etc.
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## File-Based Storage Architecture

### Resume State Persistence
```csharp
public class FileResumeStateManager : IResumeStateManager
{
    private readonly ILogger<FileResumeStateManager> _logger;
    private readonly ResumeConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public async Task SaveResumeStateAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        // Create resume directory if it doesn't exist
        var resumeDir = GetResumeDirectory();
        Directory.CreateDirectory(resumeDir);
        
        // Create resume file path
        var resumeFile = Path.Combine(resumeDir, $"{workflowId}.json");
        
        var resumeData = new WorkflowResumeFile
        {
            WorkflowMetadata = new WorkflowMetadata
            {
                Id = workflowId,
                FilePath = context.GetVariable<string>("workflow_file") ?? string.Empty,
                WorkflowHash = context.GetVariable<string>("workflow_hash") ?? string.Empty,
                OriginalContent = context.GetVariable<string>("original_content") ?? string.Empty,
                StartedAt = context.StartTime,
                LastActivity = DateTimeOffset.UtcNow,
                CurrentPhase = DeterminePhaseFromContext(context, chatHistory),
                CurrentStrategy = ExtractStrategyFromChatHistory(chatHistory)
            },
            CompletedTools = ExtractCompletedToolsFromHistory(context.ExecutionHistory).ToArray(),
            ChatHistory = chatHistory.Select(msg => new ChatMessage
            {
                Role = msg.Role.ToString(),
                Content = msg.Content,
                Timestamp = DateTimeOffset.UtcNow,
                FunctionCalls = ExtractFunctionCalls(msg)
            }).ToArray(),
            ContextEvolution = BuildContextEvolution(context, chatHistory),
            WorkflowVariables = context.Variables
        };
        
        // Optimize for storage before saving
        var optimizer = new AIWorkflowResumeOptimization();
        var optimizedState = ConvertToResumeState(resumeData);
        optimizedState = optimizer.OptimizeForStorage(optimizedState);
        resumeData = ConvertToResumeFile(optimizedState);
        
        var json = JsonSerializer.Serialize(resumeData, _jsonOptions);
        
        // Atomic write with backup
        await WriteResumeFileAtomically(resumeFile, json);
        
        _logger.LogInformation("Resume state saved to {ResumeFile} for workflow {WorkflowId}", 
            resumeFile, workflowId);
    }
    
    public async Task<(WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadResumeStateAsync(string workflowId)
    {
        var resumeFile = Path.Combine(GetResumeDirectory(), $"{workflowId}.json");
        
        if (!File.Exists(resumeFile))
        {
            _logger.LogDebug("No resume file found for workflow {WorkflowId}", workflowId);
            return null;
        }
        
        var json = await File.ReadAllTextAsync(resumeFile);
        var resumeData = JsonSerializer.Deserialize<WorkflowResumeFile>(json, _jsonOptions);
        
        if (resumeData == null)
        {
            _logger.LogWarning("Failed to deserialize resume file for workflow {WorkflowId}", workflowId);
            return null;
        }
        
        // Restore ChatHistory
        var chatHistory = new ChatHistory();
        foreach (var msg in resumeData.ChatHistory)
        {
            var chatMessage = new ChatMessageContent(
                AuthorRole.Parse(msg.Role), 
                msg.Content);
                
            // Add function call metadata if present
            if (msg.FunctionCalls?.Any() == true)
            {
                foreach (var funcCall in msg.FunctionCalls)
                {
                    chatMessage.Metadata["function_call"] = funcCall;
                }
            }
            
            chatHistory.Add(chatMessage);
        }
        
        // Restore ExecutionContext
        var context = new WorkflowExecutionContext
        {
            CurrentStep = resumeData.ContextEvolution.Changes.Count, // Approximate step from context changes
            Variables = resumeData.WorkflowVariables.ToDictionary(kv => kv.Key, kv => kv.Value),
            ExecutionHistory = ReconstructExecutionHistory(resumeData.CompletedTools),
            StartTime = resumeData.WorkflowMetadata.StartedAt
        };
        
        return (context, chatHistory);
    }
    
    private async Task WriteResumeFileAtomically(string resumeFile, string json)
    {
        var tempFile = $"{resumeFile}.tmp";
        var backupFile = $"{resumeFile}.backup";
        
        try
        {
            // Create backup if original exists
            if (File.Exists(resumeFile))
            {
                File.Copy(resumeFile, backupFile, overwrite: true);
            }
            
            // Write to temp file first
            await File.WriteAllTextAsync(tempFile, json);
            
            // Atomic move
            File.Move(tempFile, resumeFile, overwrite: true);
            
            // Clean up backup on success
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }
        }
        catch (Exception ex)
        {
            // Restore backup on failure
            if (File.Exists(backupFile))
            {
                File.Move(backupFile, resumeFile, overwrite: true);
            }
            
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            
            _logger.LogError(ex, "Failed to write resume file atomically: {ResumeFile}", resumeFile);
            throw;
        }
    }
    
    private string GetResumeDirectory()
    {
        return _config.StorageLocation ?? "./.dotnet-prompt/resume";
    }
}
```

## Semantic Kernel Integration

### State Capture from SK Telemetry
```csharp
/// <summary>
/// Captures resume state from Semantic Kernel's existing telemetry.
/// Does not produce additional telemetry - only consumes what SK already provides.
/// </summary>
public sealed class SKTelemetryResumeStateCapture : IFunctionInvocationFilter
{
    private readonly IResumeStateManager _resumeManager;
    private readonly ILogger<SKTelemetryResumeStateCapture> _logger;
    
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, 
        Func<FunctionInvocationContext, Task> next)
    {
        var workflowId = context.Arguments.ContainsName("workflow_id") 
            ? context.Arguments["workflow_id"]?.ToString() 
            : "unknown";
            
        if (string.IsNullOrEmpty(workflowId)) 
        {
            await next(context);
            return;
        }
        
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            await next(context);
            
            // Capture successful tool execution for resume state
            if (context.Result?.Value != null)
            {
                var completedTool = new CompletedTool
                {
                    FunctionName = context.Function.Name,
                    Parameters = ExtractParameters(context.Arguments),
                    Result = context.Result.ToString(),
                    ExecutedAt = startTime,
                    Success = true,
                    AIReasoning = ExtractReasoningFromContext(context)
                };
                
                await _resumeManager.TrackCompletedToolAsync(workflowId, completedTool);
            }
        }
        catch (Exception ex)
        {
            // Capture failed tool execution for resume context
            var failedTool = new CompletedTool
            {
                FunctionName = context.Function.Name,
                Parameters = ExtractParameters(context.Arguments),
                Result = $"ERROR: {ex.Message}",
                ExecutedAt = startTime,
                Success = false,
                AIReasoning = "Tool execution failed"
            };
            
            await _resumeManager.TrackCompletedToolAsync(workflowId, failedTool);
            throw;
        }
    }
    
    private Dictionary<string, object> ExtractParameters(KernelArguments arguments)
    {
        return arguments.ToDictionary(arg => arg.Key, arg => arg.Value);
    }
    
    private string ExtractReasoningFromContext(FunctionInvocationContext context)
    {
        // Simple extraction of AI reasoning from context or metadata
        return context.Arguments.ContainsName("reasoning") 
            ? context.Arguments["reasoning"]?.ToString() ?? "No reasoning provided"
            : "AI tool selection";
    }
}
```

## Resume Strategy for AI Workflows

### Context-Rich Resume Messages
```csharp
private ChatMessageContent CreateAIResumeMessage(AIWorkflowResumeState resumeState)
{
    var completedTools = resumeState.CompletedTools.Where(t => t.Success).Select(t => t.FunctionName);
    var keyInsights = resumeState.ContextEvolution.KeyInsights.TakeLast(3);
    
    var resumeContext = $"""
        WORKFLOW RESUME CONTEXT - CONTINUE FROM WHERE YOU LEFT OFF
        
        PREVIOUS SESSION SUMMARY:
        • Workflow: {resumeState.WorkflowId}
        • Phase when interrupted: {resumeState.CurrentPhase}
        • Last strategy: {resumeState.CurrentStrategy}
        • Session duration: {resumeState.LastActivity - resumeState.StartTime:mm\\:ss}
        
        COMPLETED WORK (DO NOT REPEAT):
        • Tools successfully executed: {string.Join(", ", completedTools)}
        • Key discoveries made: {string.Join("; ", keyInsights)}
        • Context variables collected: {resumeState.ContextEvolution.CurrentContext.Count} items
        
        CURRENT STATE:
        {string.Join("\n", resumeState.ContextEvolution.CurrentContext.Select(kv => $"• {kv.Key}: {kv.Value}"))}
        
        RESUME INSTRUCTION:
        You are resuming this workflow exactly where you left off. Review the context above to understand:
        1. What work you've already completed successfully (DO NOT REPEAT)
        2. What insights and context you've gathered so far  
        3. Where you were in the workflow when it was interrupted
        
        Continue your work naturally as if this is one continuous session.
        Start from where you left off and build upon the work already completed.
        """;

    return new ChatMessageContent(AuthorRole.System, resumeContext);
}
```

### Resume Compatibility Validation
```csharp
public class AIWorkflowResumeCompatibility
{
    public bool CanResume { get; set; }
    public bool RequiresAdaptation { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Adaptations { get; set; } = new();
    public float CompatibilityScore { get; set; }  // 0-1, higher is more compatible
    public Dictionary<string, string> MigrationStrategies { get; set; } = new();
}

public async Task<AIWorkflowResumeCompatibility> ValidateResumeCompatibilityAsync(
    string workflowId, 
    string currentWorkflowContent, 
    CancellationToken cancellationToken = default)
{
    var resumeState = await LoadResumeStateAsync(workflowId, cancellationToken);
    if (resumeState == null) 
        return new AIWorkflowResumeCompatibility { CanResume = false, CompatibilityScore = 0.0f };
    
    var compatibility = new AIWorkflowResumeCompatibility
    {
        CanResume = true,
        CompatibilityScore = 1.0f
    };
    
    // 1. Content Hash Comparison
    var currentHash = ComputeContentHash(currentWorkflowContent);
    var originalHash = resumeState.OriginalWorkflowHash;
    
    if (currentHash != originalHash)
    {
        // 2. Simple content similarity check
        var similarity = CalculateContentSimilarity(resumeState.OriginalWorkflowContent, currentWorkflowContent);
        compatibility.CompatibilityScore *= similarity;
        
        if (similarity < 0.8f)
        {
            compatibility.Warnings.Add("Workflow content has changed significantly since last execution");
        }
        
        if (similarity < 0.5f)
        {
            compatibility.RequiresAdaptation = true;
            compatibility.Adaptations.Add("Consider resetting workflow state due to major changes");
        }
    }
    
    // 3. Tool Availability Validation
    var unavailableTools = resumeState.CompletedTools
        .Select(t => t.FunctionName)
        .Except(resumeState.AvailableTools)
        .ToList();
        
    if (unavailableTools.Any())
    {
        compatibility.CompatibilityScore *= 0.7f;
        compatibility.Warnings.Add($"Some previously used tools are no longer available: {string.Join(", ", unavailableTools)}");
    }
    
    // Final resume decision
    compatibility.CanResume = compatibility.CompatibilityScore >= 0.6f;
    
    if (!compatibility.CanResume)
    {
        compatibility.MigrationStrategies["reset_workflow"] = "Start workflow from the beginning";
        compatibility.MigrationStrategies["partial_context"] = "Preserve context but restart execution";
    }
    
    return compatibility;
}

private float CalculateContentSimilarity(string original, string current)
{
    // Simple similarity based on character difference ratio
    var distance = ComputeLevenshteinDistance(original, current);
    var maxLength = Math.Max(original.Length, current.Length);
    
    if (maxLength == 0) return 1.0f;
    
    var similarity = 1.0f - ((float)distance / maxLength);
    return Math.Max(0.0f, similarity);
}
```

## Resume File Format

### Complete Resume File Structure (`{workflow-id}.json`)
```json
{
  "workflow_metadata": {
    "id": "workflow_project-analysis_20250710_143022_1234",
    "file_path": "./project-analysis.prompt.md", 
    "workflow_hash": "abc123def456...",
    "original_content": "---\nmodel: gpt-4o\ntools: [\"project-analysis\"]\n---\nAnalyze this project...",
    "started_at": "2025-07-10T14:30:22Z",
    "last_activity": "2025-07-10T14:35:45Z",
    "current_phase": "investigating",
    "current_strategy": "Comprehensive project analysis with focus on modernization"
  },
  "completed_tools": [
    {
      "function_name": "ProjectAnalysis.analyze_project",
      "parameters": {
        "project_path": "./MyApp.csproj",
        "include_dependencies": true
      },
      "result": "{\"project_type\": \"console\", \"target_framework\": \"net8.0\", \"dependencies\": 23}",
      "executed_at": "2025-07-10T14:32:15Z",
      "success": true,
      "ai_reasoning": "Starting with project structure analysis to understand the codebase"
    },
    {
      "function_name": "FileSystem.read_file",
      "parameters": {
        "file_path": "./MyApp.csproj"
      },
      "result": "<Project Sdk=\"Microsoft.NET.Sdk\">...",
      "executed_at": "2025-07-10T14:33:30Z",
      "success": true,
      "ai_reasoning": "Reading project file to understand configuration and dependencies"
    }
  ],
  "chat_history": [
    {
      "role": "user",
      "content": "Analyze this .NET project for modernization opportunities",
      "timestamp": "2025-07-10T14:30:22Z"
    },
    {
      "role": "assistant", 
      "content": "I'll analyze your .NET project for modernization opportunities. Let me start by examining the project file.",
      "timestamp": "2025-07-10T14:30:25Z"
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
      "timestamp": "2025-07-10T14:30:32Z"
    },
    {
      "role": "tool",
      "content": "{\"project_type\": \"console\", \"target_framework\": \"net8.0\", \"dependencies\": [...]}",
      "tool_call_id": "call_abc123",
      "timestamp": "2025-07-10T14:32:15Z"
    },
    {
      "role": "assistant",
      "content": "I can see this is a .NET 8 console application with 23 dependencies. The project already uses Clean Architecture patterns, which is excellent. Let me analyze the specific dependencies to identify modernization opportunities...",
      "timestamp": "2025-07-10T14:32:20Z"
    }
  ],
  "context_evolution": {
    "current_context": {
      "project_path": "./MyApp.csproj",
      "project_type": "console",
      "target_framework": "net8.0",
      "dependency_count": 23,
      "uses_clean_architecture": true,
      "analysis_results": {
        "project_type": "console",
        "target_framework": "net8.0",
        "dependencies": [...]
      }
    },
    "key_insights": [
      "Project uses Clean Architecture pattern",
      "Heavy dependency on Microsoft.Extensions libraries",
      "Potential for modernization to .NET 8 features"
    ],
    "changes": [
      {
        "timestamp": "2025-07-10T14:32:15Z",
        "key": "project_type",
        "old_value": null,
        "new_value": "console",
        "source": "ProjectAnalysis.analyze_project",
        "reasoning": "Discovered from project file analysis"
      },
      {
        "timestamp": "2025-07-10T14:32:15Z",
        "key": "target_framework",
        "old_value": null,
        "new_value": "net8.0",
        "source": "ProjectAnalysis.analyze_project",
        "reasoning": "Extracted from project configuration"
      }
    ]
  },
  "workflow_variables": {
    "workflow_file": "./project-analysis.prompt.md",
    "workflow_hash": "abc123def456...",
    "original_content": "---\nmodel: gpt-4o\ntools: [\"project-analysis\"]\n---\nAnalyze this project...",
    "available_tools": ["ProjectAnalysis.analyze_project", "FileSystem.read_file", "PackageAnalysis.analyze_dependencies"],
    "max_analysis_depth": 3,
    "focus_areas": ["dependencies", "architecture", "performance"]
  }
}
```

### Directory Structure
```
.dotnet-prompt/
├── resume/
│   ├── workflow_project-analysis_20250710_143022_1234.json
│   ├── workflow_documentation_20250710_150000_5678.json
│   └── cleanup_metadata.json
├── cache/
│   └── [tool result caching - separate from resume]
└── config.yaml
```

### File Naming Convention
- **Pattern**: `{workflow-id}.json`
- **Workflow ID**: `workflow_{workflow-name}_{timestamp}_{context-hash}`
- **Examples**:
  - `workflow_project-analysis_20250710_143022_1234.json`
  - `workflow_complex-build_20250710_160000_abcd.json`

## State Serialization

### What Gets Saved
- **Complete conversation history**: All user, assistant, and tool messages with function calls
- **Tool execution results**: Function call results and intermediate outputs with AI reasoning
- **Workflow configuration**: Model settings, provider, and execution parameters
- **Execution context**: Variables and context evolution tracking
- **Workflow metadata**: File path, hash, timestamps, and current state
- **Context evolution**: Key insights and context changes over time

### Checkpoint Strategy
- **Automatic checkpoints**: Created after each successful tool execution
- **Manual checkpoints**: Can be triggered via CLI commands
- **Resume frequency**: Configurable via `checkpoint_frequency` setting
- **Storage location**: Configurable via `storage_location` setting (default: `./.dotnet-prompt/resume/`)
- **File management**: Automatic cleanup of old resume files based on retention policy

### File Management
- **Atomic writes**: Resume files written atomically to prevent corruption
- **Backup creation**: Previous resume file backed up before updates
- **Cleanup policies**: Configurable retention (default: 7 days for completed workflows)
- **Compression**: Optional gzip compression for large resume files
- **Validation**: JSON schema validation on load to detect corruption

## Performance Considerations

### Minimal State Storage
```csharp
public class AIWorkflowResumeOptimization
{
    // Keep state lean - only essential information for resume
    private readonly int MaxCompletedTools = 50;       // Recent successful tool executions
    private readonly int MaxChatHistory = 20;          // Recent conversation context  
    private readonly int MaxContextVariables = 30;     // Essential context variables
    private readonly int MaxKeyInsights = 10;          // Important discoveries
    
    public AIWorkflowResumeState OptimizeForStorage(AIWorkflowResumeState resumeState)
    {
        // Keep only successful tool executions for resume context
        if (resumeState.CompletedTools.Count > MaxCompletedTools)
        {
            resumeState.CompletedTools = resumeState.CompletedTools
                .Where(t => t.Success)
                .TakeLast(MaxCompletedTools)
                .ToList();
        }
        
        // Limit context variables to most essential
        if (resumeState.ContextEvolution.CurrentContext.Count > MaxContextVariables)
        {
            var essential = resumeState.ContextEvolution.CurrentContext
                .OrderByDescending(kv => EstimateResumeImportance(kv.Key, kv.Value))
                .Take(MaxContextVariables)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
                
            resumeState.ContextEvolution.CurrentContext = essential;
        }
        
        // Keep recent chat history for context
        if (resumeState.ChatHistory.Count > MaxChatHistory)
        {
            resumeState.ChatHistory = resumeState.ChatHistory
                .TakeLast(MaxChatHistory)
                .ToList();
        }
        
        // Limit key insights to most important discoveries
        if (resumeState.ContextEvolution.KeyInsights.Count > MaxKeyInsights)
        {
            resumeState.ContextEvolution.KeyInsights = resumeState.ContextEvolution.KeyInsights
                .TakeLast(MaxKeyInsights)
                .ToList();
        }
        
        return resumeState;
    }
    
    private float EstimateResumeImportance(string key, object value)
    {
        // Heuristic: important for workflow resumption
        float score = 1.0f;
        
        // Keys critical for resume context
        if (CriticalResumeKeys.Contains(key.ToLower()))
            score *= 3.0f;
            
        // Recent context changes are more important
        if (value?.ToString()?.Contains("discovered") == true || 
            value?.ToString()?.Contains("found") == true)
            score *= 2.0f;
            
        return score;
    }
    
    private readonly HashSet<string> CriticalResumeKeys = new()
    {
        "project_path", "main_goal", "current_phase", "last_strategy",
        "key_findings", "target_framework", "critical_issue", "file_path",
        "current_analysis", "next_steps", "workflow_intent"
    };
}
```

### Efficient File I/O
```csharp
public class ResumeFileOptimization
{
    private readonly ResumeConfig _config;
    
    public async Task<string> OptimizeJsonForStorage(string json)
    {
        if (json.Length > _config.MaxFileSizeBytes && _config.EnableCompression)
        {
            return await CompressJsonAsync(json);
        }
        return json;
    }
    
    public async Task<string> LoadOptimizedJson(string filePath)
    {
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        
        // Check if file is compressed (starts with gzip magic number)
        if (fileBytes.Length >= 2 && fileBytes[0] == 0x1f && fileBytes[1] == 0x8b)
        {
            return await DecompressJsonAsync(fileBytes);
        }
        
        return Encoding.UTF8.GetString(fileBytes);
    }
    
    private async Task<string> CompressJsonAsync(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await gzip.WriteAsync(bytes);
        await gzip.FlushAsync();
        return Convert.ToBase64String(output.ToArray());
    }
    
    private async Task<string> DecompressJsonAsync(byte[] compressedBytes)
    {
        using var input = new MemoryStream(compressedBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
```

## Service Registration

### Dependency Injection Setup
```csharp
public static class ResumeServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowResumeSystem(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Core resume state services
        services.AddScoped<IResumeStateManager, FileResumeStateManager>();
        services.AddScoped<AIWorkflowResumeOptimization>();
        services.AddScoped<SKTelemetryResumeStateCapture>();
        services.AddScoped<ResumeFileOptimization>();
        
        // Configuration
        services.Configure<ResumeConfig>(configuration.GetSection("Resume"));
        
        // Register SK filter for state capture
        services.AddSingleton<IFunctionInvocationFilter, SKTelemetryResumeStateCapture>();
        
        return services;
    }
}

public class ResumeConfig
{
    public string StorageLocation { get; set; } = "./.dotnet-prompt/resume";
    public int RetentionDays { get; set; } = 7;
    public bool EnableCompression { get; set; } = false;
    public int MaxFileSizeBytes { get; set; } = 1024 * 1024; // 1MB
    public int CheckpointFrequency { get; set; } = 1; // After each tool execution
    public bool EnableAtomicWrites { get; set; } = true;
    public bool EnableBackup { get; set; } = true;
}
```

## CLI Integration

### Resume Command Implementation
```csharp
[Command("resume")]
public class ResumeCommand : BaseCommand
{
    [Argument(0, Description = "Workflow file to resume (.prompt.md)")]
    public string? WorkflowFile { get; set; }
    
    [Option("--list", Description = "List available workflows to resume")]
    public bool ListWorkflows { get; set; }
    
    [Option("--force", Description = "Force resume even with compatibility warnings")]
    public bool Force { get; set; }
    
    [Option("--clean", Description = "Clean up old resume files")]
    public bool Clean { get; set; }
    
    public async Task<int> ExecuteAsync(IResumeStateManager resumeManager, IWorkflowOrchestrator orchestrator)
    {
        if (ListWorkflows)
        {
            await ListAvailableWorkflows(resumeManager);
            return ExitCodes.Success;
        }
        
        if (Clean)
        {
            await CleanOldResumeFiles(resumeManager);
            return ExitCodes.Success;
        }
        
        if (string.IsNullOrEmpty(WorkflowFile))
        {
            Console.Error.WriteLine("Error: Workflow file required for resume");
            return ExitCodes.GeneralError;
        }
        
        var workflowId = GenerateWorkflowId(WorkflowFile);
        var resumeState = await resumeManager.LoadResumeStateAsync(workflowId);
        
        if (resumeState == null)
        {
            Console.Error.WriteLine($"Error: No resume state found for workflow {WorkflowFile}");
            return ExitCodes.GeneralError;
        }
        
        // Validate compatibility
        var currentContent = await File.ReadAllTextAsync(WorkflowFile);
        var compatibility = await resumeManager.ValidateResumeCompatibilityAsync(workflowId, currentContent);
        
        if (!compatibility.CanResume && !Force)
        {
            Console.Error.WriteLine("Error: Workflow cannot be resumed due to compatibility issues:");
            foreach (var warning in compatibility.Warnings)
            {
                Console.Error.WriteLine($"  • {warning}");
            }
            Console.Error.WriteLine("Use --force to attempt resume anyway");
            return ExitCodes.GeneralError;
        }
        
        // Display resume information
        Console.WriteLine($"Resuming workflow: {WorkflowFile}");
        Console.WriteLine($"Previous session duration: {resumeState.Item1?.LastActivity - resumeState.Item1?.StartTime:mm\\:ss}");
        Console.WriteLine($"Tools completed: {resumeState.Item1?.CompletedTools.Count(t => t.Success)}");
        Console.WriteLine($"Conversation messages: {resumeState.Item2?.Count}");
        
        if (compatibility.Warnings.Any())
        {
            Console.WriteLine("Warnings:");
            foreach (var warning in compatibility.Warnings)
            {
                Console.WriteLine($"  ⚠ {warning}");
            }
        }
        
        var result = await orchestrator.ResumeWorkflowAsync(WorkflowFile, resumeState.Item1, resumeState.Item2);
        
        if (result.Success)
        {
            if (!string.IsNullOrEmpty(result.Output))
            {
                Console.WriteLine(result.Output);
            }
            return ExitCodes.Success;
        }
        else
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            return ExitCodes.GeneralError;
        }
    }
    
    private async Task ListAvailableWorkflows(IResumeStateManager resumeManager)
    {
        var workflows = await resumeManager.ListAvailableWorkflowsAsync();
        
        if (!workflows.Any())
        {
            Console.WriteLine("No workflows available for resume.");
            return;
        }
        
        Console.WriteLine("Available workflows for resume:");
        foreach (var workflow in workflows.OrderByDescending(w => w.LastActivity))
        {
            var age = DateTimeOffset.UtcNow - workflow.LastActivity;
            Console.WriteLine($"  • {workflow.WorkflowFilePath} (last activity: {age.TotalHours:F1}h ago)");
            Console.WriteLine($"    Phase: {workflow.CurrentPhase} | Tools: {workflow.CompletedTools.Count(t => t.Success)}");
        }
    }
    
    private async Task CleanOldResumeFiles(IResumeStateManager resumeManager)
    {
        var cleanedCount = await resumeManager.CleanupOldResumeFilesAsync();
        Console.WriteLine($"Cleaned up {cleanedCount} old resume files.");
    }
}
```

## Resume Logic Implementation

### Resume Process
1. **Load resume file**: Read and validate the JSON resume file
2. **Validate workflow compatibility**: Compare workflow hash for significant changes
3. **Restore conversation state**: Reconstruct SK ChatHistory from saved messages
4. **Restore execution context**: Rebuild variables and execution history
5. **Create resume message**: Generate context-rich resume message for AI
6. **Continue from last checkpoint**: Resume workflow execution with full context

### State Restoration
```csharp
public class WorkflowResumeOrchestrator
{
    public async Task<WorkflowResult> ResumeWorkflowAsync(
        string workflowFile, 
        WorkflowExecutionContext? context, 
        ChatHistory? chatHistory)
    {
        if (context == null || chatHistory == null)
        {
            return WorkflowResult.Failure("Invalid resume state");
        }
        
        // Create resume message for AI context
        var resumeState = ConvertToResumeState(context, chatHistory);
        var resumeMessage = CreateAIResumeMessage(resumeState);
        
        // Add resume message to chat history
        chatHistory.Insert(0, resumeMessage);
        
        // Continue workflow execution with restored state
        var orchestrator = new SemanticKernelOrchestrator(
            _kernelFactory, 
            _resumeManager, 
            _logger);
            
        return await orchestrator.ExecuteWorkflowAsync(
            workflowFile, 
            context, 
            chatHistory, 
            isResume: true);
    }
}
```

## Key Benefits

### Enterprise-Ready Features
- **Security**: Built on SK's enterprise security patterns
- **Observability**: Leverages existing SK telemetry infrastructure  
- **Performance**: Minimal state storage optimized for resume scenarios
- **Reliability**: File-based storage with corruption detection and recovery
- **Scalability**: Efficient state management for large workflows

### Developer Experience
- **Familiar Patterns**: Uses standard .NET dependency injection and configuration
- **Rich Context**: AI receives comprehensive context for intelligent resumption
- **Simple CLI**: Easy-to-use resume commands with compatibility validation
- **Debugging Support**: Clear resume state files for troubleshooting

### User Experience
- **Intelligent Resume**: AI understands previous work and continues naturally
- **Compatibility Checks**: Safe resumption with change detection
- **Rich Feedback**: Clear information about resume state and compatibility
- **Flexible Control**: Options for forced resume and state management
- **File Management**: Automatic cleanup and optimization of resume files

## Error Handling and Recovery

### Resume Failure Scenarios
- **Corrupted Resume Files**: Automatic backup restoration and validation
- **Missing Dependencies**: Clear error messages with migration strategies
- **Workflow Changes**: Compatibility scoring with adaptation options
- **Tool Unavailability**: Graceful degradation with alternative approaches

### Recovery Strategies
- **Partial Resume**: Use available context even if some tools are missing
- **Context Migration**: Preserve insights while restarting execution
- **Manual Intervention**: Clear guidance for resolving resume conflicts
- **Fallback Options**: Safe defaults when automatic resolution fails

The unified design ensures that AI workflow resume functionality meets enterprise requirements for security, observability, performance, and maintainability while providing a superior user experience focused solely on enabling intelligent workflow resumption without any progress estimation complexity.
