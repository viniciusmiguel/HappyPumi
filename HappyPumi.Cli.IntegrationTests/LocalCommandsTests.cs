namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Offline CLI commands that need no Pulumi Cloud backend. They still drive the real pulumi binary
/// (per the conformance requirement) but assert purely local behavior, so they pin down that our
/// build of the CLI is the one under test and document which commands never touch HappyPumi.
/// </summary>
public sealed class LocalCommandsTests(HappyPumiServer server) : CliTestBase(server)
{
    [Fact]
    public async Task VersionPrints()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "version");
        r.EnsureSucceeded();
        Assert.Contains("v", r.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AboutPrintsEnvironment()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "about");
        r.EnsureSucceeded();
        Assert.Contains("Version", r.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenCompletionEmitsBashScript()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "gen-completion", "bash");
        r.EnsureSucceeded();
        Assert.Contains("bash completion for pulumi", r.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PluginLsRunsWithNoPluginsInstalled()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "plugin", "ls");
        r.EnsureSucceeded();
    }

    [Fact]
    public async Task SchemaCheckHelpRuns()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "schema", "check", "--help");
        r.EnsureSucceeded();
        Assert.Contains("schema", r.StdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertHelpRuns()
    {
        using var cli = NewCli();
        var r = await cli.RunAsync(CancellationToken.None, "convert", "--help");
        r.EnsureSucceeded();
        Assert.Contains("Convert", r.StdOut, StringComparison.Ordinal);
    }
}
