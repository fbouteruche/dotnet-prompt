using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.Resume;

/// <summary>
/// Optimizes AI workflow resume state for storage efficiency while preserving essential context.
/// Focuses on keeping only the most important information needed for intelligent resumption.
/// </summary>
public class AIWorkflowResumeOptimization
{
    private readonly ILogger<AIWorkflowResumeOptimization> _logger;

    // Keep state lean - only essential information for resume
    private readonly int MaxCompletedTools = 50;       // Recent successful tool executions
    private readonly int MaxChatHistory = 20;          // Recent conversation context  
    private readonly int MaxContextVariables = 30;     // Essential context variables
    private readonly int MaxKeyInsights = 10;          // Important discoveries

    public AIWorkflowResumeOptimization(ILogger<AIWorkflowResumeOptimization> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Optimizes resume state for storage by keeping only essential information.
    /// Focuses on what's needed for intelligent AI resumption, not progress tracking.
    /// </summary>
    /// <param name="resumeState">Resume state to optimize</param>
    /// <returns>Optimized resume state</returns>
    public AIWorkflowResumeState OptimizeForStorage(AIWorkflowResumeState resumeState)
    {
        _logger.LogDebug("Optimizing resume state for workflow {WorkflowId}", resumeState.WorkflowId);

        var originalSize = EstimateStateSize(resumeState);

        // Keep only successful tool executions for resume context
        if (resumeState.CompletedTools.Count > MaxCompletedTools)
        {
            var originalCount = resumeState.CompletedTools.Count;
            resumeState.CompletedTools = resumeState.CompletedTools
                .Where(t => t.Success)
                .OrderByDescending(t => t.ExecutedAt)
                .Take(MaxCompletedTools)
                .ToList();

            _logger.LogDebug("Reduced completed tools from {Original} to {Optimized} for workflow {WorkflowId}",
                originalCount, resumeState.CompletedTools.Count, resumeState.WorkflowId);
        }

        // Limit context variables to most essential
        if (resumeState.ContextEvolution.CurrentContext.Count > MaxContextVariables)
        {
            var originalCount = resumeState.ContextEvolution.CurrentContext.Count;
            var essential = resumeState.ContextEvolution.CurrentContext
                .OrderByDescending(kv => EstimateResumeImportance(kv.Key, kv.Value))
                .Take(MaxContextVariables)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            resumeState.ContextEvolution.CurrentContext = essential;

            _logger.LogDebug("Reduced context variables from {Original} to {Optimized} for workflow {WorkflowId}",
                originalCount, essential.Count, resumeState.WorkflowId);
        }

        // Keep recent chat history for context
        if (resumeState.ChatHistory.Count > MaxChatHistory)
        {
            var originalCount = resumeState.ChatHistory.Count;
            resumeState.ChatHistory = resumeState.ChatHistory
                .OrderByDescending(m => m.Timestamp)
                .Take(MaxChatHistory)
                .OrderBy(m => m.Timestamp)
                .ToList();

            _logger.LogDebug("Reduced chat history from {Original} to {Optimized} messages for workflow {WorkflowId}",
                originalCount, resumeState.ChatHistory.Count, resumeState.WorkflowId);
        }

        // Limit key insights to most important discoveries
        if (resumeState.ContextEvolution.KeyInsights.Count > MaxKeyInsights)
        {
            var originalCount = resumeState.ContextEvolution.KeyInsights.Count;
            resumeState.ContextEvolution.KeyInsights = resumeState.ContextEvolution.KeyInsights
                .TakeLast(MaxKeyInsights)
                .ToList();

            _logger.LogDebug("Reduced key insights from {Original} to {Optimized} for workflow {WorkflowId}",
                originalCount, resumeState.ContextEvolution.KeyInsights.Count, resumeState.WorkflowId);
        }

        // Optimize context changes to keep only recent and important ones
        OptimizeContextChanges(resumeState);

        var optimizedSize = EstimateStateSize(resumeState);
        var reductionPercent = originalSize > 0 ? ((originalSize - optimizedSize) / (float)originalSize) * 100 : 0;

        _logger.LogInformation("Optimized resume state for workflow {WorkflowId}: {ReductionPercent:F1}% size reduction",
            resumeState.WorkflowId, reductionPercent);

        return resumeState;
    }

    /// <summary>
    /// Estimates the importance of a context variable for resume functionality
    /// </summary>
    /// <param name="key">Context variable key</param>
    /// <param name="value">Context variable value</param>
    /// <returns>Importance score (higher = more important)</returns>
    private float EstimateResumeImportance(string key, object value)
    {
        float score = 1.0f;

        // Keys critical for resume context
        if (CriticalResumeKeys.Contains(key.ToLower()))
            score *= 3.0f;

        // Recent context changes are more important
        var valueStr = value?.ToString();
        if (!string.IsNullOrEmpty(valueStr))
        {
            if (valueStr.Contains("discovered", StringComparison.OrdinalIgnoreCase) || 
                valueStr.Contains("found", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Contains("analysis", StringComparison.OrdinalIgnoreCase))
                score *= 2.0f;

            // Workflow-specific important terms
            if (valueStr.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                valueStr.Contains("requirement", StringComparison.OrdinalIgnoreCase))
                score *= 2.5f;
        }

        // File paths and configurations are important
        if (key.EndsWith("_path", StringComparison.OrdinalIgnoreCase) ||
            key.EndsWith("_file", StringComparison.OrdinalIgnoreCase) ||
            key.EndsWith("_config", StringComparison.OrdinalIgnoreCase))
            score *= 2.0f;

        return score;
    }

    /// <summary>
    /// Optimizes context changes to keep only the most relevant for resume
    /// </summary>
    /// <param name="resumeState">Resume state to optimize</param>
    private void OptimizeContextChanges(AIWorkflowResumeState resumeState)
    {
        const int maxContextChanges = 20;

        if (resumeState.ContextEvolution.Changes.Count <= maxContextChanges)
            return;

        var originalCount = resumeState.ContextEvolution.Changes.Count;

        // Keep recent changes and changes from critical sources
        var optimizedChanges = resumeState.ContextEvolution.Changes
            .OrderByDescending(c => c.Timestamp)
            .Where(c => IsImportantContextChange(c))
            .Take(maxContextChanges)
            .OrderBy(c => c.Timestamp)
            .ToList();

        resumeState.ContextEvolution.Changes = optimizedChanges;

        _logger.LogDebug("Reduced context changes from {Original} to {Optimized} for workflow {WorkflowId}",
            originalCount, optimizedChanges.Count, resumeState.WorkflowId);
    }

    /// <summary>
    /// Determines if a context change is important enough to keep for resume
    /// </summary>
    /// <param name="change">Context change to evaluate</param>
    /// <returns>True if the change is important for resume context</returns>
    private bool IsImportantContextChange(ContextChange change)
    {
        // Always keep recent changes (last hour)
        if (change.Timestamp > DateTimeOffset.UtcNow.AddHours(-1))
            return true;

        // Keep changes from critical sources
        var criticalSources = new[] { "user_input", "critical_error", "workflow_start", "phase_change" };
        if (criticalSources.Any(source => change.Source.Contains(source, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Keep changes to critical keys
        if (CriticalResumeKeys.Contains(change.Key.ToLower()))
            return true;

        // Keep changes with important reasoning
        if (!string.IsNullOrEmpty(change.Reasoning) &&
            (change.Reasoning.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
             change.Reasoning.Contains("error", StringComparison.OrdinalIgnoreCase) ||
             change.Reasoning.Contains("discovered", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    /// <summary>
    /// Estimates the storage size of a resume state for optimization metrics
    /// </summary>
    /// <param name="resumeState">Resume state to measure</param>
    /// <returns>Estimated size in bytes</returns>
    private int EstimateStateSize(AIWorkflowResumeState resumeState)
    {
        var size = 0;

        // Estimate based on content length
        size += resumeState.WorkflowId.Length;
        size += resumeState.WorkflowFilePath.Length;
        size += resumeState.OriginalWorkflowContent.Length;
        size += resumeState.CurrentPhase.Length;
        size += resumeState.CurrentStrategy.Length;

        // Completed tools
        size += resumeState.CompletedTools.Sum(t => 
            t.FunctionName.Length + 
            (t.Result?.Length ?? 0) + 
            (t.AIReasoning?.Length ?? 0) +
            t.Parameters.Sum(p => p.Key.Length + (p.Value?.ToString()?.Length ?? 0)));

        // Chat history
        size += resumeState.ChatHistory.Sum(m => 
            m.Role.Length + 
            m.Content.Length + 
            (m.FunctionCalls?.Sum(f => f.FunctionName.Length + f.CallId.Length) ?? 0));

        // Context evolution
        size += resumeState.ContextEvolution.CurrentContext.Sum(kv => 
            kv.Key.Length + (kv.Value?.ToString()?.Length ?? 0));
        size += resumeState.ContextEvolution.KeyInsights.Sum(i => i.Length);
        size += resumeState.ContextEvolution.Changes.Sum(c => 
            c.Key.Length + c.Source.Length + c.Reasoning.Length);

        return size;
    }

    /// <summary>
    /// Keys that are critical for workflow resumption
    /// </summary>
    private readonly HashSet<string> CriticalResumeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "project_path", "main_goal", "current_phase", "last_strategy",
        "key_findings", "target_framework", "critical_issue", "file_path",
        "current_analysis", "next_steps", "workflow_intent", "user_request",
        "analysis_result", "error_state", "completion_criteria", "workflow_file",
        "workflow_hash", "original_content", "available_tools", "execution_context"
    };
}