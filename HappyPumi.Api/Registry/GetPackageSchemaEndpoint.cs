#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Registry;

public sealed class GetPackageSchemaRequest
{
    [BindFrom("source")] public string Source { get; set; } = default!;
    [BindFrom("publisher")] public string Publisher { get; set; } = default!;
    [BindFrom("name")] public string Name { get; set; } = default!;
    [BindFrom("version")] public string Version { get; set; } = default!;
}

/// <summary>
/// GET .../packages/{source}/{publisher}/{name}/versions/{version}/schema — serves the published package
/// schema (the CLI fetches this during `pulumi package add` to generate a typed SDK). 404 when absent.
/// </summary>
public sealed class GetPackageSchemaEndpoint(IArtifactStore artifacts, IPackageRegistry registry) : Endpoint<GetPackageSchemaRequest>
{
    public override void Configure()
    {
        Get("/api/registry/packages/{source}/{publisher}/{name}/versions/{version}/schema");
        AllowAnonymous(); // fetched by the CLI/tooling during package resolution
        Description(b => b.WithTags("Registry").WithSummary("GetPackageSchema").WithName("GetPackageSchema"));
    }

    public override async Task HandleAsync(GetPackageSchemaRequest req, CancellationToken ct)
    {
        // "latest" resolves to the newest version's schema.
        var version = req.Version;
        if (version == "latest")
        {
            var latest = registry.Get(new PackageCoordinates(req.Source, req.Publisher, req.Name), "latest");
            if (latest is not null) version = latest.Version;
        }
        var artifact = artifacts.Get(ArtifactKeys.Package(req.Source, req.Publisher, req.Name, version, "schema"));
        if (artifact is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.BytesAsync(artifact.Content, contentType: artifact.ContentType, cancellation: ct);
    }
}
