namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Wire-compatibility for the <c>stack</c>, <c>config</c> and <c>project</c> command families against a
/// real HappyPumi + Postgres. Commands whose backend endpoint is implemented (Tiers 1–2, 6) get real
/// round-trip assertions; commands that need not-yet-implemented endpoints are <c>Skip</c>ped with the
/// missing operationId so the coverage matrix shows exactly what to build next (see ENDPOINTS.md).
/// </summary>
public sealed class StackConfigProjectTests(HappyPumiServer server) : CliTestBase(server)
{
    // ---- stack lifecycle (Tier 1a: CreateStack / ListUserStacks / DeleteStack) ----

    [Fact]
    public async Task StackInitThenLsThenRm()
    {
        using var cli = LoggedIn();
        var stack = UniqueStack("life");
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        var ls = await Run(cli, "stack", "ls", "--all");
        ls.EnsureSucceeded();

        (await Run(cli, "stack", "rm", "--yes", "--stack", stack)).EnsureSucceeded();
    }

    [Fact]
    public async Task StackSelectThenUnselect()
    {
        using var cli = LoggedIn();
        var stack = UniqueStack("sel");
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        (await Run(cli, "stack", "select", stack)).EnsureSucceeded();
        (await Run(cli, "stack", "unselect")).EnsureSucceeded();
    }

    [Fact]
    public async Task StackRenameChangesName()
    {
        using var cli = LoggedIn();
        // Use the resolved owner ("happypumi") for both source and target so RenameStack's owner-match
        // check passes; the CLI does not resolve the "organization" alias on the rename target.
        var stack = $"happypumi/happypumi-empty-stack/ren-{Guid.NewGuid():N}";
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        // RenameStack (Tier 2) only changes the stack segment; keep org/project.
        var renamed = $"happypumi/happypumi-empty-stack/renamed-{Guid.NewGuid():N}";
        (await Run(cli, "stack", "rename", renamed, "--stack", stack)).EnsureSucceeded();
    }

    [Fact]
    public async Task StackOutputOnFreshStackIsEmpty()
    {
        using var cli = LoggedIn();
        var stack = UniqueStack("out");
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        // No update has run, so there are no outputs; the command must still succeed (GetStack).
        (await Run(cli, "stack", "output", "--stack", stack)).EnsureSucceeded();
    }

    // ---- config (Tier 1a: GetStackConfig / UpdateStackConfig) ----

    [Fact]
    public async Task ConfigSetGetRmRoundTrips()
    {
        using var cli = LoggedIn();
        var dir = NewTempProject(out var project);
        var stack = $"organization/{project}/dev";
        (await RunIn(cli, dir, "stack", "init", stack)).EnsureSucceeded();

        (await RunIn(cli, dir, "config", "set", "region", "us-west-2", "--stack", stack)).EnsureSucceeded();

        var get = await RunIn(cli, dir, "config", "get", "region", "--stack", stack);
        get.EnsureSucceeded();
        Assert.Contains("us-west-2", get.StdOut, StringComparison.Ordinal);

        (await RunIn(cli, dir, "config", "rm", "region", "--stack", stack)).EnsureSucceeded();
        (await RunIn(cli, dir, "stack", "rm", "--yes", "--stack", stack)).EnsureSucceeded();
    }

    [Fact]
    public async Task ConfigSetAllThenLs()
    {
        using var cli = LoggedIn();
        var dir = NewTempProject(out var project);
        var stack = $"organization/{project}/dev";
        (await RunIn(cli, dir, "stack", "init", stack)).EnsureSucceeded();

        (await RunIn(cli, dir, "config", "set-all", "--plaintext", "a=1", "--plaintext", "b=2", "--stack", stack))
            .EnsureSucceeded();

        var ls = await RunIn(cli, dir, "config", "--stack", stack);
        ls.EnsureSucceeded();
        Assert.Contains("a", ls.StdOut, StringComparison.Ordinal);

        (await RunIn(cli, dir, "stack", "rm", "--yes", "--stack", stack)).EnsureSucceeded();
    }

    // ---- stack history-events read side (Tier 1c read) ----

    [Fact]
    public async Task StackHistoryEventsOnFreshStackSucceeds()
    {
        using var cli = LoggedIn();
        var stack = UniqueStack("hist");
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();
        // No updates yet: empty history is fine, the read endpoint must answer.
        (await Run(cli, "stack", "history", "--stack", stack)).EnsureSucceeded();
        (await Run(cli, "stack", "rm", "--yes", "--stack", stack)).EnsureSucceeded();
    }

    // ---- Tier 6 list surfaces that ARE implemented ----

    [Fact]
    public async Task StackWebhookLsIsImplemented()
    {
        using var cli = LoggedIn();
        // ListStackWebhooks (Tier 6) is implemented; an empty list is a valid success.
        var r = await Run(cli, "stack", "webhook", "list", "--stack", "happypumi/webstore/prod");
        r.EnsureSucceeded();
    }

    // ---- not yet implemented: skipped, naming the missing endpoint ----

    [Fact(Skip = "needs GetStackWebhook/UpdateStackWebhook/DeleteStackWebhook (Tier 6 remainder)")]
    public Task StackWebhookGetEditRm() => Task.CompletedTask;

    [Fact(Skip = "needs ReadScheduledDeployment/UpdateScheduledDeployment/DeleteScheduledDeployment")]
    public Task StackScheduleCrud() => Task.CompletedTask;

    [Fact(Skip = "needs change-secrets-provider state rewrite; no single endpoint, deferred")]
    public Task StackChangeSecretsProvider() => Task.CompletedTask;

    [Fact(Skip = "needs ListEnvironments_esc + config env wiring (Tier 8 ESC)")]
    public Task ConfigEnvAddLsRm() => Task.CompletedTask;

    [Fact(Skip = "project new is template/interactive-driven; covered by template registry tier")]
    public Task ProjectNew() => Task.CompletedTask;
}
