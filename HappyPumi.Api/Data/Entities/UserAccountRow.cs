#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// Persisted per-user account state backing the /api/user/* account surface (org-admin PR6, ADR-0005).
/// Key: Login (one row per user).
/// </summary>
public sealed class UserAccountRow
{
    public string Login { get; set; } = default!;
    public string? PendingEmail { get; set; }
    public bool VerifiedEmail { get; set; } = true;
    public string? DefaultOrg { get; set; }
    public DateTime Created { get; set; }
}
