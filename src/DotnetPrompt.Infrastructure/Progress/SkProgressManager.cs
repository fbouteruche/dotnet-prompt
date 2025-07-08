using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Infrastructure.Progress;

/// <summary>
/// SK-powered progress manager that uses in-memory storage for persistence
/// </summary>
public class SkProgressManager : IProgressManager
{
    private readonly Dictionary<string, WorkflowProgress> _progressStore = new();
    private readonly ILogger<SkProgressManager> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SkProgressManager(ILogger<SkProgressManager> logger)
    {
        _logger = logger;
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

            // Create execution state from context
            var executionState = new WorkflowExecutionState
            {
                WorkflowId = workflowId,
                CurrentStep = context.CurrentStep,
                StartedAt = context.StartTime,
                LastUpdated = DateTimeOffset.UtcNow,
                Status = WorkflowStatus.InProgress,
                ConversationVariables = new Dictionary<string, object>(context.Variables)
            };

            var progressRecord = new WorkflowProgress
            {
                Id = workflowId,
                ChatHistoryJson = JsonSerializer.Serialize(chatHistory, _jsonOptions),
                ExecutionContextJson = JsonSerializer.Serialize(context, _jsonOptions),
                ExecutionStateJson = JsonSerializer.Serialize(executionState, _jsonOptions),
                LastCheckpoint = DateTimeOffset.UtcNow,
                Status = WorkflowStatus.InProgress.ToString(),
                WorkflowFile = context.GetVariable<string>("workflow_file") ?? string.Empty,
                WorkflowHash = context.GetVariable<string>("workflow_hash") ?? string.Empty
            };

            // Store in in-memory dictionary
            _progressStore[workflowId] = progressRecord;

            _logger.LogInformation("Progress saved for workflow {WorkflowId} at checkpoint {Timestamp}",
                workflowId, progressRecord.LastCheckpoint);

            await Task.CompletedTask; // For async interface compatibility
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

            if (!_progressStore.TryGetValue(workflowId, out var progressRecord))
            {
                _logger.LogDebug("No progress found for workflow {WorkflowId}", workflowId);
                return null;
            }

            // Deserialize ChatHistory and ExecutionContext
            var chatHistory = JsonSerializer.Deserialize<ChatHistory>(progressRecord.ChatHistoryJson, _jsonOptions);
            var executionContext = JsonSerializer.Deserialize<Core.Models.WorkflowExecutionContext>(progressRecord.ExecutionContextJson, _jsonOptions);

            if (chatHistory == null || executionContext == null)
            {
                _logger.LogWarning("Failed to deserialize progress data for workflow {WorkflowId}", workflowId);
                return null;
            }

            _logger.LogInformation("Successfully loaded progress for workflow {WorkflowId} from checkpoint {Timestamp}",
                workflowId, progressRecord.LastCheckpoint);

            await Task.CompletedTask; // For async interface compatibility
            return (executionContext, chatHistory);
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

            // Save updated progress
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
            _logger.LogDebug("Getting available conversation states");

            var results = _progressStore
                .Where(kvp => kvp.Value.Status == WorkflowStatus.InProgress.ToString())
                .Select(kvp => kvp.Key)
                .ToList();
            
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
            _logger.LogDebug("Cleaning up progress records older than {OlderThan}", olderThan);

            var recordsToDelete = _progressStore
                .Where(kvp => kvp.Value.LastCheckpoint < olderThan)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var recordId in recordsToDelete)
            {
                _progressStore.Remove(recordId);
            }

            var cleanedCount = recordsToDelete.Count;
            _logger.LogInformation("Cleaned up {Count} old progress records", cleanedCount);
            return Task.FromResult(cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old progress records");
            throw;
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}