#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Auth;

/// <summary>
/// Enforces a valid workflow-runner pool token on the agent's pool-scoped endpoints. The customer-managed
/// workflow agent presents its pool token (minted by CreateOrgAgentPool) as <c>Authorization: token &lt;t&gt;</c>;
/// an unknown/missing token is rejected with 401 — the agent's bootstrap + poll loop can't start without a
/// real pool token. Job-scoped endpoints (workflow/jobs/*) use the server-minted job token instead and are
/// reached only after a pool-authenticated poll, so they are not gated here.
/// </summary>
public static class AgentPoolTokenMiddleware
{
    // The agent's pool-scoped calls (NOT the user-initiated POST /api/stacks/.../deployments).
    private static readonly string[] ProtectedPrefixes =
    [
        "/api/background-activities",
        "/api/deployments/poll",
        "/api/deployments/executor",
    ];

    public static IApplicationBuilder UseAgentPoolTokenAuth(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            if (!IsProtected(ctx.Request.Path))
            {
                await next();
                return;
            }

            var token = ReadToken(ctx);
            var pools = ctx.RequestServices.GetRequiredService<IAgentPoolStore>();
            if (string.IsNullOrWhiteSpace(token) || pools.FindByToken(token) is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { message = "invalid or missing agent pool token" });
                return;
            }

            await next();
        });

    private static bool IsProtected(PathString path)
        => ProtectedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static string? ReadToken(HttpContext ctx)
    {
        var header = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;
        foreach (var scheme in new[] { "token ", "Bearer " })
            if (header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return header[scheme.Length..].Trim();
        return header.Trim();
    }
}
