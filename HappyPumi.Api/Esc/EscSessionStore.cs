#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc;

/// <summary>One opened-environment session: the fully resolved property tree and when it expires.</summary>
public sealed record EscSession(string Id, Dictionary<string, EscValue> Properties, DateTimeOffset Expires);

/// <summary>
/// Holds opened-environment sessions. Sessions are <em>ephemeral</em> by design — a fully resolved
/// environment contains decrypted secrets and short-lived provider credentials that must never be
/// persisted (ADR-0005 keeps durable state in Postgres; this is deliberately not that). In-memory only.
/// </summary>
public interface IEscSessionStore
{
    /// <summary>Stores a resolved tree under a fresh session id valid for <paramref name="ttl"/>.</summary>
    string Create(Dictionary<string, EscValue> properties, TimeSpan ttl);

    /// <summary>Returns the session if it exists and has not expired; otherwise null.</summary>
    EscSession? Get(string id);
}

/// <summary>In-memory <see cref="IEscSessionStore"/>; expired sessions are dropped lazily on read.</summary>
public sealed class EscSessionStore : IEscSessionStore
{
    private readonly ConcurrentDictionary<string, EscSession> _sessions = new();

    public string Create(Dictionary<string, EscValue> properties, TimeSpan ttl)
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = new EscSession(id, properties, DateTimeOffset.UtcNow.Add(ttl));
        return id;
    }

    public EscSession? Get(string id)
    {
        if (!_sessions.TryGetValue(id, out var session))
            return null;
        if (session.Expires <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(id, out _);
            return null;
        }
        return session;
    }
}
