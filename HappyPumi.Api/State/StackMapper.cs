#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps the <see cref="StoredStack"/> domain record to the <c>AppStack</c> wire DTO.</summary>
public static class StackMapper
{
    public static AppStack ToAppStack(StoredStack stack) => new()
    {
        Id = stack.Coordinates.Qualified,
        OrgName = stack.Coordinates.Org,
        ProjectName = stack.Coordinates.Project,
        StackName = stack.Coordinates.Stack,
        Version = stack.Version,
        Tags = new Dictionary<string, string>(stack.Tags),
        Config = stack.Config,
        // No update is in flight until the lifecycle endpoints land (ENDPOINTS.md 1c).
        ActiveUpdate = string.Empty,
        CurrentOperation = null,
    };
}
