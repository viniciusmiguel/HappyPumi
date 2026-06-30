#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted access token. Key: Id; indexed on (Scope, OwnerKey) for list/revoke. Only the SHA-256
/// <see cref="HashedValue"/> is stored — the plaintext is shown once at issue time and never persisted.
/// </summary>
public sealed class AccessTokenRow
{
    public string Id { get; set; } = default!;
    public string Scope { get; set; } = default!;
    public string OwnerKey { get; set; } = default!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string HashedValue { get; set; } = default!;
    public string CreatedBy { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public long LastUsed { get; set; }
    public long Expires { get; set; }
    public bool Admin { get; set; }
    public string? RoleId { get; set; }
}
