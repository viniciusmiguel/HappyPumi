// Implemented by hand (ESC rotators). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>GetRotatorSchema — the input/output JSON-Schema for one <c>fn::rotate</c> rotator.</summary>
public sealed class GetRotatorSchemaEndpoint(IEscRotatorRegistry rotators)
    : Endpoint<GetRotatorSchemaRequest, ProviderSchema>
{
    public override void Configure()
    {
        Get("/api/esc/rotators/{rotatorName}/schema");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("GetRotatorSchema")
            .WithName("GetRotatorSchema")
        );
    }

    public override async Task HandleAsync(GetRotatorSchemaRequest req, CancellationToken ct)
    {
        if (!rotators.TryGet(req.RotatorName, out var rotator))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var schema = new ProviderSchema
        {
            Name = rotator.Name,
            Description = rotator.Description,
            Inputs = rotator.Inputs,
            Outputs = rotator.Outputs,
        };
        await Send.OkAsync(schema, ct);
    }
}
