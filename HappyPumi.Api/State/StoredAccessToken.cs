#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted access-token record (ADR-0005). Only the SHA-256 <see cref="HashedValue"/> is stored; the
/// plaintext is shown once at issue time and never again. <see cref="Scope"/> is one of <c>user</c>,
/// <c>org</c>, or <c>team</c>; <see cref="OwnerKey"/> is the owning user login, org slug, or <c>"org/team"</c>.
/// </summary>
public sealed class StoredAccessToken
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Scope { get; set; } = default!;
    public string OwnerKey { get; set; } = default!;
    /// <summary>SHA-256 hash (hex) of the issued plaintext. The plaintext is never persisted.</summary>
    public string HashedValue { get; set; } = default!;
    public string CreatedBy { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.UtcNow;
    /// <summary>Unix epoch seconds of last use; 0 when never used.</summary>
    public long LastUsed { get; set; }
    /// <summary>Unix epoch seconds when the token expires; 0 when it never expires.</summary>
    public long Expires { get; set; }
    public bool Admin { get; set; }
    /// <summary>Custom role id this org token is scoped to (org scope only); null otherwise.</summary>
    public string? RoleId { get; set; }
}
