#nullable enable

using System.Security.Claims;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Auth;

/// <summary>
/// Extracts the requesting <see cref="UpdateActor"/> from the authenticated principal (populated by the
/// PulumiToken / OIDC auth handlers). Returns null when the request is anonymous.
/// </summary>
public static class RequestActor
{
    public static UpdateActor? From(ClaimsPrincipal user)
    {
        var login = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return null;
        var name = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? login;
        return new UpdateActor(login, name);
    }
}
