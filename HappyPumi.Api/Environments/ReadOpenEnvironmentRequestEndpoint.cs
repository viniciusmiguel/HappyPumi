// Implemented by hand (ESC open-request workflow). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ReadOpenEnvironmentRequest — the requested access/grant durations (404 when the request is unknown).</summary>
public sealed class ReadOpenEnvironmentRequestEndpoint(IEscOpenRequestStore requests)
    : Endpoint<ReadOpenEnvironmentRequestRequest, CreateEnvironmentOpenRequest>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/open/request/{changeRequestID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadOpenEnvironmentRequest")
            .WithName("ReadOpenEnvironmentRequest")
        );
    }

    public override async Task HandleAsync(ReadOpenEnvironmentRequestRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var request = requests.Get(coords, req.ChangeRequestId);
        if (request is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(new CreateEnvironmentOpenRequest
        {
            AccessDurationSeconds = request.AccessDurationSeconds,
            GrantExpirationSeconds = request.GrantExpirationSeconds,
        }, ct);
    }
}
