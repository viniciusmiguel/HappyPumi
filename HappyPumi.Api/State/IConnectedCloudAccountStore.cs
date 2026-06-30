#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for cloud accounts connected through the ESC cloud-setup OAuth flow (PR6, ADR-0005).
/// Distinct from <see cref="ICloudAccountStore"/> (Insights resource scanning); here a row is keyed by
/// (org, provider) and holds the accounts/subscriptions/projects discovered for that connected provider.
/// The <see cref="StoredConnectedCloudAccount.Credential"/> is write-only (set on OAuth completion) and is
/// never projected into a list response.
/// </summary>
public interface IConnectedCloudAccountStore
{
    /// <summary>
    /// Replaces the connected-account record for (org, provider) with the supplied accounts + credential.
    /// </summary>
    void Upsert(string org, string provider, IReadOnlyList<CloudAccountEntry> accounts, string? credential);

    /// <summary>Lists the connected accounts for (org, provider); empty when none are connected.</summary>
    IReadOnlyList<CloudAccountEntry> List(string org, string provider);
}

/// <summary>One discovered cloud account/subscription/project under a connected provider.</summary>
public sealed class CloudAccountEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Numeric account/project id (e.g. GCP project number); null when not applicable.</summary>
    public long? Number { get; init; }

    /// <summary>Roles the connected principal holds on the account; null when not reported.</summary>
    public List<string>? Roles { get; init; }
}

/// <summary>
/// A connected-provider record (one per org+provider). <see cref="Credential"/> is the write-only OAuth
/// access token used to refresh the account list; it is never returned to callers.
/// </summary>
public sealed class StoredConnectedCloudAccount
{
    public required string Org { get; init; }

    /// <summary>"aws" | "azure" | "gcp".</summary>
    public required string Provider { get; init; }

    public List<CloudAccountEntry> Accounts { get; set; } = new();
    public string? Credential { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
