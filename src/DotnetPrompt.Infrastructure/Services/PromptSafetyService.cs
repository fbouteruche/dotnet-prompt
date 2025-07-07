using System.Diagnostics;
using DotnetPrompt.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DotnetPrompt.Infrastructure.Services;

/// <summary>
/// SK-native prompt safety service that leverages built-in content safety features
/// including OpenAI moderation API and content filtering capabilities
/// </summary>
public interface IPromptSafetyService
{
    /// <summary>
    /// Validates prompt content using SK's built-in content safety mechanisms
    /// </summary>
    Task<PromptSafetyResult> ValidatePromptAsync(string content, string correlationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates function parameters for safety concerns
    /// </summary>
    Task<PromptSafetyResult> ValidateParametersAsync(KernelArguments arguments, string functionName, string correlationId, CancellationToken cancellationToken = default);
}

public class PromptSafetyService : IPromptSafetyService
{
    private readonly ILogger<PromptSafetyService> _logger;
    private readonly Kernel _kernel;
    private readonly OpenAIModerationService? _moderationService;

    public PromptSafetyService(ILogger<PromptSafetyService> logger, Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
        
        // Try to initialize OpenAI moderation service if API key is available
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                _moderationService = new OpenAIModerationService(apiKey);
                _logger.LogDebug("OpenAI moderation service initialized for prompt safety validation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize OpenAI moderation service: {Message}", ex.Message);
            }
        }
        else
        {
            _logger.LogDebug("OpenAI API key not available, using fallback prompt safety validation");
        }
    }

    public async Task<PromptSafetyResult> ValidatePromptAsync(string content, string correlationId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting prompt safety validation with correlation {CorrelationId}", correlationId);

            // Use OpenAI moderation service if available
            if (_moderationService != null)
            {
                var moderationResult = await _moderationService.CheckModerationAsync(content, cancellationToken);
                
                if (moderationResult.IsFlagged)
                {
                    _logger.LogWarning("OpenAI moderation flagged content with categories: {Categories} for correlation {CorrelationId}", 
                        string.Join(", ", moderationResult.FlaggedCategories), correlationId);
                    
                    return new PromptSafetyResult(
                        IsValid: false,
                        RiskLevel: DetermineRiskLevel(moderationResult.FlaggedCategories),
                        Issues: moderationResult.FlaggedCategories.ToArray(),
                        Recommendation: "Content was flagged by OpenAI moderation service. Please review and modify the prompt.",
                        ValidationTime: stopwatch.Elapsed
                    );
                }
            }
            
            // Fallback to enhanced pattern-based validation
            var fallbackResult = await ValidateWithEnhancedPatterns(content, correlationId);
            
            _logger.LogDebug("Prompt safety validation completed in {Duration}ms for correlation {CorrelationId}", 
                stopwatch.ElapsedMilliseconds, correlationId);
            
            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during prompt safety validation for correlation {CorrelationId}", correlationId);
            
            // Return a conservative result on error
            return new PromptSafetyResult(
                IsValid: false,
                RiskLevel: PromptRiskLevel.High,
                Issues: new[] { "Validation error occurred" },
                Recommendation: "Could not validate prompt safety. Please review manually.",
                ValidationTime: stopwatch.Elapsed
            );
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<PromptSafetyResult> ValidateParametersAsync(KernelArguments arguments, string functionName, string correlationId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var issues = new List<string>();
        var maxRiskLevel = PromptRiskLevel.Low;

        try
        {
            _logger.LogDebug("Starting parameter safety validation for function {FunctionName} with correlation {CorrelationId}", 
                functionName, correlationId);

            foreach (var argument in arguments)
            {
                var paramValue = argument.Value?.ToString();
                if (string.IsNullOrEmpty(paramValue))
                    continue;

                // Validate each parameter value
                var paramResult = await ValidatePromptAsync(paramValue, correlationId, cancellationToken);
                
                if (!paramResult.IsValid)
                {
                    issues.AddRange(paramResult.Issues.Select(issue => $"Parameter '{argument.Key}': {issue}"));
                    
                    if (paramResult.RiskLevel > maxRiskLevel)
                        maxRiskLevel = paramResult.RiskLevel;
                }

                // Check for sensitive data exposure
                if (ContainsSensitiveData(argument.Key, paramValue))
                {
                    issues.Add($"Parameter '{argument.Key}' may contain sensitive information");
                    maxRiskLevel = PromptRiskLevel.Medium;
                }
            }

            var isValid = issues.Count == 0;
            var recommendation = isValid 
                ? "All parameters passed safety validation" 
                : $"Found {issues.Count} safety issues in function parameters. Please review and sanitize input.";

            _logger.LogDebug("Parameter safety validation completed in {Duration}ms for function {FunctionName} with correlation {CorrelationId}", 
                stopwatch.ElapsedMilliseconds, functionName, correlationId);

            return new PromptSafetyResult(
                IsValid: isValid,
                RiskLevel: maxRiskLevel,
                Issues: issues.ToArray(),
                Recommendation: recommendation,
                ValidationTime: stopwatch.Elapsed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during parameter safety validation for function {FunctionName} with correlation {CorrelationId}", 
                functionName, correlationId);
            
            return new PromptSafetyResult(
                IsValid: false,
                RiskLevel: PromptRiskLevel.High,
                Issues: new[] { "Parameter validation error occurred" },
                Recommendation: "Could not validate parameter safety. Please review manually.",
                ValidationTime: stopwatch.Elapsed
            );
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Enhanced pattern-based validation as fallback when moderation service is not available
    /// </summary>
    private Task<PromptSafetyResult> ValidateWithEnhancedPatterns(string content, string correlationId)
    {
        var issues = new List<string>();
        var contentLower = content.ToLowerInvariant();
        var riskLevel = PromptRiskLevel.Low;

        // Enhanced prompt injection patterns with severity levels
        var highRiskPatterns = new[]
        {
            "ignore previous instructions",
            "ignore all previous instructions",
            "forget previous instructions",
            "disregard previous instructions",
            "override previous instructions",
            "new instructions:",
            "system prompt:",
            "you are now",
            "act as if",
            "pretend you are",
            "roleplay as",
            "jailbreak",
            "dev mode",
            "ignore safety",
            "bypass safety",
            "ignore guidelines"
        };

        var mediumRiskPatterns = new[]
        {
            "ignore previous",
            "forget previous", 
            "disregard previous",
            "act as",
            "roleplay",
            "pretend",
            "simulate",
            "imagine you are",
            "let's pretend",
            "in this scenario"
        };

        // Check for high-risk patterns
        foreach (var pattern in highRiskPatterns)
        {
            if (contentLower.Contains(pattern))
            {
                issues.Add($"High-risk prompt injection pattern detected: '{pattern}'");
                riskLevel = PromptRiskLevel.High;
                _logger.LogWarning("High-risk prompt injection pattern '{Pattern}' detected for correlation {CorrelationId}", 
                    pattern, correlationId);
            }
        }

        // Check for medium-risk patterns (only if no high-risk found)
        if (riskLevel != PromptRiskLevel.High)
        {
            foreach (var pattern in mediumRiskPatterns)
            {
                if (contentLower.Contains(pattern))
                {
                    issues.Add($"Medium-risk prompt injection pattern detected: '{pattern}'");
                    riskLevel = PromptRiskLevel.Medium;
                    _logger.LogWarning("Medium-risk prompt injection pattern '{Pattern}' detected for correlation {CorrelationId}", 
                        pattern, correlationId);
                }
            }
        }

        // Check for excessive length (potential for complex injection)
        if (content.Length > 10000)
        {
            issues.Add($"Unusually long input detected: {content.Length} characters");
            if (riskLevel == PromptRiskLevel.Low)
                riskLevel = PromptRiskLevel.Medium;
        }

        // Check for repeated characters or patterns (potential obfuscation)
        if (HasSuspiciousRepetition(content))
        {
            issues.Add("Suspicious character repetition detected (potential obfuscation)");
            if (riskLevel == PromptRiskLevel.Low)
                riskLevel = PromptRiskLevel.Medium;
        }

        var isValid = issues.Count == 0;
        var recommendation = isValid 
            ? "Content passed enhanced pattern validation" 
            : $"Found {issues.Count} potential safety issues. Content may contain prompt injection attempts.";

        return Task.FromResult(new PromptSafetyResult(
            IsValid: isValid,
            RiskLevel: riskLevel,
            Issues: issues.ToArray(),
            Recommendation: recommendation,
            ValidationTime: TimeSpan.Zero
        ));
    }

    private static PromptRiskLevel DetermineRiskLevel(IEnumerable<string> flaggedCategories)
    {
        var categories = flaggedCategories.ToArray();
        
        // High-risk categories
        if (categories.Any(c => c.Contains("violence") || c.Contains("harassment") || c.Contains("hate")))
            return PromptRiskLevel.High;
        
        // Medium-risk categories  
        if (categories.Any(c => c.Contains("sexual") || c.Contains("self-harm")))
            return PromptRiskLevel.Medium;
        
        return PromptRiskLevel.Low;
    }

    private static bool ContainsSensitiveData(string parameterName, string value)
    {
        var sensitivePatterns = new[] { "api", "key", "token", "password", "secret", "credential" };
        var paramLower = parameterName.ToLowerInvariant();
        
        return sensitivePatterns.Any(pattern => paramLower.Contains(pattern)) ||
               value.Length > 50 && (value.Contains("sk-") || value.Contains("Bearer ") || value.Contains("pk_"));
    }

    private static bool HasSuspiciousRepetition(string content)
    {
        // Simple check for repeated characters or short sequences
        var charCounts = new Dictionary<char, int>();
        
        foreach (var c in content)
        {
            charCounts[c] = charCounts.GetValueOrDefault(c, 0) + 1;
        }
        
        // Flag if any character appears more than 20% of the total length
        var threshold = Math.Max(10, content.Length / 5);
        return charCounts.Values.Any(count => count > threshold);
    }
}

/// <summary>
/// Wrapper for OpenAI moderation service integration
/// </summary>
internal class OpenAIModerationService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public OpenAIModerationService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<ModerationResult> CheckModerationAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new { input = content };
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/moderations", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                // Return non-flagged result if API call fails
                return new ModerationResult(false, Array.Empty<string>());
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var moderationResponse = System.Text.Json.JsonSerializer.Deserialize<ModerationResponse>(responseJson);
            
            if (moderationResponse?.results?.Length > 0)
            {
                var result = moderationResponse.results[0];
                var flaggedCategories = new List<string>();
                
                if (result.category_scores != null)
                {
                    foreach (var category in result.categories.GetType().GetProperties())
                    {
                        var value = category.GetValue(result.categories);
                        if (value is bool isTrue && isTrue)
                        {
                            flaggedCategories.Add(category.Name);
                        }
                    }
                }
                
                return new ModerationResult(result.flagged, flaggedCategories);
            }
            
            return new ModerationResult(false, Array.Empty<string>());
        }
        catch
        {
            // Return non-flagged result if moderation check fails
            return new ModerationResult(false, Array.Empty<string>());
        }
    }
}

