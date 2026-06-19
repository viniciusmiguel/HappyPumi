#nullable enable

using System;
using System.Text.RegularExpressions;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps a policy-violation engine event (<see cref="AppPolicyEvent"/>) to a stored finding.</summary>
public static partial class PolicyFindingMapper
{
    // Pulumi colorizes engine-event messages with "<{%...%}>" control tags; strip them for stored findings.
    [GeneratedRegex(@"<\{%[^%]*%\}>")]
    private static partial Regex ColorTags();

    public static PolicyViolationV2 FromEvent(string project, string stack, AppPolicyEvent e)
    {
        var (type, name) = ParseUrn(e.ResourceUrn);
        return new PolicyViolationV2
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = "update",
            Level = e.EnforcementLevel,
            Message = ColorTags().Replace(e.Message ?? "", "").Trim(),
            ObservedAt = DateTime.UtcNow,
            PolicyName = e.PolicyName,
            PolicyPack = e.PolicyPackName,
            PolicyPackTag = string.IsNullOrEmpty(e.PolicyPackVersionTag) ? e.PolicyPackVersion : e.PolicyPackVersionTag,
            ProjectName = project,
            StackName = stack,
            ResourceUrn = e.ResourceUrn ?? "",
            ResourceType = type,
            ResourceName = name,
        };
    }

    /// <summary>Pulls the resource type + name out of a URN (urn:pulumi:{stack}::{project}::{type}::{name}).</summary>
    private static (string Type, string Name) ParseUrn(string? urn)
    {
        if (string.IsNullOrEmpty(urn))
            return ("", "");
        var parts = urn.Split("::");
        var type = parts.Length >= 3 ? parts[^2] : "";
        var name = parts.Length >= 4 ? parts[^1] : "";
        return (type, name);
    }
}
