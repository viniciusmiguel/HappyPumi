// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>PauseEnvironmentSchedule — pauses a scheduled action (404 when unknown).</summary>
public sealed class PauseEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<PauseEnvironmentScheduleRequest>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules/{scheduleID}/pause");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("PauseEnvironmentSchedule")
            .WithName("PauseEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(PauseEnvironmentScheduleRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (schedules.Mutate(coords, req.ScheduleId, a => a.Paused = true) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
