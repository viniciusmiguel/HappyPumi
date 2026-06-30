#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for access tokens (ENDPOINTS.md settings cluster, PR1). Tokens are scoped to a user, an
/// org, or a team and are write-only on the wire: <see cref="Issue"/> returns the plaintext once via the
/// <c>plaintext</c> out-param, and only its SHA-256 hash is stored. This store manages token <em>records</em>
/// only — it does NOT change how <c>PulumiTokenAuthHandler</c> authenticates requests (a deliberate, separate
/// change). Backed by PostgreSQL in production and an in-memory map in unit tests (ADR-0005).
/// </summary>
public interface IAccessTokenStore
{
    /// <summary>
    /// Issues a new token under <paramref name="scope"/>/<paramref name="ownerKey"/>, persisting only its
    /// hash, and returns the record. The single-use plaintext is returned via <paramref name="plaintext"/>.
    /// </summary>
    StoredAccessToken Issue(
        string scope, string ownerKey, string name, string description, string createdBy,
        out string plaintext, long expires = 0, bool admin = false, string? roleId = null);

    /// <summary>All tokens for a scope/owner, newest first (metadata only — never the hash or plaintext).</summary>
    IReadOnlyList<StoredAccessToken> List(string scope, string ownerKey);

    /// <summary>Org-scoped tokens filtered to those assigned <paramref name="roleId"/>, newest first.</summary>
    IReadOnlyList<StoredAccessToken> ListByRole(string org, string roleId);

    /// <summary>Revokes a token by id within a scope/owner. False when no such token exists.</summary>
    bool Delete(string scope, string ownerKey, string id);
}
