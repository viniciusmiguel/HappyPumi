#nullable enable

using System;
using System.Collections.Concurrent;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>A request to open a protected environment: the desired access + grant durations, as a change request.</summary>
public sealed record EscOpenRequest(string Id, long AccessDurationSeconds, long GrantExpirationSeconds, long BaseRevision);

/// <summary>
/// Holds open-access requests for protected environments (the approvals entry point). In-memory for now; the
/// thin wire contract exposes the request's durations and a change-request id — full approve/grant gating is
/// a follow-up.
/// </summary>
public interface IEscOpenRequestStore
{
    EscOpenRequest Create(EnvCoordinates environment, long accessDurationSeconds, long grantExpirationSeconds, long baseRevision);
    EscOpenRequest? Get(EnvCoordinates environment, string changeRequestId);
    EscOpenRequest? Update(EnvCoordinates environment, string changeRequestId, long accessDurationSeconds, long grantExpirationSeconds);
}

/// <summary>In-memory <see cref="IEscOpenRequestStore"/>.</summary>
public sealed class EscOpenRequestStore : IEscOpenRequestStore
{
    private readonly ConcurrentDictionary<string, EscOpenRequest> _requests = new();

    private static string Key(EnvCoordinates e, string id) => $"{e.Org}/{e.Project}/{e.Name}/{id}";

    public EscOpenRequest Create(EnvCoordinates environment, long accessDurationSeconds, long grantExpirationSeconds, long baseRevision)
    {
        var request = new EscOpenRequest(Guid.NewGuid().ToString("N"), accessDurationSeconds, grantExpirationSeconds, baseRevision);
        _requests[Key(environment, request.Id)] = request;
        return request;
    }

    public EscOpenRequest? Get(EnvCoordinates environment, string changeRequestId)
        => _requests.GetValueOrDefault(Key(environment, changeRequestId));

    public EscOpenRequest? Update(EnvCoordinates environment, string changeRequestId, long accessDurationSeconds, long grantExpirationSeconds)
    {
        var key = Key(environment, changeRequestId);
        if (!_requests.TryGetValue(key, out var existing))
            return null;
        var updated = existing with { AccessDurationSeconds = accessDurationSeconds, GrantExpirationSeconds = grantExpirationSeconds };
        _requests[key] = updated;
        return updated;
    }
}
