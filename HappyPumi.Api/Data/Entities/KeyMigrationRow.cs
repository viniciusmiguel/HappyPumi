#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted key-encryption-key (KEK) migration. Key: Id; indexed on (Org, Created) for the per-org list.
/// <see cref="State"/> is <c>completed</c> or <c>failed</c>; a retry flips failed rows to completed.
/// </summary>
public sealed class KeyMigrationRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string State { get; set; } = "completed";
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
