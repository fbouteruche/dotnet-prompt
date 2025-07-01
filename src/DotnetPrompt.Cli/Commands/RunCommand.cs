using System.CommandLine;
using DotnetPrompt.Cli.Options;
using DotnetPrompt.Core;
using DotnetPrompt.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Cli.Commands;

/// <summary>
/// Run command implementation
/// </summary>
public class RunCommand : Command
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<RunCommand> _logger;

    public RunCommand(IWorkflowService workflowService, ILogger<RunCommand> logger)
        : base("run", "Execute a workflow file")
    {
        _workflowService = workflowService;
        _logger = logger;

        // Workflow file argument (required)
        var workflowFileArgument = new Argument<string>(
            name: "workflow-file",
            description: "Path to the .prompt.md workflow file");

        // Command options
        var contextOption = new Option<string?>(
            aliases: new[] { "--context" },
            description: "Working directory context (default: current directory)")
        {
            IsRequired = false
        };

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            description: "Validate workflow without execution")
        {
            IsRequired = false
        };

        var timeoutOption = new Option<int?>(
            aliases: new[] { "--timeout" },
            description: "Execution timeout in seconds")
        {
            IsRequired = false
        };

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose" },
            description: "Enable verbose output")
        {
            IsRequired = false
        };

        AddArgument(workflowFileArgument);
        AddOption(contextOption);
        AddOption(dryRunOption);
        AddOption(timeoutOption);
        AddOption(verboseOption);

        this.SetHandler(async (workflowFile, context, dryRun, timeout, verbose) =>
        {
            var options = new RunOptions
            {
                WorkflowFile = workflowFile,
                Context = context,
                DryRun = dryRun,
                Timeout = timeout,
                Verbose = verbose
            };

            var exitCode = await ExecuteAsync(options);
            Environment.Exit(exitCode);
        }, workflowFileArgument, contextOption, dryRunOption, timeoutOption, verboseOption);
    }

    private async Task<int> ExecuteAsync(RunOptions options)
    {
        try
        {
            if (options.Verbose)
            {
                _logger.LogInformation("Verbose mode enabled");
                _logger.LogInformation("Options: {@Options}", options);
            }

            // Apply environment variables
            ApplyEnvironmentVariables(options);

            // Build execution options
            var executionOptions = new WorkflowExecutionOptions(
                Context: options.Context ?? Environment.CurrentDirectory,
                DryRun: options.DryRun,
                Timeout: options.Timeout.HasValue ? TimeSpan.FromSeconds(options.Timeout.Value) : null,
                Verbose: options.Verbose
            );

            _logger.LogInformation("Executing workflow: {WorkflowFile}", options.WorkflowFile);

            var result = await _workflowService.ExecuteAsync(options.WorkflowFile, executionOptions);

            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.Output))
                {
                    Console.WriteLine(result.Output);
                }
                _logger.LogInformation("Workflow completed successfully in {ExecutionTime}", result.ExecutionTime);
                return ExitCodes.Success;
            }
            else
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                }
                _logger.LogError("Workflow execution failed: {ErrorMessage}", result.ErrorMessage);
                return ExitCodes.GeneralError;
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: Workflow file not found - {ex.Message}");
            _logger.LogError(ex, "Workflow file not found");
            return ExitCodes.GeneralError;
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("Error: Workflow execution timed out");
            _logger.LogError("Workflow execution timed out");
            return ExitCodes.ExecutionTimeout;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error during workflow execution");
            return ExitCodes.GeneralError;
        }
    }

    private static void ApplyEnvironmentVariables(RunOptions options)
    {
        // Apply environment variable overrides
        if (!options.Verbose && bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_PROMPT_VERBOSE"), out var verboseEnv))
        {
            options.Verbose = verboseEnv;
        }

        if (!options.Timeout.HasValue && int.TryParse(Environment.GetEnvironmentVariable("DOTNET_PROMPT_TIMEOUT"), out var timeoutEnv))
        {
            options.Timeout = timeoutEnv;
        }
    }
}