/// <summary>
/// Result of prompt safety validation
/// </summary>
public record PromptSafetyResult(
    bool IsValid,
    PromptRiskLevel RiskLevel,
    string[] Issues,
    string Recommendation,
    TimeSpan ValidationTime
);

/// <summary>
/// Risk levels for prompt content
/// </summary>
public enum PromptRiskLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// OpenAI moderation result
/// </summary>
internal record ModerationResult(bool IsFlagged, IEnumerable<string> FlaggedCategories);

/// <summary>
/// OpenAI moderation API response structure
/// </summary>
internal class ModerationResponse
{
    public string id { get; set; } = "";
    public string model { get; set; } = "";
    public ModerationResultData[] results { get; set; } = Array.Empty<ModerationResultData>();
}

internal class ModerationResultData
{
    public bool flagged { get; set; }
    public ModerationCategories categories { get; set; } = new();
    public ModerationCategoryScores category_scores { get; set; } = new();
}

internal class ModerationCategories
{
    public bool sexual { get; set; }
    public bool hate { get; set; }
    public bool harassment { get; set; }
    public bool violence { get; set; }
    public bool self_harm { get; set; }
}

internal class ModerationCategoryScores
{
    public double sexual { get; set; }
    public double hate { get; set; }
    public double harassment { get; set; }
    public double violence { get; set; }
    public double self_harm { get; set; }
}