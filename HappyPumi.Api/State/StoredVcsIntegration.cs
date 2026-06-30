#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>
/// A provider-neutral VCS integration record (ADR-0009). One row per connected GitHub /
/// GitHub Enterprise / Azure DevOps account; per-provider contract shapes are projected from this by
/// <c>VcsIntegrationMapper</c>. External provider calls (OAuth, repo listing) land in PR2/PR3.
/// </summary>
public sealed class StoredVcsIntegration
{
    public required string Id { get; init; }
    public required string Org { get; init; }

    /// <summary>"github" | "github-enterprise" | "azure-devops".</summary>
    public required string Kind { get; set; }

    public string? Name { get; set; }

    /// <summary>GitHub Enterprise / Azure DevOps base url.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>GitHub org/account login or Azure DevOps organization name.</summary>
    public string? AccountName { get; set; }
    public long? AccountId { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>Azure DevOps project name.</summary>
    public string? AzureProject { get; set; }

    /// <summary>
    /// Write-only OAuth/PAT access token for the connected account (ADR-0009). Set by
    /// <c>CompleteAzureDevOpsOAuth</c> via <see cref="IVcsIntegrationStore.SetCredential"/>; the providers
    /// use it as the bearer for real VCS REST calls. Never projected into a response contract.
    /// </summary>
    public string? Credential { get; set; }

    public VcsIntegrationSettings Settings { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

/// <summary>PR-comment / AI-review toggles shared across the VCS providers (jsonb on the row).</summary>
public sealed class VcsIntegrationSettings
{
    public bool DisableDetailedDiff { get; set; }
    public bool DisablePrComments { get; set; }
    public bool DisableNeoSummaries { get; set; }
    public bool DisableCodeAccessForReviews { get; set; }
}
