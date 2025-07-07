using System.Diagnostics;
using System.Text;

namespace DotnetPrompt.IntegrationTests;

public class CliIntegrationTests
{
    private readonly string _cliPath;

    public CliIntegrationTests()
    {
        // Path to the CLI executable
        _cliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DotnetPrompt.Cli.dll");
    }

    [Fact]
    public async Task CLI_Help_DisplaysCorrectInformation()
    {
        // Act
        var result = await RunCliAsync("--help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("A powerful CLI tool for .NET developers", result.Output);
        Assert.Contains("run <workflow-file>", result.Output);
        Assert.Contains("--quiet", result.Output);
    }

    [Fact]
    public async Task CLI_Version_DisplaysVersionInformation()
    {
        // Act
        var result = await RunCliAsync("--version");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"\d+\.\d+\.\d+", result.Output);
    }

    [Fact]
    public async Task CLI_RunHelp_DisplaysRunCommandOptions()
    {
        // Act
        var result = await RunCliAsync("run --help");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Execute a workflow file", result.Output);
        Assert.Contains("--context", result.Output);
        Assert.Contains("--dry-run", result.Output);
        Assert.Contains("--timeout", result.Output);
        Assert.Contains("--verbose", result.Output);
    }

    [Fact]
    public async Task CLI_RunNonExistentFile_ReturnsError()
    {
        // Act
        var result = await RunCliAsync("run non-existent.prompt.md");

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task CLI_RunValidFile_DryRun_Succeeds()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var workflowFile = Path.ChangeExtension(tempFile, ".prompt.md");
        await File.WriteAllTextAsync(workflowFile, "---\nname: test-workflow\nmodel: gpt-4o\n---\n# Test workflow\n\nThis is a valid test workflow.");

        // Set environment variable to use local provider for integration testing (doesn't require credentials)
        Environment.SetEnvironmentVariable("DOTNET_PROMPT_PROVIDER", "local");

        try
        {
            // Act
            var result = await RunCliAsync($"run \"{workflowFile}\" --dry-run");

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Dry run completed", result.Output);
        }
        finally
        {
            if (File.Exists(workflowFile))
                File.Delete(workflowFile);
            
            // Clean up environment variable
            Environment.SetEnvironmentVariable("DOTNET_PROMPT_PROVIDER", null);
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

    private record CliResult(int ExitCode, string Output, string Error);
}