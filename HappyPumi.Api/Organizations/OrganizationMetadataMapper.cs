#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Builds the wire <see cref="OrganizationMetadata"/> from the persisted <see cref="StoredOrgSettings"/> plus
/// live counts. Counts without a readily-queryable source (stacks/environments/accounts) report a
/// deterministic zero rather than a fabricated value (org-admin long-tail design).
/// </summary>
public static class OrganizationMetadataMapper
{
    public static OrganizationMetadata ToMetadata(StoredOrgSettings s, long memberCount)
    {
        var meta = new OrganizationMetadata
        {
            Id = s.Org,
            Kind = "organization",
            UserRole = "admin", // permissive-token decision: the caller is treated as an org admin (ADR-0007)
            Created = s.Created,
            MemberCount = memberCount,
            Features = new OrganizationFeatures(),
        };
        MapPermissions(s, meta);
        MapFlags(s, meta);
        MapNeo(s, meta);
        return meta;
    }

    private static void MapPermissions(StoredOrgSettings s, OrganizationMetadata meta)
    {
        meta.DefaultStackPermission = s.DefaultStackPermission;
        meta.DefaultAccountPermission = s.DefaultAccountPermission;
        meta.DefaultEnvironmentPermission = s.DefaultEnvironmentPermission;
        meta.DefaultRoleId = s.DefaultRoleId;
        meta.DefaultDeploymentRoleId = s.DefaultDeploymentRoleId;
        meta.PreferredVcs = s.PreferredVcs;
    }

    private static void MapFlags(StoredOrgSettings s, OrganizationMetadata meta)
    {
        meta.MembersCanCreateStacks = s.MembersCanCreateStacks;
        meta.MembersCanDeleteStacks = s.MembersCanDeleteStacks;
        meta.MembersCanCreateTeams = s.MembersCanCreateTeams;
        meta.MembersCanTransferStacks = s.MembersCanTransferStacks;
        meta.MembersCanCreateAccounts = s.MembersCanCreateAccounts;
    }

    private static void MapNeo(StoredOrgSettings s, OrganizationMetadata meta)
    {
        meta.AiEnablement = s.AiEnablement;
        meta.NeoEnabled = s.NeoEnabled;
        meta.NeoApprovalMode = "manual";
        meta.NeoTaskSharingMode = "none";
    }
}
