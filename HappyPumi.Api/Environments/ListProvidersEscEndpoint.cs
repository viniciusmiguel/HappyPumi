// Implemented by hand (ESC providers). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListProviders — the registered <c>fn::open</c> providers available to environments.</summary>
public sealed class ListProvidersEscEndpoint(IEscProviderRegistry providers)
    : Endpoint<ListProvidersEscRequest, ListProvidersResponse>
{
    public override void Configure()
    {
        Get("/api/esc/providers");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListProviders")
            .WithName("ListProvidersEsc")
        );
    }

    public override async Task HandleAsync(ListProvidersEscRequest req, CancellationToken ct)
    {
        var response = new ListProvidersResponse { Providers = providers.All.Select(p => p.Name).ToList() };
        await Send.OkAsync(response, ct);
    }
}
