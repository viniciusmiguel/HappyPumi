#nullable enable

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyPumi.Api.Auth;

/// <summary>
/// Authenticates the Pulumi CLI, which sends <c>Authorization: token &lt;accessToken&gt;</c> (the CLI's own
/// scheme, not OIDC Bearer). This is the access-token half of ADR-0007; interactive/console callers use the
/// JWT Bearer scheme against Dex instead.
/// </summary>
/// <remarks>
/// DEV BEHAVIOR: any non-empty token is accepted and mapped to the seeded <c>happypumi</c> admin identity.
/// Real verification — looking the token up in a personal-access-token store, and resolving the caller's org
/// role from it — is the follow-up; the enforcement wiring (schemes, policies, per-endpoint roles) is what
/// lands here. A token of the form <c>role:&lt;role&gt;:&lt;login&gt;</c> is honoured so tests can exercise
/// non-admin authorization.
/// </remarks>
public sealed class PulumiTokenAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PulumiToken";
    private const string DefaultLogin = "happypumi";
    private const string DefaultRole = "admin";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
            // No credentials: stay anonymous so AllowAnonymous endpoints still work; secured endpoints 401.
            return Task.FromResult(AuthenticateResult.NoResult());

        var (login, role) = Resolve(token);
        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, login));
        identity.AddClaim(new Claim(ClaimTypes.Name, login));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        // Attach the caller's RBAC permission grants so FastEndpoints' Permissions(...) gating can enforce
        // resource:action checks (the console reads the same set via /permissions). ADR-0007.
        foreach (var permission in RbacPermissions.ForRole(role))
            identity.AddClaim(new Claim("permissions", permission));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>Reads the bearer value from either the Pulumi <c>token</c> scheme or standard <c>Bearer</c>.</summary>
    private string? ReadToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;
        foreach (var scheme in new[] { "token ", "Bearer " })
            if (header.StartsWith(scheme, System.StringComparison.OrdinalIgnoreCase))
                return header[scheme.Length..].Trim();
        return header.Trim();
    }

    private static (string Login, string Role) Resolve(string token)
    {
        // Test/dev convention to exercise non-admin paths: "role:<role>:<login>".
        var parts = token.Split(':');
        if (parts.Length == 3 && parts[0] == "role")
            return (parts[2], parts[1]);
        return (DefaultLogin, DefaultRole);
    }
}
