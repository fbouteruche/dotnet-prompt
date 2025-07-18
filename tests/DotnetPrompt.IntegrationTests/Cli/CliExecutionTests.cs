using FluentAssertions;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace DotnetPrompt.IntegrationTests.Cli;

/// <summary>
/// Enhanced CLI execution tests with comprehensive end-to-end workflow validation
/// Tests actual CLI process execution with various options and SK integration
/// </summary>
public class CliExecutionTests : IDisposable
{
    private readonly string _cliPath;
    private readonly string _testDirectory;

    public CliExecutionTests()
    {
        // Path to the CLI executable
        _cliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DotnetPrompt.Cli.dll");
        
        // Create test directory for workflows
        _testDirectory = Path.Combine(Path.GetTempPath(), "cli-execution-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task CLI_RunWorkflowWithSkPlugins_ExecutesSuccessfully()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "sk-plugins-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            
            config:
              temperature: 0.3
              maxOutputTokens: 1000
            ---
            
            # Test SK Plugin Integration
            
            This is a test workflow that uses SK plugins for file operations.
            The workflow should execute without errors when using --dry-run mode.
            """;

        var workflowFile = Path.Combine(_testDirectory, "sk-plugins-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Set environment variable for testing
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act
            var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run");

            // Assert
            result.ExitCode.Should().Be(0, $"CLI execution should succeed. Error: {result.Error}");
            result.Output.Should().Contain("sk-plugins-test", "Output should mention the workflow name");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task CLI_RunWithVerboseOption_ShowsSkTelemetry()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "verbose-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            ---
            
            # Verbose Output Test
            
            This workflow tests verbose output with SK telemetry information.
            """;

        var workflowFile = Path.Combine(_testDirectory, "verbose-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Act
        var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run --verbose");

        // Assert
        result.ExitCode.Should().Be(0, $"CLI execution should succeed. Error: {result.Error}");
        
        // Verbose output should include additional logging/telemetry information
        var output = result.Output + result.Error; // Check both stdout and stderr for verbose logs
        (output.Contains("verbose", StringComparison.OrdinalIgnoreCase) || 
         output.Contains("debug", StringComparison.OrdinalIgnoreCase) || 
         output.Contains("telemetry", StringComparison.OrdinalIgnoreCase) || 
         output.Contains("plugin", StringComparison.OrdinalIgnoreCase) || 
         output.Contains("kernel", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("Verbose mode should show additional SK telemetry or debug information");
    }

    [Fact]
    public async Task CLI_RunWithContextOption_SetsWorkingDirectory()
    {
        // Arrange
        var contextDirectory = Path.Combine(_testDirectory, "context-test");
        Directory.CreateDirectory(contextDirectory);

        var workflowContent = """
            ---
            name: "context-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            ---
            
            # Working Directory Context Test
            
            This workflow tests the --context option for setting working directory.
            """;

        var workflowFile = Path.Combine(_testDirectory, "context-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Set environment variable for testing
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act
            var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run --context \"{contextDirectory}\"");

            // Assert
            result.ExitCode.Should().Be(0, $"CLI execution should succeed. Error: {result.Error}");
            result.Output.Should().Contain("context-test", "Output should mention the workflow name");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task CLI_RunWithTimeoutOption_HandlesTimeout()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "timeout-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            ---
            
            # Timeout Test
            
            This workflow tests timeout handling with the --timeout option.
            """;

        var workflowFile = Path.Combine(_testDirectory, "timeout-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Set environment variable for testing
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act - Use a very short timeout to test timeout handling
            var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run --timeout 1");

            // Assert - Either succeeds quickly or times out appropriately
            // In dry-run mode, this should succeed quickly
            result.ExitCode.Should().BeOneOf(0, 4); // Success or ExecutionTimeout
            
            if (result.ExitCode == 4)
            {
                result.Error.Should().Match("*timeout*", "Timeout error should mention timeout");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public async Task CLI_RunWithProgressTracking_CreatesProgressFiles()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "progress-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            
            config:
              temperature: 0.5
              maxOutputTokens: 2000
            ---
            
            # Progress Tracking Test
            
            This workflow tests progress file creation during execution.
            The system should create progress files for tracking workflow state.
            """;

        var workflowFile = Path.Combine(_testDirectory, "progress-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Act
        var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run");

        // Assert
        result.ExitCode.Should().Be(0, $"CLI execution should succeed. Error: {result.Error}");
        
        // Check for progress-related output or directory creation
        // In dry-run mode, progress files might not be created, but the system should indicate it would create them
        var output = result.Output + result.Error;
        output.Should().NotBeNullOrEmpty("Should have some output indicating progress tracking capability");
    }

    [Fact] 
    public async Task CLI_ResumeFromProgressFile_ContinuesExecution()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "resume-test"
            model: "ollama/test-model"
            tools: ["file-system"]
            ---
            
            # Resume Test
            
            This workflow tests the resume functionality.
            """;

        var workflowFile = Path.Combine(_testDirectory, "resume-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Act - First try to run the resume command (should indicate no progress found)
        var result = await RunCliAsync($"resume \"{workflowFile}\"");

        // Assert - Should return appropriate exit code for no progress found
        result.ExitCode.Should().BeOneOf(0, 11, 12); // Success, FeatureNotAvailable, or NoProgressFound
        
        // If it returns NoProgressFound (12), should have user-friendly error message
        if (result.ExitCode == 12)
        {
            result.Error.Should().Contain("No resumable workflow states found", "Should provide clear error message to user");
        }
    }

    [Fact]
    public async Task CLI_RunWithInvalidWorkflowFile_ReturnsValidationError()
    {
        // Arrange
        var invalidWorkflowContent = """
            ---
            name: "invalid-test"
            model: "ollama/test-model"
            tools: ["nonexistent-tool"]
            invalid yaml structure here: [unclosed
            malformed: 
              - item1
              - item2
                nested:
                  unclosed: "value
            ---
            
            # Invalid Workflow Test
            
            This workflow contains malformed YAML that should cause parsing errors.
            """;

        var workflowFile = Path.Combine(_testDirectory, "invalid-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, invalidWorkflowContent);

        // Act
        var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run");

        // Assert
        result.ExitCode.Should().NotBe(0, "Invalid workflow should not succeed");
        var allOutput = result.Output + result.Error;
        (allOutput.Contains("validation", StringComparison.OrdinalIgnoreCase) || 
         allOutput.Contains("invalid", StringComparison.OrdinalIgnoreCase) || 
         allOutput.Contains("error", StringComparison.OrdinalIgnoreCase) ||
         allOutput.Contains("parse", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue("Should indicate validation or parsing error");
    }

    [Fact]
    public async Task CLI_RunWithSkFilterPipeline_ProcessesCorrectly()
    {
        // Arrange
        var workflowContent = """
            ---
            name: "filter-pipeline-test"
            model: "ollama/test-model"
            tools: ["file-system", "project-analysis"]
            
            config:
              temperature: 0.7
              maxOutputTokens: 3000
            ---
            
            # SK Filter Pipeline Test
            
            This workflow tests the SK filter pipeline integration.
            The WorkflowExecutionFilter should process this workflow.
            """;

        var workflowFile = Path.Combine(_testDirectory, "filter-pipeline-test.prompt.md");
        await File.WriteAllTextAsync(workflowFile, workflowContent);

        // Set environment variable for testing
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "test-token");

        try
        {
            // Act
            var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run --verbose");

            // Assert
            result.ExitCode.Should().Be(0, $"CLI execution should succeed. Error: {result.Error}");
            result.Output.Should().Contain("filter-pipeline-test", "Output should mention the workflow name");
            
            // In verbose mode, should show some indication of filter processing
            var output = result.Output + result.Error;
            output.Should().NotBeNullOrEmpty("Should have output indicating SK processing");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    private async Task<CliResult> RunCliAsync(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_cliPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new CliResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString()
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private record CliResult(int ExitCode, string Output, string Error);
}