namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Real-CLI wire-compatibility for the read/query surfaces across Tiers 2–6 (ENDPOINTS.md), driven against
/// the demo data seeded into Postgres (DatabaseSeeder, enabled on the integration server). Each test runs an
/// actual <c>pulumi</c> command and asserts it returns the seeded data — i.e. genuine queries to a real DB.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class SeededDataTests(HappyPumiServer server)
{
    // The seeded org/project/stacks (see DatabaseSeeder).
    private const string SeededStack = "happypumi/webstore/prod";
    private const string SeededOrg = "happypumi";

    private static string Fixture => RepoPaths.Fixture("empty-stack");

    private PulumiCli LoggedIn()
    {
        var cli = new PulumiCli(RepoPaths.PulumiBinary ?? throw new InvalidOperationException("pulumi binary not found."));
        cli.RunAsync(CancellationToken.None, "login", server.BaseUrl).GetAwaiter().GetResult().EnsureSucceeded();
        return cli;
    }

    private async Task<CliResult> Run(PulumiCli cli, params string[] args)
        => await cli.RunAsync(CancellationToken.None, new[] { "--cwd", Fixture }.Concat(args).ToArray());

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
