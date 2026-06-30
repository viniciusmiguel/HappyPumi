#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A cloud-setup connected-provider record (PR6, ADR-0005). Key: (Org, Provider). The discovered
/// <see cref="Accounts"/> are stored as jsonb; <see cref="Credential"/> is the write-only OAuth token.
/// </summary>
public sealed class ConnectedCloudAccountRow
{
    public string Org { get; set; } = default!;

    /// <summary>"aws" | "azure" | "gcp".</summary>
    public string Provider { get; set; } = default!;

    /// <summary>Discovered accounts/subscriptions/projects (jsonb).</summary>
    public List<CloudAccountEntry> Accounts { get; set; } = new();

    /// <summary>Write-only OAuth access token (set on OAuth completion); never returned to callers.</summary>
    public string? Credential { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
}
