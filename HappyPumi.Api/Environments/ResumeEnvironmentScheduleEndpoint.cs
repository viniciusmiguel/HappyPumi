// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ResumeEnvironmentSchedule — resumes a paused scheduled action (404 when unknown).</summary>
public sealed class ResumeEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<ResumeEnvironmentScheduleRequest>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules/{scheduleID}/resume");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ResumeEnvironmentSchedule")
            .WithName("ResumeEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(ResumeEnvironmentScheduleRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (schedules.Mutate(coords, req.ScheduleId, a => a.Paused = false) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
