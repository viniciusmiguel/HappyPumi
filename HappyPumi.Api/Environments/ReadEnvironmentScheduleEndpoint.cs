// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ReadEnvironmentSchedule — a single scheduled action (404 when unknown).</summary>
public sealed class ReadEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<ReadEnvironmentScheduleRequest, ScheduledAction>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules/{scheduleID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadEnvironmentSchedule")
            .WithName("ReadEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(ReadEnvironmentScheduleRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var action = schedules.Get(coords, req.ScheduleId);
        if (action is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(action, ct);
    }
}
