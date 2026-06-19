#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// A request to open a protected environment: the desired access + grant durations, who asked, who has
/// approved, and when the resulting grant expires. Persisted as jsonb, so new fields are backward-compatible
/// (old rows deserialize with defaults).
/// </summary>
public sealed record EscOpenRequest(
    string Id,
    long AccessDurationSeconds,
    long GrantExpirationSeconds,
    long BaseRevision,
    string Requester = "",
    long GrantExpiresAtUnix = 0,
    IReadOnlyList<string>? Approvers = null)
{
    /// <summary>The principals who have approved this request (never null at use sites).</summary>
    public IReadOnlyList<string> ApproverList => Approvers ?? Array.Empty<string>();

    /// <summary>True while an approved grant is still within its expiration window.</summary>
    public bool IsGranted(DateTime nowUtc)
        => GrantExpiresAtUnix > 0 && DateTimeOffset.FromUnixTimeSeconds(GrantExpiresAtUnix).UtcDateTime > nowUtc;
}

/// <summary>Where an open-request lives — its environment plus the request itself (for org-scoped lookups).</summary>
public sealed record OpenRequestLocation(EnvCoordinates Environment, EscOpenRequest Request);

/// <summary>
/// Persistence seam for open-access requests against gated environments (the approvals entry point).
/// Backed by PostgreSQL (see <c>PostgresEscOpenRequestStore</c>).
/// </summary>
public interface IEscOpenRequestStore
{
    EscOpenRequest Create(EnvCoordinates environment, long accessDurationSeconds, long grantExpirationSeconds, long baseRevision, string requester);
    EscOpenRequest? Get(EnvCoordinates environment, string changeRequestId);
    EscOpenRequest? Update(EnvCoordinates environment, string changeRequestId, long accessDurationSeconds, long grantExpirationSeconds);

    /// <summary>Finds a request by id within an org (the approve/unapprove routes carry no project/env).</summary>
    OpenRequestLocation? Locate(string org, string changeRequestId);

    /// <summary>Persists a mutated request (e.g. after recording an approval / setting the grant).</summary>
    EscOpenRequest? Replace(EnvCoordinates environment, EscOpenRequest request);

    /// <summary>True when <paramref name="requester"/> holds an approved, unexpired grant on the environment.</summary>
    bool HasActiveGrant(EnvCoordinates environment, string requester, DateTime nowUtc);
}
