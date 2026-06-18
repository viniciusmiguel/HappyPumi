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
            var mock = StackMetadataMock(path) ?? StackUpdatesMock(path) ?? Match(path);
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
        // Console catch-all for unimplemented bootstrap endpoints — but let the real /api/console/* endpoints
        // (package nav, org permissions, the project page) handle their own paths instead of returning {}.
        if (path.StartsWith("/api/console/", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/registry/", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/permissions", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("/projects/", StringComparison.OrdinalIgnoreCase))
            return "{}";
        return null;
    }

    // Ordered: more specific paths first.
    private static readonly List<(string needle, string json)> Mocks =
    [
        // deployments/metadata: in-flight counts. (Org metadata + permissions are now real endpoints.)
        ("/deployments/metadata", """{"running":0,"queued":0}"""),

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
}
