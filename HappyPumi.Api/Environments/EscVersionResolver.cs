#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Resolves an ESC <c>version</c> path segment to a concrete revision. A version is either a revision number
/// (e.g. <c>3</c>) or an existing revision-tag name pointing at one (e.g. <c>latest</c>, <c>stable</c>).
/// </summary>
public static class EscVersionResolver
{
    public static StoredEnvRevision? Resolve(IReadOnlyList<StoredEnvRevision> revisions, string version)
        => long.TryParse(version, out var number)
            ? revisions.FirstOrDefault(r => r.Number == number)
            : revisions.FirstOrDefault(r => r.Tags.Contains(version));
}
