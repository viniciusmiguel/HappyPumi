#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.ConsoleApi;

/// <summary>
/// Dev-only mock layer (gated by <c>MockConsole=true</c>) that feeds the prebuilt Pulumi web console
/// permissive data for its internal, non-spec endpoints so every UI surface can be navigated before the
/// real API is wired. Runs before FastEndpoints; intercepts the console's bootstrap/gating calls and
/// returns canned JSON. Reverse-engineered black-box from the running console (ADR-0008); shapes are
/// refined as the console reveals requirements. NOT for production.
/// </summary>
public static class MockConsoleMiddleware
{
    public static IApplicationBuilder UseMockConsole(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path.Value ?? "";

            // Known internal console endpoints: short-circuit with a canned mock.
            var mock = ProjectMock(path) ?? StackMetadataMock(path) ?? StackUpdatesMock(path)
                       ?? OrgDeploymentsMock(path) ?? DeploymentVersionMock(path) ?? DeploymentLogsMock(path)
                       ?? DeploymentUpdatesMock(path) ?? Match(path);
            if (mock is not null && HttpMethods.IsGet(ctx.Request.Method))
            {
                await Write(ctx, mock);
                return;
            }

            // Otherwise let the real endpoint run; if it's an unimplemented stub (throws), serve a mock so
            // the UI keeps working instead of 500-ing. This auto-mocks every not-yet-implemented endpoint.
            try
            {
                await next();
            }
            catch (NotImplementedException)
            {
                if (ctx.Response.HasStarted) throw;
                ctx.Response.Clear();
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                await Write(ctx, Match(path) ?? Default(path));
            }
        });

    private static async Task Write(HttpContext ctx, string json)
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(json);
    }

    /// <summary>
    /// GET /api/console/orgs/{org}/projects/{project} — the project-detail page (`getOrganizationProject`)
    /// reads `resp.project.stacks`. Returns the project with COMPLETE stack objects (every field the stack
    /// lists/cards read) so the page renders fully. Dynamic so any project name works.
    /// </summary>
    private static string? ProjectMock(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, console, orgs, {org}, projects, {project}
        if (parts.Length != 6 || parts[0] != "api" || parts[1] != "console" || parts[2] != "orgs" || parts[4] != "projects")
            return null;
        var org = parts[3];
        var project = parts[5];
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var requestedBy = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" };
        // lastUpdate is an OBJECT (the console reads .version/.result/.endTime/.requestedBy and writes .info).
        object Stack(string name, int rc, long ago, int ver) => new
        {
            orgName = org, projectName = project, stackName = name, name,  // both: lists use .name and .stackName
            resourceCount = rc, version = ver,
            tags = new Dictionary<string, string> { ["pulumi:project"] = project },
            deletedAt = (string?)null,
            lastUpdate = new
            {
                version = ver, result = "succeeded", kind = "update",
                startTime = now - ago - 30, endTime = now - ago, time = now - ago, requestedBy,
            },
        };
        var body = new
        {
            project = new
            {
                orgName = org, name = project,
                stacks = new[] { Stack("dev", 12, 3600, 4), Stack("staging", 18, 7200, 7), Stack("prod", 24, 86400, 11) },
            },
            continuationToken = (string?)null,
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/stacks/{org}/{project}/{stack}/metadata — the stack-detail route's resolver. An empty {} is
    /// treated as "stack not found" (redirects to the project), so return a complete stack metadata object.
    /// </summary>
    private static string? StackMetadataMock(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, stacks, {org}, {project}, {stack}, metadata
        if (parts.Length != 6 || parts[0] != "api" || parts[1] != "stacks" || parts[5] != "metadata")
            return null;
        var (org, project, stack) = (parts[2], parts[3], parts[4]);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var body = new
        {
            orgName = org, projectName = project, stackName = stack, name = stack,
            resourceCount = 12, version = 4, ownedBy = (object?)null, tags = new Dictionary<string, string>(),
            lastUpdate = new
            {
                version = 4, result = "succeeded", kind = "update",
                startTime = now - 3600, endTime = now - 3590, time = now - 3590,
                requestedBy = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" },
            },
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/stacks/{org}/{project}/{stack}/updates — the stack Updates tab. The console reads
    /// <c>update.updateKind</c> (an object passed to getUpdateKindFriendly, which reads <c>.kind</c>), so each
    /// update needs a complete shape incl. <c>updateKind</c>.
    /// </summary>
    private static string? StackUpdatesMock(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, stacks, {org}, {project}, {stack}, updates
        if (parts.Length != 6 || parts[0] != "api" || parts[1] != "stacks" || parts[5] != "updates")
            return null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        object Update(int ver, long ago)
        {
            var by = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" };
            var changes = new { create = 0, update = 0, delete = 0, same = 12 };
            // The console reads most fields off update.info (incl. info.environment, which feeds the VCS info
            // component) — keep it comprehensive. Environment is a tag map (e.g. git.headName, github.pr.number).
            var info = new
            {
                version = ver, kind = "update", message = $"Update {ver}", result = "succeeded",
                startTime = now - ago - 60, endTime = now - ago, requestedBy = by,
                resourceChanges = changes, environment = new Dictionary<string, string>(),
                policyPacks = Array.Empty<object>(), githubCommitInfo = (object?)null,
            };
            return new
            {
                version = ver, updateID = $"u-{ver}", latestVersion = ver, message = $"Update {ver}",
                result = "succeeded", kind = "update", updateKind = new { kind = "update" }, info,
                startTime = now - ago - 60, endTime = now - ago, time = now - ago,
                requestedBy = by, requestedByToken = (object?)null,
                policyPacks = Array.Empty<object>(), githubCommitInfo = (object?)null,
                resourceChanges = changes, resourceCount = 12,
                environment = new Dictionary<string, string>(), config = new { },
            };
        }
        return System.Text.Json.JsonSerializer.Serialize(new { updates = new[] { Update(3, 7200), Update(2, 86400), Update(1, 172800) } });
    }

    /// <summary>
    /// GET /api/orgs/{org}/deployments — the org Deployments page list. Returns complete deployment records
    /// (id/version/status/pulumiOperation/created/requestedBy/jobs) across the seeded stacks so the list and
    /// its status filter render with data instead of an empty state.
    /// </summary>
    private static string? OrgDeploymentsMock(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, orgs, {org}, deployments
        if (parts.Length != 4 || parts[0] != "api" || parts[1] != "orgs" || parts[3] != "deployments")
            return null;
        var org = parts[2];
        var now = DateTimeOffset.UtcNow;
        string Iso(long secsAgo) => now.AddSeconds(-secsAgo).ToString("o");  // deployment times are RFC3339 strings
        var by = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" };
        object Dep(string stack, int ver, long ago, string status, string op) => new
        {
            id = $"dep-{stack}-{ver}", version = ver, latestVersion = ver,
            created = Iso(ago + 120), modified = Iso(ago), status, pulumiOperation = op,
            projectName = "webstore", stackName = stack, orgName = org, requestedBy = by,
            jobs = new[] { new { status, started = Iso(ago + 120), lastUpdated = Iso(ago), steps = Array.Empty<object>() } },
            updates = Array.Empty<object>(),
        };
        var body = new
        {
            deployments = new[]
            {
                Dep("prod", 11, 3600, "succeeded", "update"),
                Dep("staging", 7, 7200, "succeeded", "preview"),
                Dep("dev", 4, 90000, "failed", "update"),
            },
            continuationToken = (string?)null,
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    private static readonly string[] StepNames =
        ["Provision", "Install dependencies", "Run pulumi update", "Collect outputs", "Finalize"];

    /// <summary>
    /// GET /api/stacks/{org}/{project}/{stack}/deployments/version/{v} — the per-deployment detail. Returns a
    /// complete deployment with a job and steps (StepRun) so the detail header + step timeline populate.
    /// </summary>
    private static string? DeploymentVersionMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,stacks,org,project,stack,deployments,version,{v}
        if (p.Length != 8 || p[0] != "api" || p[1] != "stacks" || p[5] != "deployments" || p[6] != "version")
            return null;
        var (org, project, stack, ver) = (p[2], p[3], p[4], int.TryParse(p[7], out var v) ? v : 1);
        var now = DateTimeOffset.UtcNow;
        string Iso(long s) => now.AddSeconds(-s).ToString("o");
        var by = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" };
        var steps = StepNames.Select((n, i) => new
        { name = n, status = "succeeded", started = Iso(120 - i * 20), lastUpdated = Iso(100 - i * 20), isComplete = true });
        var body = new
        {
            id = $"dep-{stack}-{ver}", version = ver, latestVersion = ver,
            created = Iso(3600), modified = Iso(3480), status = "succeeded", pulumiOperation = "update",
            initiator = "happypumi", requestedBy = by,
            jobs = new[] { new { status = "succeeded", started = Iso(3600), lastUpdated = Iso(3480), steps } },
            updates = Array.Empty<object>(),
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/stacks/{org}/{project}/{stack}/deployments/{id}/logs — the deployment log panel. Returns
    /// DeploymentLogs { __type, lines:[{header,line,timestamp}], nextToken }.
    /// </summary>
    private static string? DeploymentLogsMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,stacks,org,project,stack,deployments,{id},logs
        if (p.Length != 8 || p[0] != "api" || p[1] != "stacks" || p[5] != "deployments" || p[7] != "logs")
            return null;
        var now = DateTimeOffset.UtcNow;
        string Iso(int s) => now.AddSeconds(-s).ToString("o");
        var lines = new[]
        {
            new { header = "pulumi", line = "Provisioning deployment environment...", timestamp = Iso(120) },
            new { header = "pulumi", line = "Installing dependencies (go mod download)", timestamp = Iso(100) },
            new { header = "pulumi", line = "Running `pulumi up --yes`", timestamp = Iso(80) },
            new { header = "pulumi", line = "Updating (webstore/prod)", timestamp = Iso(70) },
            new { header = "pulumi", line = "    + 12 created", timestamp = Iso(40) },
            new { header = "pulumi", line = "Resources: 12 unchanged", timestamp = Iso(20) },
            new { header = "pulumi", line = "Update succeeded.", timestamp = Iso(10) },
        };
        return System.Text.Json.JsonSerializer.Serialize(new { __type = "deploymentLogs", lines, nextToken = (string?)null });
    }

    /// <summary>GET /api/stacks/{org}/{project}/{stack}/deployments/{id}/updates — the console spreads this as
    /// an array (the per-deployment stack-update results), so return a bare JSON array.</summary>
    private static string? DeploymentUpdatesMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return p.Length == 8 && p[0] == "api" && p[1] == "stacks" && p[5] == "deployments" && p[7] == "updates"
            ? "[]" : null;
    }

    /// <summary>Best-effort empty shape: list-looking endpoints get a wrapped empty list, else an empty object.</summary>
    private static string Default(string path)
    {
        var last = path.TrimEnd('/').Split('/').LastOrDefault() ?? "";
        // Plural resource collection (e.g. .../members, .../teams, .../deployments) -> wrapped empty arrays.
        if (last.EndsWith('s') && !path.Contains('{'))
            return $"{{\"{last}\":[],\"continuationToken\":null}}";
        return "{}";
    }

    // Exact-path mocks (checked first) — needed where a substring match would be too broad (e.g. "/api/user").
    private static readonly Dictionary<string, string> Exact = new(StringComparer.OrdinalIgnoreCase)
    {
        // The console 404s the org route unless the user lists the org as a membership.
        ["/api/user"] = """
        {"githubLogin":"happypumi","name":"HappyPumi","id":"happypumi","email":"admin@happypumi.dev",
         "avatarUrl":"","hasMFA":false,"identities":[],"siteAdmin":false,
         "organizations":[{"githubLogin":"happypumi","name":"HappyPumi","avatarUrl":"","defaultRepo":""}]}
        """,
    };

    // First match wins. Patterns are matched against the raw path; {org} etc. are just wildcards here.
    private static string? Match(string path)
    {
        if (Exact.TryGetValue(path, out var exact))
            return exact;
        foreach (var (needle, json) in Mocks)
            if (path.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return json;
        if (path.StartsWith("/api/console/", StringComparison.OrdinalIgnoreCase))
            return "{}";
        return null;
    }

    // Ordered: more specific paths first.
    private static readonly List<(string needle, string json)> Mocks =
    [
        // console/orgs/{org}/permissions (GetUserPermissionsForResource) — the console does `new Set(resp)`,
        // so this MUST be a JSON array of permission strings. Return the full set so every UI surface unlocks.
        ("/permissions", AllPermissions),

        // deployments/metadata: in-flight counts (NOT org features — must precede the "/metadata" needle).
        ("/deployments/metadata", """{"running":0,"queued":0}"""),

        // orgs/{org}/metadata — the console reads `r.features.<flag>` and `r.subscriptionStatus`. Enable
        // every feature so all nav surfaces are reachable. (Field set mined from the bundle.)
        ("/metadata", """
        {"subscriptionStatus":"active","features":{
          "aiAgentsEnabled":true,"aleEnabled":true,"crossGuardEnabled":true,"customRolesEnabled":true,
          "dependencyCachingEnabled":true,"deployEnabled":true,"driftDetectionEnabled":true,
          "environmentsEnabled":true,"gitHubEnterpriseIntegrationEnabled":true,"insightsMonetizationEnabled":true,
          "neoPlanModeEnabled":true,"neoServerSideApprovalsEnabled":true,"neoTaskSharingEnabled":true,
          "policyIssueManagementEnabled":true,"resourceSearchEnabled":true,"selfHostedDeploymentsEnabled":true,
          "webhooksEnabled":true,"teamsEnabled":true}}
        """),

        ("/console-settings/favorites", """{"favorites":[]}"""),
        ("/neo/token-budget", """{"remaining":1000000,"limit":1000000,"used":0}"""),
        ("/tags", "[]"),  // org tags collection the stacks page .map()s
        ("/invites", "[]"),                                   // access-management invites list
        ("/repos", """{"repositories":[],"continuationToken":null}"""),          // getOrganizationRepositories
        ("/registry/publishers", "[]"),                                        // idp/registry publishers list
        ("/registry/terraform-modules", """{"terraformModules":[],"continuationToken":null}"""),
        ("/registry/policypacks", """{"policyPacks":[],"continuationToken":null}"""),
        // stack-detail overview: processStackOverviewResponse reads referencedStacks, resources.resources[].resource, tags.
        ("/overview", """{"referencedStacks":[],"resources":{"resources":[]},"tags":{}}"""),
        ("/auditlogs/v2", """{"auditLogEvents":[],"continuationToken":null}"""),
        ("/auditlogs/reader-kind", """{"readerKind":"standard"}"""),
        ("/auditlogs/export/config", """{"enabled":false}"""),
    ];

    // Every "resource:action" permission the console gates UI on (mined from the bundle). Grants everything.
    private const string AllPermissions = """
    ["agent_pool:create","agent_pool:delete","agent_pool:read","agent_pool:update","ai_conversations:create",
     "ai_conversations:list_all","ai_conversations:read","ai_conversations:update","audit_logs:export","audit_logs:read",
     "auth_policies:read","auth_policies:update","change_gate:create","change_gate:delete","change_gate:update",
     "deployments:read","deployments:read_usage","environment:create","environment:delete","environment:list",
     "environment:list_deleted","environment:read","environment:write","environment_access:read","environment_access:update",
     "environment_schedule:create","environment_schedule:delete","environment_schedule:read","environment_schedule:update",
     "environment_settings:read","environment_settings:update","environment_tag:create","environment_tag:delete",
     "environment_tag:read","environment_tag:update","environment_tags:list","environment_version:create",
     "environment_version:delete","environment_version:read","environment_version:update","environment_webhook:create",
     "environment_webhook:delete","environment_webhook:read","environment_webhook:update","github_team:create",
     "insights_account:create","insights_account:delete","insights_account:list","insights_account:read",
     "insights_account:update","insights_account_access:read","insights_account_access:update","insights_account_scan:read",
     "insights_account_scan:update","insights_policy_evaluator:delete","insights_policy_evaluator:read",
     "insights_policy_evaluator:update","insights_policy_queue:read","integrations:read","integrations:update",
     "invites:create","invites:read","oidc_issuers:create","oidc_issuers:delete","oidc_issuers:read","oidc_issuers:update",
     "org_integrations:read","org_integrations:update","org_member:delete","org_member:read","org_member:update",
     "org_member_access:read","org_requests:read","org_requests:update","org_token:create","org_token:delete",
     "org_token:read","organization:delete","organization:read_usage","organization:rename","organization:update",
     "organization_annotations:read","organization_annotations:update","organization_webhook:create",
     "organization_webhook:delete","organization_webhook:read","organization_webhook:update","policy_groups:create",
     "policy_groups:delete","policy_groups:read","policy_groups:update","policy_pack:create","policy_pack:delete",
     "policy_pack:read","policy_pack:update","policy_results:read","project_annotations:read","project_annotations:update",
     "pullrequest:write","repository:admin","repository:write","role:create","role:delete","role:read","role:update",
     "saml:read","saml:update","scim:delete","scim:read","scim:update","services:admin","services:create","services:read",
     "services:write","stack:cancel_update","stack:create","stack:delete","stack:export","stack:import","stack:list",
     "stack:list_deleted","stack:read","stack:rename","stack:transfer","stack:write","stack_access:read",
     "stack_access:update","stack_annotations:read","stack_annotations:update","stack_deployment:create",
     "stack_deployment:read","stack_deployment_cache:read","stack_deployment_settings:read","stack_deployment_settings:write",
     "stack_schedule:create","stack_schedule:delete","stack_schedule:read","stack_schedule:update","stack_tags:update",
     "stack_webhook:create","stack_webhook:delete","stack_webhook:read","stack_webhook:update","tags:read","team:create",
     "team:create_token","team:delete","team:delete_token","team:list","team:list_tokens","team:read","team:update",
     "templates:read","templates_source:create","templates_source:delete","templates_source:read","templates_source:update"]
    """;
}
