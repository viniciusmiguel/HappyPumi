#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Registry;

/// <summary>
/// PUT .../templates/{source}/{publisher}/{name}/versions/{version}/upload/archive — the pre-signed upload
/// target the CLI PUTs the template tarball (.tar.gz) to. Bodyless so FastEndpoints doesn't consume the
/// gzip stream during binding.
/// </summary>
public sealed class PutTemplateArchiveEndpoint(IArtifactStore artifacts) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Put("/api/registry/templates/{source}/{publisher}/{name}/versions/{version}/upload/archive");
        AllowAnonymous(); // pre-signed upload target (the publish handshake authorizes the version)
        Description(b => b.WithTags("Registry").WithSummary("PutTemplateArchive").WithName("PutTemplateArchive"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (source, publisher, name, version) = (
            Route<string>("source")!, Route<string>("publisher")!, Route<string>("name")!, Route<string>("version")!);
        using var ms = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(ms, ct);
        var contentType = HttpContext.Request.ContentType ?? "application/gzip";
        artifacts.Put(ArtifactKeys.TemplateArchive(source, publisher, name, version), ms.ToArray(), contentType);
        // The template-publish CLI treats a 204 as a failure, so respond 200 OK.
        await Send.ResultAsync(Microsoft.AspNetCore.Http.Results.Ok());
    }
}

/// <summary>
/// GET .../templates/{source}/{publisher}/{name}/versions/{version}/archive — serves the published template
/// tarball (the workflow runner / CLI fetches it to materialize the template). "latest" resolves the newest.
/// </summary>
public sealed class GetTemplateArchiveEndpoint(IArtifactStore artifacts, ITemplateRegistry registry) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/registry/templates/{source}/{publisher}/{name}/versions/{version}/archive");
        AllowAnonymous(); // fetched by the runner/CLI to materialize the template
        Description(b => b.WithTags("Registry").WithSummary("GetTemplateArchive").WithName("GetTemplateArchive"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (source, publisher, name, version) = (
            Route<string>("source")!, Route<string>("publisher")!, Route<string>("name")!, Route<string>("version")!);
        if (version == "latest")
        {
            var latest = registry.Get(new TemplateCoordinates(source, publisher, name), "latest");
            if (latest is not null) version = latest.Version;
        }
        var artifact = artifacts.Get(ArtifactKeys.TemplateArchive(source, publisher, name, version));
        if (artifact is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.BytesAsync(artifact.Content, fileName: $"{name}-{version}.tar.gz",
            contentType: artifact.ContentType, cancellation: ct);
    }
}
