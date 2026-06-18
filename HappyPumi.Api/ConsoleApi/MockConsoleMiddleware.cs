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
            var mock = Match(path);
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
        ("/projects/services", """{"services":[],"continuationToken":null}"""),  // listServices: pipe maps e.services
        ("/repos", """{"repositories":[],"continuationToken":null}"""),          // getOrganizationRepositories
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
