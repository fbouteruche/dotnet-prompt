using DotnetPrompt.Cli.Options;
using DotnetPrompt.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetPrompt.UnitTests.Cli;

public class RunOptionsTests
{
    [Fact]
    public void RunOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new RunOptions();

        // Assert
        Assert.Equal(string.Empty, options.WorkflowFile);
        Assert.Null(options.Context);
        Assert.False(options.DryRun);
        Assert.Null(options.Timeout);
        Assert.False(options.Verbose);
        Assert.False(options.Help);
        Assert.False(options.Version);
        Assert.False(options.Quiet);
        Assert.False(options.NoColor);
        Assert.Null(options.ConfigFile);
    }

    [Fact]
    public void GlobalOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new GlobalOptions();

        // Assert
        Assert.False(options.Help);
        Assert.False(options.Version);
        Assert.False(options.Quiet);
        Assert.False(options.NoColor);
        Assert.Null(options.ConfigFile);
        Assert.False(options.Verbose);
    }
}