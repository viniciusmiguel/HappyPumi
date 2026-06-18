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

            // Some endpoints are fetched as raw text, not JSON (README markdown, ESC environment YAML), so
            // serve them before the JSON chain with the right content type.
            var readme = PackageReadmeMock(path);
            if (readme is not null && HttpMethods.IsGet(ctx.Request.Method))
            {
                await WriteText(ctx, readme, "text/markdown; charset=utf-8");
                return;
            }
            var yaml = EscEnvironmentYaml(path);
            if (yaml is not null && HttpMethods.IsGet(ctx.Request.Method))
            {
                await WriteText(ctx, yaml, "application/x-yaml; charset=utf-8");
                return;
            }

            // The ESC editor preview POSTs the YAML to .../yaml/check and renders the evaluated result; the
            // real endpoint 400s, so intercept the POST and return a resolved environment.
            if (HttpMethods.IsPost(ctx.Request.Method))
            {
                var checkResult = EscCheckYamlMock(path);
                if (checkResult is not null)
                {
                    await Write(ctx, checkResult);
                    return;
                }
            }

            // Known internal console endpoints: short-circuit with a canned mock.
            var mock = ProjectMock(path) ?? StackMetadataMock(path) ?? StackUpdatesMock(path)
                       ?? PackageVersionsMock(path)
                       ?? PackageNavMock(path) ?? OrgEnvironmentsMock(path) ?? EscEnvironmentMock(path) ?? Match(path);
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

    private static async Task WriteText(HttpContext ctx, string text, string contentType)
    {
        ctx.Response.ContentType = contentType;
        await ctx.Response.WriteAsync(text);
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
    /// GET /api/registry/packages/{source}/{publisher}/{name}/versions — the Private Components "Versions" tab
    /// and the version selector (ListPackageVersions). Not in the public spec (console-only). The console reads
    /// <c>resp.packages</c> as the version list and each item's <c>.version</c>/<c>.isLatest</c>. Returns a
    /// couple of versions with the newest flagged latest so the selector + table render.
    /// </summary>
    private static string? PackageVersionsMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,registry,packages,source,publisher,name,versions
        if (p.Length != 7 || p[0] != "api" || p[1] != "registry" || p[2] != "packages" || p[6] != "versions")
            return null;
        var (source, publisher, name) = (p[3], p[4], p[5]);
        var now = DateTimeOffset.UtcNow;
        object Ver(string version, bool isLatest, long agoSecs) => new
        {
            source, publisher, name, version, isLatest,
            createdAt = now.AddSeconds(-agoSecs).ToString("o"),
            description = $"The {name} component.", packageStatus = "published", isFeatured = false,
            readmeURL = $"/api/registry/packages/{source}/{publisher}/{name}/versions/{version}/readme",
            schemaURL = $"/api/registry/packages/{source}/{publisher}/{name}/versions/{version}/schema",
            repoUrl = (string?)null, logoUrl = (string?)null, pluginDownloadURL = (string?)null,
        };
        var body = new
        {
            packages = new[] { Ver("1.0.0", true, 3600), Ver("0.9.0", false, 604800) },
            continuationToken = (string?)null,
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/console/registry/packages/{source}/{publisher}/{name}/versions/{version}/nav — the component
    /// "API Docs" tab. The console's <c>constructTree</c> iterates <c>resp.modules</c> and reads each module/
    /// resource/function's <c>name["go"]</c> (a per-language name MAP, not a string) plus <c>typeToken</c>.
    /// Returns one module ("index") with a resource and a function so the docs navigation tree renders.
    /// </summary>
    private static string? PackageNavMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,console,registry,packages,src,pub,name,versions,{ver},nav
        if (p.Length != 10 || p[0] != "api" || p[1] != "console" || p[2] != "registry" || p[3] != "packages"
            || p[7] != "versions" || p[9] != "nav")
            return null;
        var (source, publisher, name, version) = (p[4], p[5], p[6], p[8]);
        // name is a language-keyed map (the console reads .["go"]); typeToken is "{pkg}:{module}:{Type}".
        object LangName(string go) => new { go, nodejs = go, python = go, dotnet = go };
        var module = new
        {
            name = LangName("index"),
            resources = new[] { new { name = LangName("Widget"), typeToken = $"{name}:index:Widget" } },
            functions = new[] { new { name = LangName("getWidget"), typeToken = $"{name}:index:getWidget" } },
            resourcesTotal = 1, functionsTotal = 1,
        };
        var basePath = $"/api/registry/packages/{source}/{publisher}/{name}/versions/{version}";
        var body = new
        {
            source, publisher, name, version, title = name, description = $"The {name} component.",
            language = "go", modules = new[] { module }, modulesTotal = 1,
            packageVersionUrl = basePath, readmeUrl = $"{basePath}/readme",
            docsUrlTemplate = $"{basePath}/docs/{{token}}",
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/registry/packages/{source}/{publisher}/{name}/versions/{version}/readme — the component
    /// Overview README. The console fetches this with <c>responseType:"text"</c> and feeds it to a markdown
    /// renderer, so return raw markdown (not JSON). Returns null for non-readme paths.
    /// </summary>
    private static string? PackageReadmeMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,registry,packages,source,pub,name,versions,{ver},readme
        if (p.Length != 9 || p[0] != "api" || p[1] != "registry" || p[2] != "packages" || p[6] != "versions" || p[8] != "readme")
            return null;
        var (publisher, name, version) = (p[4], p[5], p[7]);
        return $$"""
        # {{name}}

        A reusable Pulumi component published to the **{{publisher}}** private registry (version `{{version}}`).

        Components bundle best practices and sensible defaults so teams can compose infrastructure from
        higher-level building blocks instead of wiring primitives by hand.

        ## Installation

        ```bash
        pulumi package add {{publisher}}/{{name}}@{{version}}
        ```

        ## Example

        ```typescript
        import * as {{name}} from "@{{publisher}}/{{name}}";

        const widget = new {{name}}.Widget("my-widget", {
            size: "large",
        });

        export const widgetId = widget.id;
        ```

        ## Inputs

        | Name | Type | Description |
        | ---- | ---- | ----------- |
        | `size` | `string` | The widget size. Defaults to `medium`. |

        ## Outputs

        | Name | Type | Description |
        | ---- | ---- | ----------- |
        | `id` | `string` | The provisioned widget identifier. |
        """;
    }

    /// <summary>
    /// GET /api/esc/environments/{org} — the ESC Environments list (listOrgEnvironments). Returns
    /// {environments:[OrgEnvironment], nextToken}; the console builds each row name as "{project}/{name}" and
    /// groups by project. Returns a realistic set across two projects so the grouped grid renders.
    /// </summary>
    private static string? OrgEnvironmentsMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, esc, environments, {org}
        if (p.Length != 4 || p[0] != "api" || p[1] != "esc" || p[2] != "environments")
            return null;
        var org = p[3];
        var now = DateTimeOffset.UtcNow;
        string Iso(long days) => now.AddDays(-days).ToString("o");
        var by = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "" };
        object Env(string project, string name, long created, long modified) => new
        {
            id = $"{org}/{project}/{name}", organization = org, project, name,
            created = Iso(created), modified = Iso(modified), deletedAt = (string?)null,
            ownedBy = by, tags = new Dictionary<string, string>(),
            settings = new { deletionProtected = false },
        };
        var body = new
        {
            environments = new[]
            {
                Env("webstore", "prod", 90, 1),
                Env("webstore", "staging", 60, 2),
                Env("webstore", "dev", 45, 0),
                Env("platform", "shared-secrets", 120, 7),
            },
            nextToken = (string?)null,
        };
        return System.Text.Json.JsonSerializer.Serialize(body);
    }

    /// <summary>
    /// GET /api/esc/environments/{org}/{project}/{name}[/versions/{version}] — ReadEnvironment returns the raw
    /// environment definition as <c>application/x-yaml</c> (the editor loads it as text). Returns a realistic
    /// ESC definition (imports, values, secrets, environmentVariables, pulumiConfig). Non-env paths return null.
    /// </summary>
    private static string? EscEnvironmentYaml(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,esc,environments,org,proj,name[,versions,ver]
        var isBase = p.Length == 6 && p[0] == "api" && p[1] == "esc" && p[2] == "environments";
        var isVersion = p.Length == 8 && p[0] == "api" && p[1] == "esc" && p[2] == "environments"
                        && p[6] == "versions" && p[7] != "tags";
        if (!isBase && !isVersion)
            return null;
        return """
        values:
          aws:
            region: us-west-2
          app:
            name: webstore
            replicas: 3
          environmentVariables:
            AWS_REGION: ${aws.region}
            APP_NAME: ${app.name}
            DATABASE_URL:
              fn::secret:
                ciphertext: ZXNjeAAAAAE... (encrypted)
          pulumiConfig:
            aws:region: ${aws.region}
            webstore:replicas: ${app.replicas}
        """;
    }

    /// <summary>
    /// JSON sub-resources of the ESC environment detail page: metadata, settings, revisions (versions), and
    /// revision tags. ListEnvironmentRevisions returns a BARE ARRAY (the console calls <c>.sort()</c> on it).
    /// </summary>
    private static string? EscEnvironmentMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,esc,environments,org,proj,name,<sub>...
        if (p.Length < 7 || p[0] != "api" || p[1] != "esc" || p[2] != "environments")
            return null;
        var (org, project, name) = (p[3], p[4], p[5]);
        var now = DateTimeOffset.UtcNow;
        string Iso(long days) => now.AddDays(-days).ToString("o");
        var by = new { githubLogin = "happypumi", name = "HappyPumi", avatarUrl = "", email = "admin@happypumi.dev" };

        // .../versions/tags
        if (p.Length == 8 && p[6] == "versions" && p[7] == "tags")
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                tags = new[]
                {
                    new { name = "latest", revision = 3, created = Iso(0), modified = Iso(0), editorLogin = "happypumi", editorName = "HappyPumi" },
                    new { name = "stable", revision = 2, created = Iso(2), modified = Iso(2), editorLogin = "happypumi", editorName = "HappyPumi" },
                },
                nextToken = (string?)null,
            });

        if (p.Length != 7) return null;
        switch (p[6])
        {
            case "versions": // ListEnvironmentRevisions — BARE ARRAY (console .sort()s it)
                object Rev(int number, long days, string[] tags) => new
                { number, created = Iso(days), creatorLogin = "happypumi", creatorName = "HappyPumi", tags };
                return System.Text.Json.JsonSerializer.Serialize(new[]
                { Rev(3, 0, ["latest"]), Rev(2, 2, ["stable"]), Rev(1, 30, []) });
            case "metadata":
                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    id = $"{org}/{project}/{name}", openRequestNeeded = false,
                    gatedActions = Array.Empty<string>(), ownedBy = by, activeChangeRequest = (object?)null,
                });
            case "settings":
                return System.Text.Json.JsonSerializer.Serialize(new { deletionProtected = false });
            case "tags": // ListEnvironmentTags (environment-level tag map)
                return System.Text.Json.JsonSerializer.Serialize(new { tags = new Dictionary<string, object>() });
            case "schedules":
                return System.Text.Json.JsonSerializer.Serialize(new { schedules = Array.Empty<object>(), nextToken = (string?)null });
            case "referrers":
                return System.Text.Json.JsonSerializer.Serialize(new { referrers = Array.Empty<object>(), nextToken = (string?)null });
            default:
                return null;
        }
    }

    /// <summary>
    /// POST /api/esc/environments/{org}/yaml/check — the ESC editor's live evaluator. The "Environment preview"
    /// panel renders the response's <c>properties</c> (a tree of EscValue {value, secret?}) plus <c>schema</c>.
    /// Returns the resolved value of the sample definition EscEnvironmentYaml produces. Non-check paths return null.
    /// </summary>
    private static string? EscCheckYamlMock(string path)
    {
        var p = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api,esc,environments,{org},yaml,check
        if (p.Length != 6 || p[0] != "api" || p[1] != "esc" || p[2] != "environments" || p[4] != "yaml" || p[5] != "check")
            return null;
        object Val(object v) => new { value = v };
        object Secret(object v) => new { value = v, secret = true };
        var properties = new Dictionary<string, object>
        {
            ["aws"] = Val(new { region = Val("us-west-2") }),
            ["app"] = Val(new { name = Val("webstore"), replicas = Val(3) }),
            ["environmentVariables"] = Val(new
            {
                AWS_REGION = Val("us-west-2"), APP_NAME = Val("webstore"),
                DATABASE_URL = Secret("postgres://webstore:****@db.internal:5432/webstore"),
            }),
            ["pulumiConfig"] = Val(new Dictionary<string, object>
            {
                ["aws:region"] = Val("us-west-2"), ["webstore:replicas"] = Val(3),
            }),
        };
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            properties, schema = new { type = "object" }, diagnostics = Array.Empty<object>(),
        });
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

        // ESC dynamic-config provider/rotator catalogs (the env editor's "add provider" pickers).
        ("/esc/providers", """{"providers":["aws-login","aws-secrets","gcp-login","gcp-secrets","azure-login","vault-login","1password","pulumi-stacks"]}"""),
        ("/esc/rotators", """{"rotators":["aws-iam","aws-secrets-manager"]}"""),
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
