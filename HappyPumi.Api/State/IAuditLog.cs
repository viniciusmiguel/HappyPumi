#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.State;

/// <summary>
/// Records infrastructure-changing actions for an org (ADR-0010) so they surface on the Audit logs page.
/// In-memory by default like the other stores; safe for concurrent use.
/// </summary>
public interface IAuditLog
{
    /// <summary>Records an audit event (e.g. event "team.create", description, actor).</summary>
    void Record(string org, string @event, string description, string actor);

    /// <summary>All audit events for an org, newest first.</summary>
    IReadOnlyList<AuditLogRow> List(string org);
}
