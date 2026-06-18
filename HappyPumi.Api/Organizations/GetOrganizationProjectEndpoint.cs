#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

public sealed class GetOrganizationProjectRequest
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
}

// Console-only response shapes for the project page (getOrganizationProject). The console reads
// project.stacks[].{name,stackName,resourceCount,version,lastUpdate,tags} and writes lastUpdate.info.
public sealed class ConsoleLastUpdate
{
    public long Version { get; set; }
    public string Result { get; set; } = "succeeded";
    public string Kind { get; set; } = "update";
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public long Time { get; set; }
    public Contracts.UserInfo RequestedBy { get; set; } = default!;
}

public sealed class ConsoleStackSummary
{
    public string OrgName { get; set; } = default!;
    public string ProjectName { get; set; } = default!;
    public string StackName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long ResourceCount { get; set; }
    public long Version { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public ConsoleLastUpdate? LastUpdate { get; set; }
}

public sealed class ConsoleProject
{
    public string OrgName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<ConsoleStackSummary> Stacks { get; set; } = new();
}

public sealed class GetOrganizationProjectResponse
{
    public ConsoleProject Project { get; set; } = new();
    public string? ContinuationToken { get; set; }
}

/// <summary>
/// GET /api/console/orgs/{orgName}/projects/{projectName} — the console project-detail page. Returns the
/// project with its stacks, each carrying a complete <c>lastUpdate</c> derived from the stack's latest
/// update history and a resource count from its checkpoint.
/// </summary>
public sealed class GetOrganizationProjectEndpoint(IStackStore stacks) : Endpoint<GetOrganizationProjectRequest, GetOrganizationProjectResponse>
{
    public override void Configure()
    {
        Get("/api/console/orgs/{orgName}/projects/{projectName}");
        Permissions("stack:read");
        Description(b => b.WithTags("Organizations").WithSummary("GetOrganizationProject").WithName("GetOrganizationProject"));
    }

    public override async Task HandleAsync(GetOrganizationProjectRequest req, CancellationToken ct)
    {
        var owner = new Contracts.UserInfo { GithubLogin = req.OrgName, Name = req.OrgName, AvatarUrl = "" };
        var summaries = stacks.All()
            .Where(s => s.Coordinates.Org == req.OrgName && s.Coordinates.Project == req.ProjectName)
            .OrderBy(s => s.Coordinates.Stack)
            .Select(s => ToSummary(s, owner))
            .ToList();

        await Send.OkAsync(new GetOrganizationProjectResponse
        {
            Project = new ConsoleProject { OrgName = req.OrgName, Name = req.ProjectName, Stacks = summaries },
            ContinuationToken = null,
        }, ct);
    }

    private static ConsoleStackSummary ToSummary(StoredStack s, Contracts.UserInfo owner)
    {
        var latest = s.History.LastOrDefault()?.Info;
        ConsoleLastUpdate? lastUpdate = latest is null ? null : new ConsoleLastUpdate
        {
            Version = latest.Version, Result = latest.Result ?? "succeeded", Kind = latest.Kind ?? "update",
            StartTime = latest.StartTime, EndTime = latest.EndTime, Time = latest.EndTime, RequestedBy = owner,
        };
        return new ConsoleStackSummary
        {
            OrgName = s.Coordinates.Org, ProjectName = s.Coordinates.Project,
            StackName = s.Coordinates.Stack, Name = s.Coordinates.Stack,
            ResourceCount = StackResources.Extract(s.Deployment).Count, Version = s.Version,
            Tags = s.Tags, LastUpdate = lastUpdate,
        };
    }
}
