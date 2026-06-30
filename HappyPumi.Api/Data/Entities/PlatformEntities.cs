#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.Data.Entities;

/// <summary>An audit event — an infrastructure-changing action recorded for the org (ADR-0010). Key: Id.</summary>
public sealed class AuditLogRow
{
    public long Id { get; set; }
    public string Org { get; set; } = default!;
    public string Event { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string ActorName { get; set; } = default!;
    public string SourceIp { get; set; } = "127.0.0.1";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>An IDP "service" grouping stacks/environments/etc. Key: (Org, Name). Items are jsonb.</summary>
public sealed class ServiceRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>A connected cloud account for Insights resource scanning. Key: (Org, Name).</summary>
public sealed class CloudAccountRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Provider { get; set; } = "aws"; // aws | azure | gcp | kubernetes
    public string Description { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>A connected version-control account (ADR-0009 VCS). Key: (Org, Name).</summary>
public sealed class VcsConnectionRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Kind { get; set; } = "github"; // github | gitlab | azuredevops | bitbucket
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>An OIDC issuer trusted for SSO/token exchange (the Identity-providers page). Key: (Org, Name).</summary>
public sealed class OidcIssuerRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;

    /// <summary>Opaque GUID identifier used by the spec's /oidc/issuers/{issuerId} routes (distinct from the name key).</summary>
    public string Id { get; set; } = default!;

    /// <summary>SHA-1 TLS certificate thumbprints (uppercase hex). Stored as jsonb. Empty until fetched/regenerated.</summary>
    public List<string> Thumbprints { get; set; } = new();

    /// <summary>Maximum token expiration in seconds the issuer's tokens may request; null = service default.</summary>
    public long? MaxExpiration { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Modified { get; set; }
    public DateTime? LastUsed { get; set; }
}

/// <summary>An approval rule requiring sign-off before updates to matching stacks. Key: (Org, Name).</summary>
public sealed class ApprovalRuleRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string StackPattern { get; set; } = "*";
    public int RequiredApprovals { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
