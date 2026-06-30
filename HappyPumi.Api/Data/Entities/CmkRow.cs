#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted customer-managed key (CMK / BYOK). Key: Id; indexed on (Org, Id) for per-org list/lookup.
/// At most one row per org carries <see cref="IsDefault"/>. KMS ARNs are null for non-AWS key types.
/// </summary>
public sealed class CmkRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Name { get; set; } = "";
    public string KeyType { get; set; } = "aws-kms";
    public string? KeyArn { get; set; }
    public string? RoleArn { get; set; }
    public bool IsDefault { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
