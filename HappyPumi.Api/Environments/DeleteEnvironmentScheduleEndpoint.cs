// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>DeleteEnvironmentSchedule — removes a scheduled action (404 when unknown).</summary>
public sealed class DeleteEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<DeleteEnvironmentScheduleRequest>
{
    public override void Configure()
    {
        Delete("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules/{scheduleID}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DeleteEnvironmentSchedule")
            .WithName("DeleteEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(DeleteEnvironmentScheduleRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (!schedules.Remove(coords, req.ScheduleId))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
