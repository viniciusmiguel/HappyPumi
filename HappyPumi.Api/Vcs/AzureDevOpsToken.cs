#nullable enable

using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Vcs;

/// <summary>
/// Resolves the Azure DevOps access token an org's integration records carry (stored by
/// <c>CompleteAzureDevOpsOAuth</c>). The access-status / org / project endpoints share this, so the
/// token lookup lives in one place rather than being duplicated per endpoint.
/// </summary>
public static class AzureDevOpsToken
{
    /// <summary>The first non-empty credential on the org's azure-devops integrations, or null.</summary>
    public static string? For(IVcsIntegrationStore store, string org)
        => store.List(org, "azure-devops")
            .Select(i => i.Credential)
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
}
