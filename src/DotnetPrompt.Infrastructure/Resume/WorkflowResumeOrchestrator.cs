using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotnetPrompt.Infrastructure.Resume;

/// <summary>
/// Orchestrates workflow resumption with context-rich AI messaging.
/// Generates intelligent resume context for seamless AI workflow continuation.
/// </summary>
public class WorkflowResumeOrchestrator
{
    private readonly IResumeStateManager _resumeManager;
    private readonly ILogger<WorkflowResumeOrchestrator> _logger;

    public WorkflowResumeOrchestrator(
        IResumeStateManager resumeManager,
        ILogger<WorkflowResumeOrchestrator> logger)
    {
        _resumeManager = resumeManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a context-rich resume message for AI workflow continuation
    /// </summary>
    /// <param name="resumeState">Resume state containing workflow context</param>
    /// <returns>Resume message for AI context</returns>
    public ChatMessageContent CreateAIResumeMessage(AIWorkflowResumeState resumeState)
    {
        _logger.LogDebug("Creating AI resume message for workflow {WorkflowId}", resumeState.WorkflowId);

        var completedTools = resumeState.CompletedTools.Where(t => t.Success).Select(t => t.FunctionName);
        var keyInsights = resumeState.ContextEvolution.KeyInsights.TakeLast(3);
        var sessionDuration = resumeState.LastActivity - resumeState.StartTime;

        var resumeContext = CreateResumeContextMessage(
            resumeState.WorkflowId,
            resumeState.CurrentPhase,
            resumeState.CurrentStrategy,
            sessionDuration,
            completedTools,
            keyInsights,
            resumeState.ContextEvolution.CurrentContext);

        var resumeMessage = new ChatMessageContent(AuthorRole.System, resumeContext);

        _logger.LogInformation("Created AI resume message for workflow {WorkflowId} with {ToolCount} completed tools and {InsightCount} key insights",
            resumeState.WorkflowId, completedTools.Count(), keyInsights.Count());

        return resumeMessage;
    }

    /// <summary>
    /// Determines the current workflow phase from chat history and context
    /// </summary>
    /// <param name="context">Workflow execution context</param>
    /// <param name="chatHistory">Chat history</param>
    /// <returns>Current workflow phase</returns>
    public string DeterminePhaseFromContext(WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        // Strategy 1: Check if phase is explicitly stored in context
        var explicitPhase = context.GetVariable<string>("current_phase");
        if (!string.IsNullOrEmpty(explicitPhase))
        {
            return explicitPhase;
        }

        // Strategy 2: Infer phase from recent chat messages
        var recentMessages = chatHistory.TakeLast(5);
        var phase = InferPhaseFromMessages(recentMessages);
        if (!string.IsNullOrEmpty(phase))
        {
            return phase;
        }

        // Strategy 3: Infer from completed tools
        var completedToolCount = context.ExecutionHistory.Count(h => h.Success);
        return completedToolCount switch
        {
            0 => "understanding",
            var count when count < 3 => "investigating",
            var count when count < 7 => "analyzing",
            _ => "finalizing"
        };
    }

    /// <summary>
    /// Extracts the current AI strategy from chat history
    /// </summary>
    /// <param name="chatHistory">Chat history</param>
    /// <returns>Current strategy description</returns>
    public string ExtractStrategyFromChatHistory(ChatHistory chatHistory)
    {
        // Look for strategy indicators in recent assistant messages
        var recentAssistantMessages = chatHistory
            .Where(m => m.Role == AuthorRole.Assistant)
            .TakeLast(3)
            .Select(m => m.Content)
            .Where(content => !string.IsNullOrEmpty(content));

        foreach (var message in recentAssistantMessages)
        {
            var strategy = ExtractStrategyFromMessage(message);
            if (!string.IsNullOrEmpty(strategy))
            {
                return strategy;
            }
        }

        return "Comprehensive analysis and problem-solving approach";
    }

    /// <summary>
    /// Builds context evolution from execution context and chat history
    /// </summary>
    /// <param name="context">Workflow execution context</param>
    /// <param name="chatHistory">Chat history</param>
    /// <returns>Context evolution data</returns>
    public ContextEvolution BuildContextEvolution(WorkflowExecutionContext context, ChatHistory chatHistory)
    {
        var evolution = new ContextEvolution
        {
            CurrentContext = context.Variables,
            KeyInsights = ExtractKeyInsights(chatHistory),
            Changes = BuildContextChanges(context)
        };

        return evolution;
    }

    /// <summary>
    /// Prepares a chat history for workflow resumption by injecting resume context
    /// </summary>
    /// <param name="existingChatHistory">Existing chat history</param>
    /// <param name="resumeState">Resume state for context</param>
    /// <returns>Enhanced chat history with resume context</returns>
    public ChatHistory PrepareResumeContext(ChatHistory existingChatHistory, AIWorkflowResumeState resumeState)
    {
        var enhancedHistory = new ChatHistory();

        // Add resume message at the beginning for AI context
        var resumeMessage = CreateAIResumeMessage(resumeState);
        enhancedHistory.Add(resumeMessage);

        // Add existing chat history
        foreach (var message in existingChatHistory)
        {
            enhancedHistory.Add(message);
        }

        _logger.LogDebug("Prepared resume context with {MessageCount} messages for workflow {WorkflowId}",
            enhancedHistory.Count, resumeState.WorkflowId);

        return enhancedHistory;
    }

    private string CreateResumeContextMessage(
        string workflowId,
        string currentPhase,
        string currentStrategy,
        TimeSpan sessionDuration,
        IEnumerable<string> completedTools,
        IEnumerable<string> keyInsights,
        Dictionary<string, object> currentContext)
    {
        var toolsList = string.Join(", ", completedTools);
        var insightsList = string.Join("; ", keyInsights);
        var contextItems = currentContext.Take(5).Select(kv => $"• {kv.Key}: {kv.Value}");

        return $"""
            WORKFLOW RESUME CONTEXT - CONTINUE FROM WHERE YOU LEFT OFF
            
            PREVIOUS SESSION SUMMARY:
            • Workflow: {workflowId}
            • Phase when interrupted: {currentPhase}
            • Last strategy: {currentStrategy}
            • Session duration: {sessionDuration:mm\\:ss}
            
            COMPLETED WORK (DO NOT REPEAT):
            • Tools successfully executed: {toolsList}
            • Key discoveries made: {insightsList}
            • Context variables collected: {currentContext.Count} items
            
            CURRENT STATE:
            {string.Join("\n", contextItems)}
            
            RESUME INSTRUCTION:
            You are resuming this workflow exactly where you left off. Review the context above to understand:
            1. What work you've already completed successfully (DO NOT REPEAT)
            2. What insights and context you've gathered so far  
            3. Where you were in the workflow when it was interrupted
            
            Continue your work naturally as if this is one continuous session.
            Start from where you left off and build upon the work already completed.
            """;
    }

    private string InferPhaseFromMessages(IEnumerable<ChatMessageContent> messages)
    {
        var content = string.Join(" ", messages.Select(m => m.Content)).ToLowerInvariant();

        if (content.Contains("understand") || content.Contains("clarify") || content.Contains("what"))
            return "understanding";
        if (content.Contains("investigate") || content.Contains("explore") || content.Contains("examine"))
            return "investigating";
        if (content.Contains("analyze") || content.Contains("process") || content.Contains("review"))
            return "analyzing";
        if (content.Contains("implement") || content.Contains("create") || content.Contains("build"))
            return "implementing";
        if (content.Contains("finalize") || content.Contains("complete") || content.Contains("conclude"))
            return "finalizing";

        return "working";
    }

    private string ExtractStrategyFromMessage(string message)
    {
        var strategyIndicators = new[]
        {
            "approach", "strategy", "plan", "method", "technique",
            "focus on", "concentrate on", "prioritize", "emphasize"
        };

        var lowerMessage = message.ToLowerInvariant();
        
        foreach (var indicator in strategyIndicators)
        {
            var index = lowerMessage.IndexOf(indicator, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                // Extract strategy context around the indicator
                var start = Math.Max(0, index - 50);
                var length = Math.Min(100, message.Length - start);
                return message.Substring(start, length).Trim();
            }
        }

        return string.Empty;
    }

    private List<string> ExtractKeyInsights(ChatHistory chatHistory)
    {
        var insights = new List<string>();

        var assistantMessages = chatHistory
            .Where(m => m.Role == AuthorRole.Assistant)
            .Select(m => m.Content)
            .Where(content => !string.IsNullOrEmpty(content));

        foreach (var message in assistantMessages)
        {
            var messageInsights = ExtractInsightsFromMessage(message);
            insights.AddRange(messageInsights);
        }

        return insights.Distinct().Take(10).ToList();
    }

    private List<string> ExtractInsightsFromMessage(string message)
    {
        var insights = new List<string>();
        var insightPatterns = new[]
        {
            "discovered", "found", "identified", "detected", "noticed",
            "important", "significant", "critical", "key", "main"
        };

        var sentences = message.Split('.', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length < 20 || trimmed.Length > 200) continue;

            var lowerSentence = trimmed.ToLowerInvariant();
            if (insightPatterns.Any(pattern => lowerSentence.Contains(pattern)))
            {
                insights.Add(trimmed);
            }
        }

        return insights;
    }

    private List<ContextChange> BuildContextChanges(WorkflowExecutionContext context)
    {
        var changes = new List<ContextChange>();

        // Build context changes from execution history
        foreach (var historyEntry in context.ExecutionHistory)
        {
            if (historyEntry.Success && !string.IsNullOrEmpty(historyEntry.OutputVariable))
            {
                changes.Add(new ContextChange
                {
                    Timestamp = historyEntry.EndTime,
                    Key = historyEntry.OutputVariable,
                    OldValue = null,
                    NewValue = "Set by step execution",
                    Source = historyEntry.StepName,
                    Reasoning = $"Output from successful execution of {historyEntry.StepType}"
                });
            }
        }

        return changes;
    }
}