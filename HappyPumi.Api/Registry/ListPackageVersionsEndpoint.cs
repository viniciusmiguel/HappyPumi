#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Registry;

/// <summary>Request for listing all versions of a registry package.</summary>
public sealed class ListPackageVersionsRequest
{
    [BindFrom("source")] public string Source { get; set; } = default!;
    [BindFrom("publisher")] public string Publisher { get; set; } = default!;
    [BindFrom("name")] public string Name { get; set; } = default!;
}

/// <summary>
/// Lists all versions of a package (the Private Components "Versions" tab + version selector). Not part of
/// the public spec — the web console calls GET on the versions collection; the spec only defines POST
/// (publish) there. Returns a ListPackagesResponse with the newest version flagged <c>isLatest</c>.
/// </summary>
public sealed class ListPackageVersionsEndpoint(IPackageRegistry registry) : Endpoint<ListPackageVersionsRequest, ListPackagesResponse>
{
    public override void Configure()
    {
        Get("/api/registry/packages/{source}/{publisher}/{name}/versions");
        Permissions("stack:read");
        Description(b => b.WithTags("Registry").WithSummary("ListPackageVersions").WithName("ListPackageVersions"));
    }

    public override async Task HandleAsync(ListPackageVersionsRequest req, CancellationToken ct)
    {
        var versions = registry.ListVersions(new PackageCoordinates(req.Source, req.Publisher, req.Name))
            .OrderByDescending(v => v.CreatedAt).ToList();
        var packages = new List<PackageMetadata>();
        for (var i = 0; i < versions.Count; i++)
        {
            var meta = RegistryMapper.ToMetadata(versions[i]);
            meta.IsLatest = i == 0; // newest first
            packages.Add(meta);
        }
        await Send.OkAsync(new ListPackagesResponse { Packages = packages, ContinuationToken = null }, ct);
    }
}
