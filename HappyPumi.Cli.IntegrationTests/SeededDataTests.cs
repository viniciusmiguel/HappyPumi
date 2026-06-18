namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Real-CLI wire-compatibility for the read/query surfaces across Tiers 2–6 (ENDPOINTS.md), driven against
/// the demo data seeded into Postgres (DatabaseSeeder, enabled on the integration server). Each test runs an
/// actual <c>pulumi</c> command and asserts it returns the seeded data — i.e. genuine queries to a real DB.
/// </summary>
public sealed class SeededDataTests(HappyPumiServer server) : CliTestBase(server)
{
    // The seeded org/project/stacks (see DatabaseSeeder).
    private const string SeededStack = "happypumi/webstore/prod";
    private const string SeededOrg = "happypumi";

    [Fact]
    public async Task StackLsShowsSeededStacks()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "stack", "ls", "--all");
        result.EnsureSucceeded();
        Assert.Contains("webstore", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrgMemberListShowsSeededMembers()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "org", "member", "list");
        result.EnsureSucceeded();
        Assert.Contains("alice", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrgRoleListShowsSeededRole()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "org", "role", "list");
        result.EnsureSucceeded();
        Assert.Contains("deployer", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PolicyLsShowsSeededPack()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "policy", "ls", SeededOrg);
        result.EnsureSucceeded();
        Assert.Contains("security", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PolicyGroupLsShowsSeededGroup()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "policy", "group", "ls", SeededOrg);
        result.EnsureSucceeded();
        Assert.Contains("production", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StackHistoryShowsSeededUpdates()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "stack", "history", "--stack", SeededStack);
        result.EnsureSucceeded();
        Assert.Contains("succeeded", result.StdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StackExportReturnsSeededDeployment()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "stack", "export", "--stack", SeededStack);
        result.EnsureSucceeded();
        Assert.Contains("\"version\"", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeploymentSettingsGetReturnsSeededSettings()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "deployment", "settings", "get", "--stack", SeededStack);
        result.EnsureSucceeded();
        Assert.Contains("v1", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeploymentListShowsSeededDeployment()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "deployment", "list", "--stack", SeededStack);
        result.EnsureSucceeded();
        Assert.Contains("update", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StackTagSetThenListRoundTrips()
    {
        using var cli = LoggedIn();
        var stack = $"organization/happypumi-empty-stack/tag-{Guid.NewGuid():N}";
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        (await Run(cli, "stack", "tag", "set", "owner", "platform", "--stack", stack)).EnsureSucceeded();
        var list = await Run(cli, "stack", "tag", "ls", "--stack", stack);

        list.EnsureSucceeded();
        Assert.Contains("owner", list.StdOut, StringComparison.Ordinal);
        Assert.Contains("platform", list.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrgUsageGetSucceeds()
    {
        using var cli = LoggedIn();
        var result = await Run(cli, "org", "usage", "get");
        result.EnsureSucceeded(); // empty window is fine — the point is the endpoint answers the CLI
    }

    [Fact]
    public async Task ExportImportRoundTripCompletes()
    {
        using var cli = LoggedIn();
        var stack = $"organization/happypumi-empty-stack/imp-{Guid.NewGuid():N}";
        (await Run(cli, "stack", "init", stack)).EnsureSucceeded();

        var file = Path.Combine(Path.GetTempPath(), $"hp-export-{Guid.NewGuid():N}.json");
        try
        {
            (await Run(cli, "stack", "export", "--stack", stack, "--file", file)).EnsureSucceeded();
            var import = await Run(cli, "stack", "import", "--stack", stack, "--file", file);
            import.EnsureSucceeded();
            Assert.Contains("Import complete", import.StdOut, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
