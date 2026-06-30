#nullable enable

using System;
using System.Collections.Concurrent;

namespace HappyPumi.Api.CloudSetup;

/// <summary>One in-flight cloud-setup OAuth session: which org/provider initiated it and where to return.</summary>
public sealed record CloudOAuthSession(string Org, string Provider, string? ReturnUrl);

/// <summary>
/// Ephemeral store for cloud-setup OAuth sessions (PR6). The session id doubles as the OAuth <c>state</c>
/// parameter, so completion can recover the org/provider that started the flow. In-memory only — sessions
/// are short-lived and need no persistence.
/// </summary>
public interface ICloudOAuthSessionStore
{
    /// <summary>Creates a session and returns its id (the OAuth <c>state</c>).</summary>
    string Create(string org, string provider, string? returnUrl);

    /// <summary>Looks up a session by id; null when unknown or already consumed.</summary>
    CloudOAuthSession? Get(string sessionId);
}

/// <summary>In-memory <see cref="ICloudOAuthSessionStore"/> (singleton, concurrent).</summary>
public sealed class CloudOAuthSessionStore : ICloudOAuthSessionStore
{
    private readonly ConcurrentDictionary<string, CloudOAuthSession> _sessions = new();

    public string Create(string org, string provider, string? returnUrl)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        _sessions[sessionId] = new CloudOAuthSession(org, provider, returnUrl);
        return sessionId;
    }

    public CloudOAuthSession? Get(string sessionId)
        => _sessions.TryGetValue(sessionId, out var session) ? session : null;
}
