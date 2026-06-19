// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>Request for UpdateEnvironmentSchedule with the correct body type.</summary>
public sealed class UpdateScheduleInput
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
    [BindFrom("envName")] public string EnvName { get; set; } = default!;
    [BindFrom("scheduleID")] public string ScheduleId { get; set; } = default!;
    [FromBody] public EnvironmentScheduleBody Body { get; set; } = default!;
}

/// <summary>UpdateEnvironmentSchedule — updates a scheduled action's timing/kind (404 when unknown).</summary>
public sealed class UpdateEnvironmentScheduleEndpoint(IEscScheduleStore schedules)
    : Endpoint<UpdateScheduleInput, ScheduledAction>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules/{scheduleID}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("UpdateEnvironmentSchedule")
            .WithName("UpdateEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(UpdateScheduleInput req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var updated = schedules.Mutate(coords, req.ScheduleId, action =>
        {
            if (req.Body?.Kind is { Length: > 0 } kind) action.Kind = kind;
            action.ScheduleCron = req.Body?.ScheduleCron ?? action.ScheduleCron;
            action.ScheduleOnce = req.Body?.ScheduleOnce ?? action.ScheduleOnce;
            if (req.Body?.Definition is { } definition) action.Definition = definition;
        });
        if (updated is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(updated, ct);
    }
}
