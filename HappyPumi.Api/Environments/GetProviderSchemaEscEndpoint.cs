// Implemented by hand (ESC providers). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>GetProviderSchema — the input/output JSON-Schema for one <c>fn::open</c> provider.</summary>
public sealed class GetProviderSchemaEscEndpoint(IEscProviderRegistry providers)
    : Endpoint<GetProviderSchemaEscRequest, ProviderSchema>
{
    public override void Configure()
    {
        Get("/api/esc/providers/{providerName}/schema");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("GetProviderSchema")
            .WithName("GetProviderSchemaEsc")
        );
    }

    public override async Task HandleAsync(GetProviderSchemaEscRequest req, CancellationToken ct)
    {
        if (!providers.TryGet(req.ProviderName, out var provider))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var schema = new ProviderSchema
        {
            Name = provider.Name,
            Description = provider.Description,
            Inputs = provider.Inputs,
            Outputs = provider.Outputs,
        };
        await Send.OkAsync(schema, ct);
    }
}
