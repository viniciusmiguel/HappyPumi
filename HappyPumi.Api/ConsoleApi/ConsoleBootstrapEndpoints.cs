#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;

namespace HappyPumi.Api.ConsoleApi;

// Internal endpoints the Pulumi web console calls to bootstrap that are NOT in the public OpenAPI spec.
// Reverse-engineered black-box by running the prebuilt pulumi/console image against HappyPumi and watching
// which calls it makes (see workspace research/, ADR-0008). Permissive/minimal responses so the console's
// app shell loads; refine shapes as the console reveals more requirements.

/// <summary>GET /api/user/console-settings — per-user console preferences. Defaults are fine.</summary>
public sealed class GetConsoleSettingsEndpoint : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/api/user/console-settings");
        AllowAnonymous();
        Description(b => b.WithTags("Console").WithName("GetConsoleSettings"));
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(new { }, ct);
}
