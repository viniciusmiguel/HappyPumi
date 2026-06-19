// Implemented by hand (ESC schedules). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>The schedule create/update payload (the generator self-referenced the request type for the body).</summary>
public sealed class EnvironmentScheduleBody
{
    public string? Kind { get; set; }
    public string? ScheduleCron { get; set; }
    public string? ScheduleOnce { get; set; }
    public Dictionary<string, object>? Definition { get; set; }
}

/// <summary>Request for CreateEnvironmentSchedule with the correct body type.</summary>
public sealed class CreateScheduleInput
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
    [BindFrom("envName")] public string EnvName { get; set; } = default!;
    [FromBody] public EnvironmentScheduleBody Body { get; set; } = default!;
}

/// <summary>CreateEnvironmentSchedule — adds a scheduled action (cron or one-shot) to an environment.</summary>
public sealed class CreateEnvironmentScheduleEndpoint(IEnvironmentStore environments, IEscScheduleStore schedules)
    : Endpoint<CreateScheduleInput, ScheduledAction>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/schedules");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CreateEnvironmentSchedule")
            .WithName("CreateEnvironmentSchedule")
        );
    }

    public override async Task HandleAsync(CreateScheduleInput req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (environments.Get(coords) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTime.UtcNow.ToString("o");
        var action = schedules.Add(coords, new ScheduledAction
        {
            Id = Guid.NewGuid().ToString("N"),
            Created = now, Modified = now, LastExecuted = "", NextExecution = "",
            OrgId = req.OrgName, Kind = req.Body?.Kind ?? "rotation", Paused = false,
            ScheduleCron = req.Body?.ScheduleCron, ScheduleOnce = req.Body?.ScheduleOnce,
            Definition = req.Body?.Definition ?? new Dictionary<string, object>(),
        });
        await Send.OkAsync(action, ct);
    }
}
