#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// PUT /api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/upload — the pre-signed upload
/// target the CLI PUTs the compressed policy pack (.tgz) to. Bodyless so FastEndpoints doesn't consume the
/// gzip stream during binding (same pattern as the package/template artifact uploads).
/// </summary>
public sealed class PutPolicyPackArtifactEndpoint(IArtifactStore artifacts) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Put("/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/upload");
        AllowAnonymous(); // pre-signed upload target (the publish handshake authorizes the version)
        Description(b => b.WithTags("Organizations").WithSummary("PutPolicyPackArtifact").WithName("PutPolicyPackArtifact"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (org, name, version) = (Route<string>("orgName")!, Route<string>("policyPackName")!, Route<long>("version"));
        using var ms = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(ms, ct);
        var contentType = HttpContext.Request.ContentType ?? "application/gzip";
        artifacts.Put(ArtifactKeys.PolicyPack(org, name, version), ms.ToArray(), contentType);
        await Send.OkAsync(new { }, ct);
    }
}

/// <summary>
/// GET /api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/download — serves the published
/// policy-pack tarball. The Pulumi engine fetches this to apply the pack to a stack during an update.
/// </summary>
public sealed class GetPolicyPackArtifactEndpoint(IArtifactStore artifacts) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/download");
        AllowAnonymous(); // fetched by the engine/runner to materialize the pack
        Description(b => b.WithTags("Organizations").WithSummary("GetPolicyPackArtifact").WithName("GetPolicyPackArtifact"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (org, name, version) = (Route<string>("orgName")!, Route<string>("policyPackName")!, Route<long>("version"));
        var artifact = artifacts.Get(ArtifactKeys.PolicyPack(org, name, version));
        if (artifact is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.BytesAsync(artifact.Content, fileName: $"{name}-{version}.tgz",
            contentType: artifact.ContentType, cancellation: ct);
    }
}
