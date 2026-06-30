#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted customer-managed key (CMK / BYOK) record (ADR-0005). One org may register several keys, but
/// at most one is the <see cref="IsDefault"/> key used to encrypt new stack secrets. <see cref="KeyArn"/>/
/// <see cref="RoleArn"/> are only set for AWS KMS keys.
/// </summary>
public sealed class StoredCmk
{
    public required string Id { get; init; }
    public required string Org { get; init; }
    public required string Name { get; set; }
    public string KeyType { get; set; } = "aws-kms";
    public string? KeyArn { get; set; }
    public string? RoleArn { get; set; }
    public bool IsDefault { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A key-encryption-key (KEK) migration record: re-encrypting an org's secrets when the default CMK changes.
/// <see cref="State"/> is <c>completed</c> or <c>failed</c>; failed ones are flipped by a retry.
/// </summary>
public sealed class StoredKeyMigration
{
    public required string Id { get; init; }
    public required string Org { get; init; }
    public string State { get; set; } = "completed"; // completed | failed
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for customer-managed keys and their KEK migrations (ENDPOINTS.md settings cluster, PR4).
/// Backed by PostgreSQL in production and an in-memory map in unit tests (ADR-0005). Creating a key or setting
/// a new default demotes the previous default and records a migration.
/// </summary>
public interface ICmkStore
{
    /// <summary>Creates a key, makes it the org's default (demoting others), and records a migration.</summary>
    StoredCmk Create(string org, string name, string keyType, string? keyArn, string? roleArn);

    /// <summary>All keys for an org, newest first.</summary>
    IReadOnlyList<StoredCmk> List(string org);

    /// <summary>A single key by id within an org, or null when missing.</summary>
    StoredCmk? Get(string org, string id);

    /// <summary>Marks a key default (demoting others) and records a migration. False when the key is missing.</summary>
    bool SetDefault(string org, string id);

    /// <summary>Disables a key (Enabled=false, IsDefault=false). False when the key is missing.</summary>
    bool Disable(string org, string id);

    /// <summary>Disables every key for the org (revert to service-managed). Returns the count disabled.</summary>
    int DisableAll(string org);

    /// <summary>All KEK migrations for an org, newest first.</summary>
    IReadOnlyList<StoredKeyMigration> ListMigrations(string org);

    /// <summary>Flips any <c>failed</c> migration to <c>completed</c>. Returns the count flipped.</summary>
    int RetryMigrations(string org);
}
