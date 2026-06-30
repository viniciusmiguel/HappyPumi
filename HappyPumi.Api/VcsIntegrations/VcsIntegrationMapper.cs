#nullable enable

using System;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.VcsIntegrations;

/// <summary>
/// Projects provider-neutral <see cref="StoredVcsIntegration"/> records into the per-provider wire shapes
/// (summary / GitHub details / Azure DevOps details). PR1 reports static "has access / valid" flags; the
/// real provider-permission probes land in PR2/PR3 (ADR-0009).
/// </summary>
public static class VcsIntegrationMapper
{
    public static VcsIntegrationSummary ToSummary(StoredVcsIntegration i) => new()
    {
        Id = i.Id,
        Name = i.Name,
        VcsProvider = i.Kind,
        BaseUrl = i.BaseUrl,
        Host = HostOf(i.BaseUrl),
        AvatarUrl = i.AvatarUrl,
        HasIndividualAccess = true,
    };

    public static GitHubIntegrationDetails ToGitHubDetails(StoredVcsIntegration i) => new()
    {
        Id = i.Id,
        AccountId = i.AccountId,
        AccountName = i.AccountName,
        AvatarUrl = i.AvatarUrl,
        Created = i.Created,
        IsSelfHosted = i.Kind == "github-enterprise",
        DisableDetailedDiff = i.Settings.DisableDetailedDiff,
        DisablePrComments = i.Settings.DisablePrComments,
        DisableNeoSummaries = i.Settings.DisableNeoSummaries,
        DisableCodeAccessForReviews = i.Settings.DisableCodeAccessForReviews,
        HasContentsPermission = true,
        HasMembersPermission = true,
    };

    public static AzureDevOpsIntegrationDetails ToAzureDetails(StoredVcsIntegration i) => new()
    {
        Id = i.Id,
        Organization = AzureOrg(i),
        Project = AzureProject(i),
        DisableDetailedDiff = i.Settings.DisableDetailedDiff,
        DisablePrComments = i.Settings.DisablePrComments,
        DisableNeoSummaries = i.Settings.DisableNeoSummaries,
        DisableCodeAccessForReviews = i.Settings.DisableCodeAccessForReviews,
        Valid = true,
    };

    public static AzureDevOpsAppIntegrationResponse ToAzureAppResponse(StoredVcsIntegration i) => new()
    {
        Organization = AzureOrg(i),
        Project = AzureProject(i),
        DisableDetailedDiff = i.Settings.DisableDetailedDiff,
        DisablePrComments = i.Settings.DisablePrComments,
        DisableNeoSummaries = i.Settings.DisableNeoSummaries,
        Installed = true,
        Valid = true,
    };

    private static AzureDevOpsOrganization AzureOrg(StoredVcsIntegration i) => new()
    {
        Name = i.AccountName ?? "",
        AccountUrl = i.BaseUrl,
        HasRequiredPermissions = true,
    };

    private static AzureDevOpsProject AzureProject(StoredVcsIntegration i) => new()
    {
        Name = i.AzureProject ?? "",
    };

    /// <summary>Extracts the hostname for self-hosted providers (GitHub Enterprise / ADO base url).</summary>
    private static string? HostOf(string? baseUrl)
        => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.Host : null;
}
