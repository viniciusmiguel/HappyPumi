#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>
/// Per-user account state backing the <c>/api/user/*</c> account surface (org-admin PR6, ADR-0005): the
/// pending email-change awaiting verification, whether the primary email is verified (defaults to
/// <c>true</c> so a fresh dev user is considered verified), and the user's chosen default org. Keyed by
/// the authenticated login.
/// </summary>
public sealed class StoredUserAccount
{
    public required string Login { get; init; }
    public string? PendingEmail { get; set; }
    public bool VerifiedEmail { get; set; } = true;
    public string? DefaultOrg { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for per-user account state (ADR-0005). In-memory by default like the other stores; the
/// Postgres implementation persists one row per login. Safe for concurrent use.
/// </summary>
public interface IUserAccountStore
{
    /// <summary>Returns the user's account, or a fresh default (not persisted) when none exists yet.</summary>
    StoredUserAccount Get(string login);

    /// <summary>Applies <paramref name="mutate"/> to the user's account, persisting the result (creating the
    /// row on first write), and returns the updated account.</summary>
    StoredUserAccount Update(string login, Action<StoredUserAccount> mutate);
}
