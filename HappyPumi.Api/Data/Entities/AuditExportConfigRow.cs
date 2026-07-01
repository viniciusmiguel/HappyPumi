#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// Persisted per-org audit-log export configuration backing Get/Update/DeleteAuditLogExportConfiguration
/// (ADR-0010). Key: Org (one row per org).
/// </summary>
public sealed class AuditExportConfigRow
{
    public string Org { get; set; } = default!;
    public bool Enabled { get; set; }
    public string? IamRoleArn { get; set; }
    public string? S3BucketName { get; set; }
    public string? S3PathPrefix { get; set; }
    public string? LastResultMessage { get; set; }
    public long LastResultTimestamp { get; set; }
    public DateTime Created { get; set; }
}
