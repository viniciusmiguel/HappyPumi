#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Resolves the policy packs the Pulumi engine must download and enforce for an org's stacks. The org's
/// default policy group ("default-policy-group") applies to every stack; each enabled pack's newest
/// published version is returned with a <c>packLocation</c> download URL the engine fetches. Used both by
/// the update-create responses (where the engine reads requiredPolicies) and GetStackPolicyPacks.
/// </summary>
public static class PolicyEnforcement
{
    public const string DefaultGroup = "default-policy-group";

    public static List<AppRequiredPolicy> RequiredPolicies(IPolicyStore policy, string org, string baseUrl)
    {
        var group = policy.GetGroup(org, DefaultGroup);
        if (group is null)
            return new List<AppRequiredPolicy>();

        return group.AppliedPolicyPacks
            .Select(packName => (packName, pack: policy.GetPack(org, packName)))
            .Select(x => (x.packName, x.pack, latest: NewestPublished(x.pack)))
            .Where(x => x.latest is not null)
            .Select(x => new AppRequiredPolicy
            {
                Name = x.packName,
                DisplayName = string.IsNullOrEmpty(x.pack!.DisplayName) ? x.packName : x.pack.DisplayName,
                Version = x.latest!.Version,
                VersionTag = x.latest.VersionTag ?? x.latest.Version.ToString(),
                PackLocation = $"{baseUrl}/api/orgs/{org}/policypacks/{x.packName}/versions/{x.latest.Version}/download",
            })
            .ToList();
    }

    private static StoredPolicyPackVersion? NewestPublished(StoredPolicyPack? pack)
        => pack?.Versions.Values.Where(v => v.Published).OrderByDescending(v => v.Version).FirstOrDefault();
}
