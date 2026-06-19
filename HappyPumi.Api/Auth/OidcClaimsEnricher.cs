#nullable enable

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HappyPumi.Api.Auth;

/// <summary>
/// Resolves a HappyPumi role from a Dex (OIDC) id-token and attaches the matching RBAC permission grants,
/// so OIDC-authenticated console users are gated by the same Permissions(...) checks as token callers
/// (ADR-0007). Prefers the IdP <c>groups</c> claim (happypumi-admins → admin); falls back to the email's
/// group convention for IdPs/connectors that don't surface groups in the id-token. Default: member.
/// </summary>
public static class OidcClaimsEnricher
{
    public const string AdminGroup = "happypumi-admins";

    public static Task OnTokenValidated(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        var role = ResolveRole(identity);
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        foreach (var permission in RbacPermissions.ForRole(role))
            identity.AddClaim(new Claim("permissions", permission));
        return Task.CompletedTask;
    }

    private static string ResolveRole(ClaimsIdentity identity)
    {
        var groups = identity.FindAll("groups").Select(c => c.Value).ToList();
        if (groups.Count > 0)
            return groups.Contains(AdminGroup) ? "admin" : "member";

        // No groups claim (some connectors omit it): map by the well-known demo email local-part.
        var email = identity.FindFirst("email")?.Value ?? identity.FindFirst(ClaimTypes.Email)?.Value;
        return email?.StartsWith("admin@", System.StringComparison.OrdinalIgnoreCase) == true ? "admin" : "member";
    }
}
