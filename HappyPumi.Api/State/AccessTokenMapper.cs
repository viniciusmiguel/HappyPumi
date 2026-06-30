#nullable enable

using System.Globalization;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Projects a <see cref="StoredAccessToken"/> onto the wire <see cref="AccessToken"/>. The hash and plaintext
/// are deliberately never copied — token values are write-only on the wire (issue-once).
/// </summary>
public static class AccessTokenMapper
{
    public static AccessToken ToContract(StoredAccessToken t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        CreatedBy = t.CreatedBy,
        Created = t.Created.ToString("o", CultureInfo.InvariantCulture),
        LastUsed = t.LastUsed,
        Expires = t.Expires,
        Admin = t.Admin,
        Role = t.RoleId is null ? null : new AccessTokenRole { Id = t.RoleId, Name = t.RoleId, DefaultIdentifier = "" },
    };
}
