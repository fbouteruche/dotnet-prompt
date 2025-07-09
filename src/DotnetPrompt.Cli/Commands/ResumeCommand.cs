using System.CommandLine;
using DotnetPrompt.Cli.Options;
using DotnetPrompt.Core;
using DotnetPrompt.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Cli.Commands;

/// <summary>
/// Resume command implementation for resuming interrupted workflows
/// </summary>
public class ResumeCommand : Command
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly IProgressManager? _progressManager;
    private readonly ILogger<ResumeCommand> _logger;

    public ResumeCommand(
        IWorkflowOrchestrator orchestrator, 
        IProgressManager? progressManager,
        ILogger<ResumeCommand> logger)
        : base("resume", "Resume a previously interrupted workflow from saved progress")
    {
        _orchestrator = orchestrator;
        _progressManager = progressManager;
        _logger = logger;

        // Workflow file argument (required)
        var workflowFileArgument = new Argument<string>(
            name: "workflow-file",
            description: "Path to the original .prompt.md workflow file");

        // Command options
        var workflowIdOption = new Option<string?>(
            aliases: new[] { "--workflow-id", "-w" },
            description: "Specific workflow execution ID to resume")
        {
            IsRequired = false
        };

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Force resume even with compatibility warnings")
        {
            IsRequired = false
        };

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed resume process information")
        {
            IsRequired = false
        };

        var listOption = new Option<bool>(
            aliases: new[] { "--list", "-l" },
            description: "List available resumable workflow states")
        {
            IsRequired = false
        };

        // Add argument and options
        AddArgument(workflowFileArgument);
        AddOption(workflowIdOption);
        AddOption(forceOption);
        AddOption(verboseOption);
        AddOption(listOption);

        // Set command handler
        this.SetHandler(async (workflowFile, workflowId, force, verbose, list) =>
        {
            try
            {
                int exitCode;
                
                if (list)
                {
                    exitCode = await ListAvailableStatesAsync();
                }
                else if (string.IsNullOrEmpty(workflowFile))
                {
                    _logger.LogError("Workflow file path is required unless using --list option");
                    exitCode = ExitCodes.InvalidArguments;
                }
                else
                {
                    exitCode = await ResumeWorkflowAsync(workflowFile, workflowId, force, verbose);
                }
                
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during resume command execution");
                Environment.Exit(ExitCodes.UnexpectedError);
            }
        }, workflowFileArgument, workflowIdOption, forceOption, verboseOption, listOption);
    }

    private async Task<int> ListAvailableStatesAsync()
    {
        try
        {
            if (_progressManager == null)
            {
                Console.Error.WriteLine("Progress manager is not available - resume functionality is disabled");
                _logger.LogWarning("Progress manager is not available - resume functionality is disabled");
                return ExitCodes.FeatureNotAvailable;
            }

            _logger.LogInformation("Searching for available resumable workflow states...");
            
            var availableStates = await _progressManager.GetAvailableConversationStatesAsync();
            var states = availableStates.ToList();

            if (!states.Any())
            {
                _logger.LogInformation("No resumable workflow states found");
                return ExitCodes.NoProgressFound;
            }

            _logger.LogInformation("Found {Count} resumable workflow state(s):", states.Count);
            
            foreach (var state in states)
            {
                Console.WriteLine($"  - {state}");
            }

            _logger.LogInformation("Use 'dotnet prompt resume <workflow-file> --workflow-id <id>' to resume a specific workflow");
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list available workflow states");
            return ExitCodes.UnexpectedError;
        }
    }

    private async Task<int> ResumeWorkflowAsync(string workflowFile, string? workflowId, bool force, bool verbose)
    {
        try
        {
            if (_progressManager == null)
            {
                _logger.LogError("Progress manager is not available - resume functionality is disabled");
                return ExitCodes.FeatureNotAvailable;
            }

            // Validate workflow file exists
            if (!File.Exists(workflowFile))
            {
                Console.Error.WriteLine($"Workflow file not found: {workflowFile}");
                _logger.LogError("Workflow file not found: {WorkflowFile}", workflowFile);
                return ExitCodes.FileNotFound;
            }

            // If no specific workflow ID provided, try to find resumable states
            if (string.IsNullOrEmpty(workflowId))
            {
                var availableStates = await _progressManager.GetAvailableConversationStatesAsync();
                var states = availableStates.ToList();

                if (!states.Any())
                {
                    Console.Error.WriteLine("No resumable workflow states found. Start a new workflow execution instead.");
                    _logger.LogWarning("No resumable workflow states found. Start a new workflow execution instead.");
                    return ExitCodes.NoProgressFound;
                }

                if (states.Count == 1)
                {
                    workflowId = states.First();
                    _logger.LogInformation("Found single resumable state, using: {WorkflowId}", workflowId);
                }
                else
                {
                    Console.Error.WriteLine("Multiple resumable states found. Please specify --workflow-id:");
                    _logger.LogWarning("Multiple resumable states found. Please specify --workflow-id:");
                    foreach (var state in states)
                    {
                        Console.WriteLine($"  - {state}");
                    }
                    return ExitCodes.AmbiguousInput;
                }
            }

            if (verbose)
            {
                _logger.LogInformation("Resuming workflow {WorkflowFile} with ID {WorkflowId}", workflowFile, workflowId);
            }

            // Load workflow content for compatibility validation
            var workflowContent = await File.ReadAllTextAsync(workflowFile);
            
            // Validate workflow compatibility unless forced
            if (!force)
            {
                var isCompatible = await _progressManager.ValidateWorkflowCompatibilityAsync(workflowId, workflowContent);
                if (!isCompatible)
                {
                    Console.Error.WriteLine("Workflow has changed significantly since last execution and may not be compatible for resume.");
                    Console.Error.WriteLine("Use --force to attempt resume anyway, or start a new workflow execution.");
                    _logger.LogError("Workflow has changed significantly since last execution and may not be compatible for resume.");
                    _logger.LogError("Use --force to attempt resume anyway, or start a new workflow execution.");
                    return ExitCodes.WorkflowValidationError;
                }
            }

            // Parse workflow for orchestrator
            // Note: This is simplified - in a real implementation, you'd need to parse the workflow properly
            var workflow = new DotnetPrompt.Core.Models.DotpromptWorkflow
            {
                Name = Path.GetFileNameWithoutExtension(workflowFile),
                Content = new DotnetPrompt.Core.Models.WorkflowContent { RawMarkdown = workflowContent }
            };

            if (verbose)
            {
                _logger.LogInformation("Starting workflow resume...");
            }

            // Resume workflow execution
            var result = await _orchestrator.ResumeWorkflowAsync(workflowId, workflow);

            if (result.Success)
            {
                _logger.LogInformation("Workflow resumed and completed successfully");
                if (verbose && !string.IsNullOrEmpty(result.Output))
                {
                    Console.WriteLine("Output:");
                    Console.WriteLine(result.Output);
                }
                return ExitCodes.Success;
            }
            else
            {
                Console.Error.WriteLine($"Workflow resume failed: {result.ErrorMessage}");
                _logger.LogError("Workflow resume failed: {ErrorMessage}", result.ErrorMessage);
                return ExitCodes.WorkflowExecutionFailed;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Resume operation failed");
            return ExitCodes.InvalidOperation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during workflow resume");
            return ExitCodes.UnexpectedError;
        }
    }
}