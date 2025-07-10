using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Infrastructure.Resume;

/// <summary>
/// File-based resume state manager with atomic writes and backup/recovery.
/// Implements the unified workflow resume system specification.
/// </summary>
public class FileResumeStateManager : IResumeStateManager
{
    private readonly ILogger<FileResumeStateManager> _logger;
    private readonly ResumeConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AIWorkflowResumeOptimization _optimizer;

    public FileResumeStateManager(
        ILogger<FileResumeStateManager> logger,
        IOptions<ResumeConfig> config,
        AIWorkflowResumeOptimization optimizer)
    {
        _logger = logger;
        _config = config.Value;
        _optimizer = optimizer;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task SaveResumeStateAsync(string workflowId, WorkflowExecutionContext context, ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving resume state for workflow {WorkflowId}", workflowId);

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
                    StartedAt = context.StartTime,
                    LastCheckpoint = DateTimeOffset.UtcNow,
                    Status = "in_progress"
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
            var optimizedState = ConvertToResumeState(resumeData);
            optimizedState = _optimizer.OptimizeForStorage(optimizedState);
            resumeData = ConvertToResumeFile(optimizedState);

            var json = JsonSerializer.Serialize(resumeData, _jsonOptions);

            // Atomic write with backup
            await WriteResumeFileAtomically(resumeFile, json, cancellationToken);

            _logger.LogInformation("Resume state saved to {ResumeFile} for workflow {WorkflowId}", 
                resumeFile, workflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save resume state for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<(WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadResumeStateAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading resume state for workflow {WorkflowId}", workflowId);

            var resumeFile = Path.Combine(GetResumeDirectory(), $"{workflowId}.json");

            if (!File.Exists(resumeFile))
            {
                _logger.LogDebug("No resume file found for workflow {WorkflowId}", workflowId);
                return null;
            }

            var json = await File.ReadAllTextAsync(resumeFile, cancellationToken);
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
                var role = msg.Role.ToLowerInvariant() switch
                {
                    "user" => AuthorRole.User,
                    "assistant" => AuthorRole.Assistant,
                    "system" => AuthorRole.System,
                    "tool" => AuthorRole.Tool,
                    _ => AuthorRole.Assistant
                };

                var chatMessage = new ChatMessageContent(role, msg.Content);
                    
                // Note: Function call metadata would be added here in a real implementation
                // For now, we'll skip detailed function call tracking for resume
                
                chatHistory.Add(chatMessage);
            }

            // Restore ExecutionContext
            var context = new WorkflowExecutionContext
            {
                CurrentStep = resumeData.ContextEvolution.Changes.Count, // Approximate step from context changes
                Variables = resumeData.WorkflowVariables.ToDictionary(kv => kv.Key, kv => kv.Value),
                ExecutionHistory = ReconstructExecutionHistory(resumeData.CompletedTools),
                StartTime = resumeData.WorkflowMetadata.StartedAt.DateTime
            };

            _logger.LogInformation("Successfully loaded resume state for workflow {WorkflowId}", workflowId);
            return (context, chatHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resume state for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task TrackCompletedToolAsync(string workflowId, CompletedTool completedTool, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Tracking completed tool for workflow {WorkflowId}: {FunctionName}", 
                workflowId, completedTool.FunctionName);

            // Load existing resume state to update
            var existing = await LoadResumeStateAsync(workflowId, cancellationToken);
            if (existing == null)
            {
                _logger.LogWarning("Cannot track completed tool - no existing resume state found for workflow {WorkflowId}", workflowId);
                return;
            }

            var (context, chatHistory) = existing.Value;
            if (context == null || chatHistory == null)
            {
                _logger.LogWarning("Cannot track completed tool - invalid resume state for workflow {WorkflowId}", workflowId);
                return;
            }

            // Add tool completion to execution history
            context.ExecutionHistory.Add(new StepExecutionHistory
            {
                StepName = completedTool.FunctionName,
                StepType = "function",
                StartTime = completedTool.ExecutedAt.DateTime,
                EndTime = completedTool.ExecutedAt.DateTime,
                Success = completedTool.Success,
                ErrorMessage = completedTool.Success ? null : completedTool.Result
            });

            // Save updated resume state
            await SaveResumeStateAsync(workflowId, context, chatHistory, cancellationToken);

            _logger.LogInformation("Completed tool tracked for workflow {WorkflowId}: {FunctionName}, success: {Success}",
                workflowId, completedTool.FunctionName, completedTool.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track completed tool for workflow {WorkflowId}: {FunctionName}",
                workflowId, completedTool.FunctionName);
            throw;
        }
    }

    public async Task<IEnumerable<AIWorkflowResumeState>> ListAvailableWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing available resumable workflows in {ResumeDirectory}", GetResumeDirectory());

            var resumeDir = GetResumeDirectory();
            if (!Directory.Exists(resumeDir))
            {
                _logger.LogDebug("Resume directory does not exist, no workflows available");
                return Array.Empty<AIWorkflowResumeState>();
            }

            var resumeFiles = Directory.GetFiles(resumeDir, "*.json");
            var results = new List<AIWorkflowResumeState>();

            foreach (var file in resumeFiles)
            {
                try
                {
                    var workflowId = Path.GetFileNameWithoutExtension(file);
                    var resumeState = await LoadResumeStateDataAsync(workflowId, cancellationToken);
                    
                    if (resumeState != null)
                    {
                        results.Add(resumeState);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping invalid resume file {File}", file);
                }
            }

            _logger.LogInformation("Found {Count} available resumable workflows", results.Count);
            return results.OrderByDescending(w => w.LastActivity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list available workflows");
            throw;
        }
    }

    public async Task<AIWorkflowResumeCompatibility> ValidateResumeCompatibilityAsync(string workflowId, string currentWorkflowContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating resume compatibility for workflow {WorkflowId}", workflowId);

            var resumeState = await LoadResumeStateDataAsync(workflowId, cancellationToken);
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

            _logger.LogInformation("Resume compatibility for {WorkflowId}: {CanResume} (score: {Score:F2})",
                workflowId, compatibility.CanResume, compatibility.CompatibilityScore);

            return compatibility;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate resume compatibility for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<int> CleanupOldResumeFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Cleaning up resume files older than {RetentionDays} days", _config.RetentionDays);

            var resumeDir = GetResumeDirectory();
            if (!Directory.Exists(resumeDir))
            {
                _logger.LogDebug("Resume directory does not exist, no cleanup needed");
                return 0;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-_config.RetentionDays);
            var resumeFiles = Directory.GetFiles(resumeDir, "*.json");
            var deletedCount = 0;

            foreach (var file in resumeFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old resume file {File}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete resume file {File}", file);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old resume files", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old resume files");
            throw;
        }
    }

    public async Task<AIWorkflowResumeState?> LoadResumeStateDataAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var resumeFile = Path.Combine(GetResumeDirectory(), $"{workflowId}.json");

            if (!File.Exists(resumeFile))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(resumeFile, cancellationToken);
            var resumeData = JsonSerializer.Deserialize<WorkflowResumeFile>(json, _jsonOptions);

            if (resumeData == null)
            {
                return null;
            }

            return ConvertToResumeState(resumeData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resume state data for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task UpdateContextEvolutionAsync(string workflowId, ContextChange contextChange, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await LoadResumeStateAsync(workflowId, cancellationToken);
            if (existing?.Context != null && existing?.ChatHistory != null)
            {
                // This would be implemented by updating the resume file with new context change
                // For now, just save the current state which will capture the change
                await SaveResumeStateAsync(workflowId, existing.Value.Context, existing.Value.ChatHistory, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update context evolution for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task AddKeyInsightAsync(string workflowId, string insight, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await LoadResumeStateAsync(workflowId, cancellationToken);
            if (existing?.Context != null && existing?.ChatHistory != null)
            {
                // Add insight to context variables
                existing.Value.Context.SetVariable($"insight_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}", insight);
                await SaveResumeStateAsync(workflowId, existing.Value.Context, existing.Value.ChatHistory, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add key insight for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    private async Task WriteResumeFileAtomically(string resumeFile, string json, CancellationToken cancellationToken)
    {
        if (!_config.EnableAtomicWrites)
        {
            await File.WriteAllTextAsync(resumeFile, json, cancellationToken);
            return;
        }

        var tempFile = $"{resumeFile}.tmp";
        var backupFile = $"{resumeFile}.backup";

        try
        {
            // Create backup if original exists
            if (File.Exists(resumeFile) && _config.EnableBackup)
            {
                File.Copy(resumeFile, backupFile, overwrite: true);
            }

            // Write to temp file first
            await File.WriteAllTextAsync(tempFile, json, cancellationToken);

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
        return _config.StorageLocation;
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static float CalculateContentSimilarity(string original, string current)
    {
        // Simple similarity based on character difference ratio
        var distance = ComputeLevenshteinDistance(original, current);
        var maxLength = Math.Max(original.Length, current.Length);

        if (maxLength == 0) return 1.0f;

        var similarity = 1.0f - ((float)distance / maxLength);
        return Math.Max(0.0f, similarity);
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= target.Length; matrix[0, j] = j++) { }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static IEnumerable<CompletedTool> ExtractCompletedToolsFromHistory(List<StepExecutionHistory> executionHistory)
    {
        return executionHistory
            .Where(h => h.Success && h.StepType == "function")
            .Select(h => new CompletedTool
            {
                FunctionName = h.StepName,
                Parameters = new Dictionary<string, object>(),
                Result = "Completed successfully",
                ExecutedAt = h.EndTime,
                Success = h.Success,
                AIReasoning = "Tool execution from step history"
            });
    }

    private static List<FunctionCall>? ExtractFunctionCalls(ChatMessageContent msg)
    {
        // Simple extraction - in a real implementation, this would parse function calls from the message
        return null;
    }

    private static ContextEvolution BuildContextEvolution(WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        return new ContextEvolution
        {
            CurrentContext = context.Variables,
            KeyInsights = new List<string>(),
            Changes = new List<ContextChange>()
        };
    }

    private static AIWorkflowResumeState ConvertToResumeState(WorkflowResumeFile resumeData)
    {
        return new AIWorkflowResumeState
        {
            WorkflowId = resumeData.WorkflowMetadata.Id,
            WorkflowFilePath = resumeData.WorkflowMetadata.FilePath,
            OriginalWorkflowHash = resumeData.WorkflowMetadata.WorkflowHash,
            OriginalWorkflowContent = resumeData.WorkflowMetadata.Id, // Simplified
            StartTime = resumeData.WorkflowMetadata.StartedAt,
            LastActivity = resumeData.WorkflowMetadata.LastCheckpoint,
            CurrentPhase = "in_progress",
            CurrentStrategy = "resume",
            CompletedTools = resumeData.CompletedTools.ToList(),
            ChatHistory = resumeData.ChatHistory.ToList(),
            ContextEvolution = resumeData.ContextEvolution,
            AvailableTools = new List<string>()
        };
    }

    private static WorkflowResumeFile ConvertToResumeFile(AIWorkflowResumeState resumeState)
    {
        return new WorkflowResumeFile
        {
            WorkflowMetadata = new WorkflowMetadata
            {
                Id = resumeState.WorkflowId,
                FilePath = resumeState.WorkflowFilePath,
                WorkflowHash = resumeState.OriginalWorkflowHash,
                StartedAt = resumeState.StartTime,
                LastCheckpoint = resumeState.LastActivity,
                Status = "in_progress"
            },
            CompletedTools = resumeState.CompletedTools.ToArray(),
            ChatHistory = resumeState.ChatHistory.ToArray(),
            ContextEvolution = resumeState.ContextEvolution,
            WorkflowVariables = resumeState.ContextEvolution.CurrentContext
        };
    }

    private static List<StepExecutionHistory> ReconstructExecutionHistory(CompletedTool[] completedTools)
    {
        return completedTools.Select(tool => new StepExecutionHistory
        {
            StepName = tool.FunctionName,
            StepType = "function",
            StartTime = tool.ExecutedAt.DateTime,
            EndTime = tool.ExecutedAt.DateTime,
            Success = tool.Success,
            ErrorMessage = tool.Success ? null : tool.Result
        }).ToList();
    }
}