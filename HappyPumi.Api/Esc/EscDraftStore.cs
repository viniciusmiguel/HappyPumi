#nullable enable

using System;
using System.Collections.Concurrent;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>A draft (proposed, unpublished change) to an environment definition.</summary>
public sealed record EscDraft(string Id, string Yaml, long BaseRevision);

/// <summary>
/// Holds environment drafts — work-in-progress definitions identified by a change-request id, before they
/// are published as a revision. In-memory for now (a durable change-request store with approvals is a
/// follow-up); analogous to the open-session and rotation-history stores.
/// </summary>
public interface IEscDraftStore
{
    /// <summary>Creates a draft and returns its change-request id.</summary>
    string Create(EnvCoordinates environment, string yaml, long baseRevision);
    EscDraft? Get(EnvCoordinates environment, string changeRequestId);
    /// <summary>Replaces a draft's YAML; false when it does not exist.</summary>
    bool Update(EnvCoordinates environment, string changeRequestId, string yaml);
}

/// <summary>In-memory <see cref="IEscDraftStore"/>.</summary>
public sealed class EscDraftStore : IEscDraftStore
{
    private readonly ConcurrentDictionary<string, EscDraft> _drafts = new();

    private static string Key(EnvCoordinates e, string id) => $"{e.Org}/{e.Project}/{e.Name}/{id}";

    public string Create(EnvCoordinates environment, string yaml, long baseRevision)
    {
        var id = Guid.NewGuid().ToString("N");
        _drafts[Key(environment, id)] = new EscDraft(id, yaml, baseRevision);
        return id;
    }

    public EscDraft? Get(EnvCoordinates environment, string changeRequestId)
        => _drafts.GetValueOrDefault(Key(environment, changeRequestId));

    public bool Update(EnvCoordinates environment, string changeRequestId, string yaml)
    {
        var key = Key(environment, changeRequestId);
        if (!_drafts.TryGetValue(key, out var draft))
            return false;
        _drafts[key] = draft with { Yaml = yaml };
        return true;
    }
}
