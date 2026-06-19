#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Registry;

/// <summary>
/// PUT .../packages/{source}/{publisher}/{name}/versions/{version}/upload/{kind} — the pre-signed upload
/// target the CLI PUTs each publish artifact to. Stores the raw bytes in the artifact store; the matching
/// download endpoints (schema/readme/installation) serve them. Uses no request DTO so FastEndpoints does
/// not consume the body during model binding (the schema is uploaded as application/json).
/// </summary>
public sealed class PutPackageArtifactEndpoint(IArtifactStore artifacts) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Put("/api/registry/packages/{source}/{publisher}/{name}/versions/{version}/upload/{kind}");
        AllowAnonymous(); // pre-signed upload target (the publish handshake itself authorizes the version)
        Description(b => b.WithTags("Registry").WithSummary("PutPackageArtifact").WithName("PutPackageArtifact"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (source, publisher, name, version, kind) = (
            Route<string>("source")!, Route<string>("publisher")!, Route<string>("name")!,
            Route<string>("version")!, Route<string>("kind")!);

        using var ms = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(ms, ct);
        var contentType = HttpContext.Request.ContentType ?? "application/octet-stream";
        artifacts.Put(ArtifactKeys.Package(source, publisher, name, version, kind), ms.ToArray(), contentType);
        await Send.NoContentAsync(ct);
    }
}
