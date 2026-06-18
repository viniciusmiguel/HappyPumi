namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Tier 1d wire-compatibility: the preview / refresh / destroy lifecycles, exercised against the real
/// pulumi CLI on the resourceless program (no infra). These share the update-kind lifecycle on the
/// server, differing only by the route's kind segment.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class PreviewDestroyRefreshTests(HappyPumiServer server)
{
    private const string Project = "happypumi-empty-stack";

    private static PulumiCli NewCli() =>
        new(RepoPaths.PulumiBinary ?? throw new InvalidOperationException(
            "pulumi binary not found. Build it with `make pulumi` or set PULUMI_BIN."));

    private async Task<(PulumiCli Cli, string Project)> InitStack(string stackSuffix)
    {
        var cli = NewCli();
        var fixture = RepoPaths.Fixture("empty-stack");
        (await cli.RunAsync(CancellationToken.None, "login", server.BaseUrl)).EnsureSucceeded();
        (await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "stack", "init",
            $"organization/{Project}/{stackSuffix}")).EnsureSucceeded();
        return (cli, fixture);
    }

    [Fact]
    public async Task PreviewReportsChangesWithoutApplying()
    {
        var (cli, fixture) = await InitStack($"preview-{Guid.NewGuid():N}");
        using var _ = cli;

        var preview = await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "preview");

        preview.EnsureSucceeded();
        Assert.Contains("create", preview.StdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshAfterUpSucceeds()
    {
        var (cli, fixture) = await InitStack($"refresh-{Guid.NewGuid():N}");
        using var _ = cli;
        (await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "up", "--yes", "--skip-preview")).EnsureSucceeded();

        var refresh = await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "refresh", "--yes", "--skip-preview");

        refresh.EnsureSucceeded();
    }

    [Fact]
    public async Task DestroyAfterUpTearsDownTheStack()
    {
        var (cli, fixture) = await InitStack($"destroy-{Guid.NewGuid():N}");
        using var _ = cli;
        (await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "up", "--yes", "--skip-preview")).EnsureSucceeded();

        var destroy = await cli.RunAsync(CancellationToken.None, "--cwd", fixture, "destroy", "--yes", "--skip-preview");

        destroy.EnsureSucceeded();
        Assert.Contains("delete", destroy.StdOut, StringComparison.OrdinalIgnoreCase);
    }
}
