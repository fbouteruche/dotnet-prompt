namespace DotnetPrompt.Cli.Options;

/// <summary>
/// Global options available for all commands
/// </summary>
public class GlobalOptions
{
    public bool Help { get; set; }
    public bool Version { get; set; }
    public bool Quiet { get; set; }
    public bool NoColor { get; set; }
    public string? ConfigFile { get; set; }
    public bool Verbose { get; set; }
}

/// <summary>
/// Options specific to the run command
/// </summary>
public class RunOptions : GlobalOptions
{
    public string WorkflowFile { get; set; } = string.Empty;
    public string? Context { get; set; }
    public bool DryRun { get; set; }
    public int? Timeout { get; set; }
}