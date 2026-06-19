// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListEnvironmentSchedule — the scheduled actions on an environment.</summary>
public sealed class ListEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<ListEnvironmentScheduleRequest, ListScheduledActionsResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListEnvironmentSchedule")
            .WithName("ListEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(ListEnvironmentScheduleRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        await Send.OkAsync(new ListScheduledActionsResponse { Schedules = schedules.List(coords).ToList() }, ct);
    }
}
