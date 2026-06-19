namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Wire-compatibility for the <c>org</c>, <c>policy</c> and <c>deployment</c> command families (Tiers 3, 5, 6)
/// against real HappyPumi + Postgres. Read/list and round-trip commands backed by implemented endpoints get
/// real assertions; commands needing not-yet-implemented endpoints are <c>Skip</c>ped with the missing
/// operationId. The default org resolves to the seeded <c>happypumi</c> org (DatabaseSeeder).
/// </summary>
public sealed class OrgPolicyDeploymentTests(HappyPumiServer server) : CliTestBase(server)
{
    private const string Org = "happypumi";
    private const string SeededStack = "happypumi/webstore/prod";

    // ---- org (Tier 0/3) ----

    [Fact]
    public async Task OrgGetDefaultReturnsAnOrg()
    {
        using var cli = LoggedIn();
        var r = await Run(cli, "org", "get-default");
        r.EnsureSucceeded();
        Assert.Contains(Org, r.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OrgAuditLogListSucceeds()
    {
        using var cli = LoggedIn();
        // ListAuditLogEventsHandlerV1 (Tier 3); an empty window is a valid success.
        var r = await Run(cli, "org", "audit-log", "list", "--org", Org);
        r.EnsureSucceeded();
    }

    [Fact]
    public async Task OrgMemberEditUpdatesRole()
    {
        using var cli = LoggedIn();
        // UpdateOrganizationMember (Tier 3) against the seeded member "alice".
        var r = await Run(cli, "org", "member", "edit", "alice", "--role", "member");
        r.EnsureSucceeded();
    }

    // ---- org search (Tier 7) ----
    // GetOrgResourceSearchQuery / GetNaturalLanguageQuery ARE implemented (Tier 7) and covered by the
    // component tests, but the pulumi CLI guards `org search` client-side: it refuses individual
    // accounts ("X is an individual account, not an organization") before issuing the HTTP call. The
    // seeded principal's default org equals its own login, so the CLI classifies it as individual.
    // Exercising these via the CLI needs an identity where the user is a member of a *distinct* org.

    [Fact(Skip = "CLI refuses org search for individual accounts; endpoint (GetOrgResourceSearchQuery) is implemented + component-tested")]
    public Task OrgSearchResources() => Task.CompletedTask;

    [Fact(Skip = "CLI refuses org search for individual accounts; endpoint (GetNaturalLanguageQuery) is implemented + component-tested")]
    public Task OrgSearchAi() => Task.CompletedTask;

    // ---- policy (Tier 5) ----

    [Fact]
    public async Task PolicyComplianceListSucceeds()
    {
        using var cli = LoggedIn();
        // GetPolicyComplianceResults (Tier 5b).
        var r = await Run(cli, "policy", "compliance", "list", "--org", Org);
        r.EnsureSucceeded();
    }

    [Fact]
    public async Task PolicyIssueListSucceeds()
    {
        using var cli = LoggedIn();
        // ListPolicyIssues (Tier 5b).
        var r = await Run(cli, "policy", "issue", "list", "--org", Org);
        r.EnsureSucceeded();
    }

    [Fact]
    public async Task PolicyGroupNewThenLsShowsIt()
    {
        using var cli = LoggedIn();
        var name = "grp-" + Guid.NewGuid().ToString("N")[..8];
        // NewPolicyGroup (Tier 5a). --entity-type is required by the CLI.
        (await Run(cli, "policy", "group", "new", name, "--org", Org, "--entity-type", "stacks", "--yes"))
            .EnsureSucceeded();

        var ls = await Run(cli, "policy", "group", "ls", Org);
        ls.EnsureSucceeded();
        Assert.Contains(name, ls.StdOut, StringComparison.Ordinal);
    }

    // ---- deployment (Tier 6) ----

    [Fact]
    public async Task DeploymentListSucceeds()
    {
        using var cli = LoggedIn();
        // ListStackDeploymentsHandlerV2 (Tier 6).
        var r = await Run(cli, "deployment", "list", "--stack", SeededStack);
        r.EnsureSucceeded();
    }

    // ---- not yet implemented: skipped with the missing endpoint ----

    [Fact(Skip = "needs UpdateDefaultOrganization (Users) — Tier 8")]
    public Task OrgSetDefault() => Task.CompletedTask;

    [Fact(Skip = "needs ExportAuditLogEventsHandlerV1 — Tier 8")]
    public Task OrgAuditLogExport() => Task.CompletedTask;

    [Fact(Skip = "needs CreateOrganizationWebhook/ListOrganizationWebhooks/... — Tier 8")]
    public Task OrgWebhookCrud() => Task.CompletedTask;

    [Fact(Skip = "needs CreateRole + a team (CreatePulumiTeam) to assign to — Tier 3/8 remainder")]
    public Task OrgRoleNewAndAssign() => Task.CompletedTask;

    [Fact(Skip = "policy enable/disable apply a pack to a stack; no implemented endpoint yet")]
    public Task PolicyEnableDisable() => Task.CompletedTask;

    [Fact(Skip = "policy new/publish/install drive the registry publish pipeline (Tier 4 remainder)")]
    public Task PolicyPublishLifecycle() => Task.CompletedTask;

    [Fact(Skip = "needs GetDeployment — Tier 6 remainder")]
    public Task DeploymentGet() => Task.CompletedTask;

    [Fact(Skip = "needs GetDeploymentLogs — Tier 6 remainder")]
    public Task DeploymentLog() => Task.CompletedTask;

    [Fact(Skip = "deployment run needs a git source + CreateAPIDeploymentHandlerV2 execution backend")]
    public Task DeploymentRun() => Task.CompletedTask;
}
