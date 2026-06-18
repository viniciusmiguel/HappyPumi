namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Tier 1c (update lifecycle) wire-compatibility, exercised WITHOUT real infra: `pulumi up` on the
/// resourceless Go program in fixtures/empty-stack still drives the full backend lifecycle
/// (CreateUpdate -> StartUpdate -> checkpoints -> complete) — it just provisions nothing.
///
/// Skipped until those endpoints exist in HappyPumi (see ENDPOINTS.md Tier 1). The CLI and the Go
/// language host are already built locally by tools/build-pulumi-cli.sh, so enabling these is purely
/// a matter of implementing the server side.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class UpdateLifecycleTests(HappyPumiServer server)
{
    private const string PendingTier1 =
        "Pending HappyPumi Tier 1 stack/config + update-lifecycle endpoints (see ENDPOINTS.md). " +
        "Harness (CLI + Go language host + resourceless program) is ready; only the server side is missing.";

    private static PulumiCli NewCli() =>
        new(RepoPaths.PulumiBinary ?? throw new InvalidOperationException(
            "pulumi binary not found. Build it with `make pulumi` or set PULUMI_BIN."));

    [Fact(Skip = PendingTier1)]
    public async Task UpOnResourcelessProgramCompletesAnUpdate()
    {
        using var cli = NewCli();
        var project = RepoPaths.Fixture("empty-stack");

        (await cli.RunAsync(CancellationToken.None, "login", server.BaseUrl)).EnsureSucceeded();
        (await cli.RunAsync(CancellationToken.None, "--cwd", project, "stack", "init", "organization/happypumi-empty-stack/dev")).EnsureSucceeded();

        var up = await cli.RunAsync(CancellationToken.None, "--cwd", project, "up", "--yes", "--skip-preview");

        up.EnsureSucceeded();
        Assert.Contains("ok", up.StdOut, StringComparison.OrdinalIgnoreCase);
    }
}
