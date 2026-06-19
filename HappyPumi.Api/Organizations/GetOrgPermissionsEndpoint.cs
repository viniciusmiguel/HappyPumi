#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Auth;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>Request for the caller's org-level permission set.</summary>
public sealed class GetOrgPermissionsRequest
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
}

/// <summary>
/// GET /api/console/orgs/{orgName}/permissions — the console fetches the caller's granted RBAC permissions
/// and enables/disables UI on them (it does <c>new Set(resp)</c>, so this is a bare JSON array). The seeded
/// admin is granted the full set; per-role grants are the ADR-0007 follow-up.
/// </summary>
public sealed class GetOrgPermissionsEndpoint : Endpoint<GetOrgPermissionsRequest, List<string>>
{
    public override void Configure()
    {
        Get("/api/console/orgs/{orgName}/permissions");
        Description(b => b.WithTags("Organizations").WithSummary("GetOrgPermissions").WithName("GetOrgPermissions"));
    }

    public override Task HandleAsync(GetOrgPermissionsRequest req, CancellationToken ct)
        => Send.OkAsync(new List<string>(RbacPermissions.All), ct);
}
