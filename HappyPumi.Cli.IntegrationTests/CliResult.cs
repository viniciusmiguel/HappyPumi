namespace HappyPumi.Cli.IntegrationTests;

/// <summary>Outcome of a single pulumi CLI invocation.</summary>
/// <param name="Command">The full command line that was run (for assertion messages).</param>
/// <param name="ExitCode">Process exit code; 0 is success.</param>
/// <param name="StdOut">Captured standard output.</param>
/// <param name="StdErr">Captured standard error.</param>
public sealed record CliResult(string Command, int ExitCode, string StdOut, string StdErr)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>Asserts the command exited 0, surfacing the command and both streams on failure.</summary>
    public CliResult EnsureSucceeded()
    {
        if (Succeeded)
            return this;

        throw new Xunit.Sdk.XunitException(
            $"'{Command}' exited {ExitCode}.\n--- stdout ---\n{StdOut}\n--- stderr ---\n{StdErr}");
    }
}
