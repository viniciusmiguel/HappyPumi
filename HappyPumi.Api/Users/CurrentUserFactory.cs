#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Users;

/// <summary>
/// Builds the dev <see cref="User"/> profile returned by the account endpoints (GetCurrentUser and the VCS
/// identity-provider endpoints that echo the refreshed user). Extracted so the several endpoints that must
/// return an identical user shape share one construction (DRY).
/// </summary>
/// <example><code>await Send.OkAsync(CurrentUserFactory.Build(login), ct);</code></example>
public static class CurrentUserFactory
{
    /// <summary>
    /// Builds the profile for <paramref name="login"/>. <see cref="User.GithubLogin"/> MUST be non-empty:
    /// the Pulumi CLI rejects the login otherwise ("unexpected response from server") — see
    /// GetPulumiAccountDetails in pulumi/pkg/backend/httpstate/client/client.go. It is the canonical user
    /// handle the CLI keys off (per ADR-0009 we map the active VCS provider's handle onto it).
    /// </summary>
    public static User Build(string login) => new()
    {
        Email = "test@contoso.com",
        Id = Guid.NewGuid().ToString(),
        GithubLogin = login,
        Name = "Vinicius",
        AvatarUrl = "https://example.invalid/avatar.png",
        HasMfa = false,
        Identities = new List<string>(),
        Organizations = new List<OrganizationSummaryWithRole>(),
        IsManagedByMultiOrg = false,
        SiteAdmin = true,
        TokenInfo = new TokenInfo(),
    };
}
