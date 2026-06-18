namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Coverage placeholders for CLI command families whose backend endpoints are NOT yet implemented
/// (ENDPOINTS.md Tier 8: ESC/Environments, Insights, Workflows/agent-pools, VCS, AI/Neo, and the
/// publish/registry-write pipelines). Each is <c>Skip</c>ped with the missing operationId(s) so the
/// suite documents the full command surface and shows exactly what to build to turn each green.
///
/// Convention: when an endpoint lands, move its test into the matching area test class and replace
/// the skip with a real assertion that drives the pulumi binary end-to-end.
/// </summary>
public sealed class Tier8UnimplementedTests(HappyPumiServer server) : CliTestBase(server)
{
    // ---- env / ESC (~60 commands) — ListEnvironments_esc is the only CLI-reachable one (ENDPOINTS.md) ----

    [Fact(Skip = "needs ListEnvironments_esc — Tier 8 ESC")]
    public Task EnvLs() => Task.CompletedTask;

    [Fact(Skip = "needs CreateEnvironment/ReadEnvironment/UpdateEnvironment/DeleteEnvironment (ESC)")]
    public Task EnvInitGetSetRm() => Task.CompletedTask;

    [Fact(Skip = "needs OpenEnvironment + CheckEnvironment (ESC)")]
    public Task EnvOpenRunDiff() => Task.CompletedTask;

    [Fact(Skip = "needs environment version/tag/referrer/rotate/schedule/webhook endpoints (ESC)")]
    public Task EnvVersionTagScheduleWebhook() => Task.CompletedTask;

    [Fact(Skip = "needs aws/azure/gcp-login provider endpoints (ESC + CloudSetup)")]
    public Task EnvProviderLogin() => Task.CompletedTask;

    // ---- insights (~10 commands) ----

    [Fact(Skip = "needs ListAccounts/CreateAccount (Insights)")]
    public Task InsightsAccountListNew() => Task.CompletedTask;

    [Fact(Skip = "needs ScanAccount/GetScan/GetScanLogs (Insights)")]
    public Task InsightsAccountScan() => Task.CompletedTask;

    [Fact(Skip = "needs ReadResource/SearchStacks (Insights)")]
    public Task InsightsResourceGetSearch() => Task.CompletedTask;

    // ---- registry / publish write pipelines (Tier 4 remainder) ----

    [Fact(Skip = "needs PostPublishPackageVersion(+complete) write flow with a package archive (Tier 4)")]
    public Task PackagePublish() => Task.CompletedTask;

    [Fact(Skip = "needs PostPublishTemplateVersion(+complete) write flow (Tier 4)")]
    public Task TemplatePublish() => Task.CompletedTask;

    [Fact(Skip = "template list resolves registry templates; ListTemplates wired but CLI flow needs publish first")]
    public Task TemplateList() => Task.CompletedTask;

    // ---- VCS, agent pools, AI/Neo, raw api passthrough ----

    [Fact(Skip = "needs ListOrgAgentPool/CreateOrgAgentPool (Workflows)")]
    public Task DeploymentAgentPools() => Task.CompletedTask;

    [Fact(Skip = "neo drives AI agent task endpoints (CreateTasks/StreamTaskEvents) — Tier 8 AI")]
    public Task Neo() => Task.CompletedTask;

    [Fact(Skip = "`pulumi do` interacts directly with cloud resources; out of scope for HappyPumi")]
    public Task Do() => Task.CompletedTask;

    [Fact(Skip = "`pulumi api` raw passthrough — exercised indirectly by every other endpoint test")]
    public Task ApiPassthrough() => Task.CompletedTask;

    // ---- stack drift (Tier 6) — depends on deployment drift runs being produced ----

    [Fact(Skip = "drift status/list need GetStackDriftStatus/ListDriftRuns to be populated by a drift run")]
    public Task StackDrift() => Task.CompletedTask;

    // ---- state mutation subtree — local state edits over export/import; no dedicated endpoints ----

    [Fact(Skip = "state move/rename/protect/taint operate on exported state; needs a resourceful program")]
    public Task StateMutations() => Task.CompletedTask;

    // ---- logs / watch — need a resourceful, long-running program ----

    [Fact(Skip = "logs/watch need a provisioned, log-emitting program; not exercisable with empty-stack")]
    public Task LogsWatch() => Task.CompletedTask;
}
