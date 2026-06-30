#nullable enable

using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A provider-neutral VCS integration record (ADR-0009). Key: Id; scoped/listed by (Org, Kind).
/// Scalar columns for the fields the endpoints filter/sort on; <see cref="Settings"/> is jsonb.
/// </summary>
public sealed class VcsIntegrationRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;

    /// <summary>"github" | "github-enterprise" | "azure-devops".</summary>
    public string Kind { get; set; } = default!;

    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public string? AccountName { get; set; }
    public long? AccountId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AzureProject { get; set; }

    /// <summary>Write-only OAuth/PAT access token for the connected account (set on OAuth completion).</summary>
    public string? Credential { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    /// <summary>PR-comment / AI-review toggles (jsonb).</summary>
    public VcsIntegrationSettings Settings { get; set; } = new();
}
