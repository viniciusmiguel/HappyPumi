#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>
/// The persisted audit-log export configuration for an org (ADR-0005/ADR-0010). Backs
/// <c>Get/Update/DeleteAuditLogExportConfiguration</c> plus the force/test actions. S3 field names mirror the
/// <c>AuditLogsExportS3Config</c> wire contract (iamRoleArn / s3BucketName / s3PathPrefix). A fresh, unset org
/// yields a disabled default (never null) so the GET endpoint always returns a 200 shape.
/// </summary>
public sealed class StoredAuditExportConfig
{
    public required string Org { get; init; }
    public bool Enabled { get; set; }
    public string? IamRoleArn { get; set; }
    public string? S3BucketName { get; set; }
    public string? S3PathPrefix { get; set; }
    public string? LastResultMessage { get; set; }
    public long LastResultTimestamp { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for per-org audit-log export configuration (ADR-0005). In-memory by default like the
/// other stores; the Postgres implementation persists one row per org. Safe for concurrent use.
/// </summary>
public interface IAuditExportConfigStore
{
    /// <summary>The org's export config, or a fresh disabled default (not persisted) when none exists yet.</summary>
    StoredAuditExportConfig Get(string org);

    /// <summary>Applies <paramref name="mutate"/> to the org's config, persisting the result (creating the
    /// row on first write), and returns the updated config.</summary>
    StoredAuditExportConfig Upsert(string org, Action<StoredAuditExportConfig> mutate);

    /// <summary>Removes the org's config (back to a disabled default). False when none existed.</summary>
    bool Delete(string org);
}
