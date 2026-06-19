#nullable enable

using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>A draft (proposed, unpublished change) to an environment definition.</summary>
public sealed record EscDraft(string Id, string Yaml, long BaseRevision);

/// <summary>
/// Persistence seam for environment drafts — work-in-progress definitions identified by a change-request id,
/// before they are published as a revision. Backed by PostgreSQL (see <c>PostgresEscDraftStore</c>).
/// </summary>
public interface IEscDraftStore
{
    /// <summary>Creates a draft and returns its change-request id.</summary>
    string Create(EnvCoordinates environment, string yaml, long baseRevision);
    EscDraft? Get(EnvCoordinates environment, string changeRequestId);
    /// <summary>Replaces a draft's YAML; false when it does not exist.</summary>
    bool Update(EnvCoordinates environment, string changeRequestId, string yaml);
}
