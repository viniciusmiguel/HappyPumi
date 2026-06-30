#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted change request (change-requests feature group, PR2, ADR-0005). A change request wraps an ESC
/// environment draft (its id == the draft id), moves through a status lifecycle
/// (<c>draft → submitted → applied | closed</c>), and accrues approvals. Apply commits the wrapped draft as
/// a new environment revision. Gate enforcement is layered on in PR3.
/// </summary>
public sealed class StoredChangeRequest
{
    public required string Id { get; init; }
    public required string Org { get; init; }
    public string Action { get; set; } = "update";
    public string Description { get; set; } = "";
    public string TargetProject { get; set; } = "";
    public string TargetEnv { get; set; } = "";
    public string Status { get; set; } = "draft"; // draft|submitted|applied|closed
    public long LatestRevisionNumber { get; set; }
    public string CreatedBy { get; set; } = "happypumi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Approvers { get; set; } = new();
}

/// <summary>
/// Persistence seam for change requests (ADR-0005). Backed by PostgreSQL in production and an in-memory map
/// in unit tests. Change requests are keyed by id within an org. <see cref="Update"/> mutates in place and
/// returns the updated record (null when missing), mirroring the other settings-cluster stores.
/// </summary>
public interface IChangeRequestStore
{
    /// <summary>Persists a new change request and returns it.</summary>
    StoredChangeRequest Create(StoredChangeRequest cr);

    /// <summary>All change requests for an org, newest first.</summary>
    IReadOnlyList<StoredChangeRequest> List(string org);

    /// <summary>A single change request by id within an org, or null when missing.</summary>
    StoredChangeRequest? Get(string org, string id);

    /// <summary>Applies <paramref name="mutate"/> to the change request and returns it; null when missing.</summary>
    StoredChangeRequest? Update(string org, string id, Action<StoredChangeRequest> mutate);
}
