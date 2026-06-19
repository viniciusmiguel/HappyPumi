// Implemented by hand (ESC rotators). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListRotators — the registered <c>fn::rotate</c> rotators available to environments.</summary>
public sealed class ListRotatorsEndpoint(IEscRotatorRegistry rotators)
    : Endpoint<ListRotatorsRequest, ListRotatorsResponse>
{
    public override void Configure()
    {
        Get("/api/esc/rotators");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListRotators")
            .WithName("ListRotators")
        );
    }

    public override async Task HandleAsync(ListRotatorsRequest req, CancellationToken ct)
    {
        var response = new ListRotatorsResponse { Rotators = rotators.All.Select(r => r.Name).ToList() };
        await Send.OkAsync(response, ct);
    }
}
