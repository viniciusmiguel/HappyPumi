namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Tier 2 wire-compatibility: `pulumi stack ls` and `pulumi stack history` against the real CLI after a
/// completed update. Validates the listing and (untyped) history response shapes the CLI parses.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class HistoryListingTests(HappyPumiServer server)
{
    private const string Project = "happypumi-empty-stack";

    private static PulumiCli NewCli() =>
        new(RepoPaths.PulumiBinary ?? throw new InvalidOperationException(
            "pulumi binary not found. Build it with `make pulumi` or set PULUMI_BIN."));

    [Fact]
    public async Task LsAndHistoryReflectACompletedUpdate()
    {
        using var cli = NewCli();
        var fixture = RepoPaths.Fixture("empty-stack");
        var stack = $"hist-{Guid.NewGuid():N}";
        (await cli.RunAsync(CancellationToken.None, "login", server.BaseUrl)).EnsureSucceeded();
        (await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "stack", "init",
            $"organization/{Project}/{stack}")).EnsureSucceeded();
        (await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "up", "--yes", "--skip-preview")).EnsureSucceeded();

        var ls = await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "stack", "ls");
        ls.EnsureSucceeded();
        Assert.Contains(stack, ls.StdOut, StringComparison.Ordinal);

        var history = await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "stack", "history");
        history.EnsureSucceeded();
        Assert.Contains("succeeded", history.StdOut, StringComparison.OrdinalIgnoreCase);
    }
}
