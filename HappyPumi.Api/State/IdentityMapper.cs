#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps the in-memory IDP domain to the org members/roles wire DTOs.</summary>
public static class IdentityMapper
{
    public static OrganizationMember ToMember(StoredMember member) => new()
    {
        Role = member.Role,
        Created = member.Created,
        KnownToPulumi = true,
        VirtualAdmin = false,
        User = new UserInfo
        {
            GithubLogin = member.UserLogin,
            Name = member.UserLogin,
            AvatarUrl = "https://example.invalid/avatar.png",
        },
        // The fine-grained-authorization role mirrors the membership role until a richer model lands.
        FgaRole = new FgaRole { Id = member.Role, Name = member.Role, ModifiedAt = member.Created },
    };

    public static PermissionDescriptorRecord ToRecord(StoredRole role) => new()
    {
        Id = role.Id,
        OrgId = role.OrgId,
        Created = role.Created,
        Modified = role.Modified,
        Version = role.Version,
        IsOrgDefault = role.IsOrgDefault,
        Name = role.Name,
        Description = role.Description,
        ResourceType = role.ResourceType,
        UxPurpose = role.UxPurpose,
        Details = role.Details,
    };
}
