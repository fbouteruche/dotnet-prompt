using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Infrastructure.Progress;

/// <summary>
/// File-based progress manager that uses JSON file storage for SK ChatHistory persistence
/// </summary>
public class FileProgressManager : IProgressManager
{
    private readonly ILogger<FileProgressManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _progressDirectory;

    public FileProgressManager(ILogger<FileProgressManager> logger, string? progressDirectory = null)
    {
        _logger = logger;
        _progressDirectory = progressDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), ".dotnet-prompt", "progress");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task SaveProgressAsync(string workflowId, Core.Models.WorkflowExecutionContext context, ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving progress for workflow {WorkflowId}", workflowId);

            // Ensure progress directory exists
            Directory.CreateDirectory(_progressDirectory);

            // Create progress file path
            var progressFile = Path.Combine(_progressDirectory, $"{workflowId}.json");

            // Convert ChatHistory to file format
            var chatMessages = chatHistory.Select(msg => new ChatMessage
            {
                Role = msg.Role.ToString(),
                Content = msg.Content,
                Timestamp = DateTimeOffset.UtcNow
            }).ToArray();

            // Create progress data structure
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
                ChatHistory = chatMessages,
                ExecutionContext = new ExecutionContextData
                {
                    CurrentStep = context.CurrentStep,
                    Variables = context.Variables,
                    ExecutionHistory = context.ExecutionHistory
                }
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(progressData, _jsonOptions);

            // Write to temporary file first for atomic operation
            var tempFile = progressFile + ".tmp";
            await File.WriteAllTextAsync(tempFile, json, cancellationToken);

            // Atomic move from temp to final location
            File.Move(tempFile, progressFile, overwrite: true);

            _logger.LogInformation("Progress saved to {ProgressFile} for workflow {WorkflowId}",
                progressFile, workflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save progress for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<(Core.Models.WorkflowExecutionContext? Context, ChatHistory? ChatHistory)?> LoadProgressAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading progress for workflow {WorkflowId}", workflowId);

            var progressFile = Path.Combine(_progressDirectory, $"{workflowId}.json");

            if (!File.Exists(progressFile))
            {
                _logger.LogDebug("No progress file found for workflow {WorkflowId}", workflowId);
                return null;
            }

            // Read and deserialize progress file
            var json = await File.ReadAllTextAsync(progressFile, cancellationToken);
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
                var role = msg.Role.ToLowerInvariant() switch
                {
                    "user" => AuthorRole.User,
                    "assistant" => AuthorRole.Assistant,
                    "system" => AuthorRole.System,
                    "tool" => AuthorRole.Tool,
                    _ => AuthorRole.Assistant
                };

                chatHistory.Add(new ChatMessageContent(role, msg.Content ?? string.Empty));
            }

            // Restore ExecutionContext with variable type conversion
            var context = new Core.Models.WorkflowExecutionContext
            {
                CurrentStep = progressData.ExecutionContext.CurrentStep,
                Variables = ConvertVariables(progressData.ExecutionContext.Variables),
                ExecutionHistory = progressData.ExecutionContext.ExecutionHistory,
                StartTime = progressData.WorkflowMetadata.StartedAt.DateTime
            };

            // Add workflow metadata back to context
            context.SetVariable("workflow_file", progressData.WorkflowMetadata.FilePath);
            context.SetVariable("workflow_hash", progressData.WorkflowMetadata.WorkflowHash);

            _logger.LogInformation("Successfully loaded progress for workflow {WorkflowId} from {ProgressFile}",
                workflowId, progressFile);

            return (context, chatHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load progress for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task TrackStepCompletionAsync(string workflowId, StepProgress stepProgress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Tracking step completion for workflow {WorkflowId}, step {StepName}", workflowId, stepProgress.StepName);

            // Load existing progress to update
            var existing = await LoadProgressAsync(workflowId, cancellationToken);
            if (existing == null)
            {
                _logger.LogWarning("Cannot track step completion - no existing progress found for workflow {WorkflowId}", workflowId);
                return;
            }

            var (context, chatHistory) = existing.Value;
            if (context == null || chatHistory == null)
            {
                _logger.LogWarning("Cannot track step completion - invalid progress data for workflow {WorkflowId}", workflowId);
                return;
            }

            // Add step completion to chat history
            var completionMessage = stepProgress.Success
                ? $"Step '{stepProgress.StepName}' completed successfully at {stepProgress.CompletedAt}"
                : $"Step '{stepProgress.StepName}' failed at {stepProgress.CompletedAt}: {stepProgress.ErrorMessage}";

            chatHistory.AddAssistantMessage(completionMessage);

            // Update execution history
            context.ExecutionHistory.Add(new StepExecutionHistory
            {
                StepName = stepProgress.StepName,
                StepType = stepProgress.StepType,
                StartTime = stepProgress.CompletedAt.Subtract(stepProgress.Duration).DateTime,
                EndTime = stepProgress.CompletedAt.DateTime,
                Success = stepProgress.Success,
                ErrorMessage = stepProgress.ErrorMessage
            });

            // Save updated progress to file
            await SaveProgressAsync(workflowId, context, chatHistory, cancellationToken);

            _logger.LogInformation("Step completion tracked for workflow {WorkflowId}, step {StepName}, success: {Success}",
                workflowId, stepProgress.StepName, stepProgress.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track step completion for workflow {WorkflowId}, step {StepName}",
                workflowId, stepProgress.StepName);
            throw;
        }
    }

    public async Task TrackStepFailureAsync(string workflowId, string stepName, Exception exception, CancellationToken cancellationToken = default)
    {
        var stepProgress = new StepProgress
        {
            StepName = stepName,
            StepType = "unknown",
            Success = false,
            ErrorMessage = exception.Message,
            CompletedAt = DateTimeOffset.UtcNow
        };

        await TrackStepCompletionAsync(workflowId, stepProgress, cancellationToken);
    }

    public Task<IEnumerable<string>> GetAvailableConversationStatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting available conversation states from {ProgressDirectory}", _progressDirectory);

            if (!Directory.Exists(_progressDirectory))
            {
                _logger.LogDebug("Progress directory does not exist, no conversation states available");
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }

            // Find all .json files in progress directory
            var progressFiles = Directory.GetFiles(_progressDirectory, "*.json");
            var results = new List<string>();

            foreach (var file in progressFiles)
            {
                try
                {
                    // Extract workflow ID from filename (remove .json extension)
                    var workflowId = Path.GetFileNameWithoutExtension(file);
                    
                    // Verify it's a valid progress file by checking if it can be parsed
                    var json = File.ReadAllText(file);
                    var progressData = JsonSerializer.Deserialize<WorkflowProgressFile>(json, _jsonOptions);
                    
                    if (progressData?.WorkflowMetadata.Status == "in_progress")
                    {
                        results.Add(workflowId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping invalid progress file {File}", file);
                }
            }
            
            _logger.LogInformation("Found {Count} available conversation states", results.Count);
            return Task.FromResult<IEnumerable<string>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available conversation states");
            throw;
        }
    }

    public async Task<bool> ValidateWorkflowCompatibilityAsync(string workflowId, string currentWorkflowContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating workflow compatibility for {WorkflowId}", workflowId);

            // Get current workflow hash
            var currentHash = ComputeHash(currentWorkflowContent);

            // Load existing progress to get stored hash
            var existing = await LoadProgressAsync(workflowId, cancellationToken);
            if (existing == null)
            {
                _logger.LogDebug("No existing progress found, workflow is compatible for first run");
                return true;
            }

            var (context, _) = existing.Value;
            var storedHash = context?.GetVariable<string>("workflow_hash");

            if (string.IsNullOrEmpty(storedHash))
            {
                _logger.LogWarning("No stored workflow hash found, assuming compatible");
                return true;
            }

            // Simple hash-based compatibility check
            var isCompatible = currentHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Workflow compatibility check for {WorkflowId}: {IsCompatible} (hash match: {HashMatch})",
                workflowId, isCompatible, currentHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase));

            return isCompatible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate workflow compatibility for {WorkflowId}", workflowId);
            throw;
        }
    }

    public Task<int> CleanupOldProgressAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Cleaning up progress files older than {OlderThan} in {ProgressDirectory}", olderThan, _progressDirectory);

            if (!Directory.Exists(_progressDirectory))
            {
                _logger.LogDebug("Progress directory does not exist, no cleanup needed");
                return Task.FromResult(0);
            }

            var progressFiles = Directory.GetFiles(_progressDirectory, "*.json");
            var deletedCount = 0;

            foreach (var file in progressFiles)
            {
                try
                {
                    // Check file modification time
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < olderThan)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old progress file {File}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete progress file {File}", file);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old progress files", deletedCount);
            return Task.FromResult(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old progress files");
            throw;
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Convert JsonElement values back to proper types after JSON deserialization
    /// </summary>
    private static Dictionary<string, object> ConvertVariables(Dictionary<string, object> variables)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var kvp in variables)
        {
            var value = kvp.Value;
            
            // Handle JsonElement values that come from JSON deserialization
            if (value is JsonElement jsonElement)
            {
                value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                    JsonValueKind.Number => jsonElement.TryGetInt32(out var intVal) ? intVal : jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => string.Empty,
                    _ => jsonElement.ToString()
                };
            }
            
            result[kvp.Key] = value;
        }
        
        return result;
    }
}