#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Builds the <see cref="EnvironmentTag"/> wire shape from HappyPumi's simple name→value tag storage. The
/// per-tag editor/timestamp metadata ESC exposes is synthesized from the environment (we don't track tag
/// authorship separately), which keeps the response contract-complete without a dedicated tag-audit table.
/// </summary>
public static class EscTagMapper
{
    public static EnvironmentTag From(StoredEnvironment env, string name, string value) => new()
    {
        Name = name,
        Value = value,
        EditorLogin = env.OwnerLogin,
        EditorName = env.OwnerName,
        Created = env.Created,
        Modified = env.Modified,
    };

    /// <summary>Builds the revision-tag wire shape (a name pointing at a revision number) from a revision.</summary>
    public static EnvironmentRevisionTag RevisionTag(StoredEnvRevision revision, string name) => new()
    {
        Name = name,
        Revision = revision.Number,
        Created = revision.Created,
        Modified = revision.Created,
        EditorLogin = revision.CreatorLogin,
        EditorName = revision.CreatorName,
    };
}
