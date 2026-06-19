using System.Net.Http.Headers;
using HappyPumi.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace HappyPumi.Api.Tests;

/// <summary>
/// Boots the real HappyPumi API in-process against a throwaway PostgreSQL container (ADR-0005 — all state
/// is persisted to Postgres now). The container starts once per collection; the API applies its EF
/// migration on startup. Requires a running Docker daemon.
/// </summary>
public sealed class HappyPumiApp : WebApplicationFactory<ApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine").Build();

    async Task IAsyncLifetime.InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseSetting("ConnectionStrings:happypumidb", _db.GetConnectionString());

    /// <summary>
    /// A client that authenticates with the Pulumi <c>token</c> scheme (ADR-0007). The default token maps
    /// to the seeded admin identity; pass <c>role:&lt;role&gt;:&lt;login&gt;</c> to exercise other roles.
    /// </summary>
    public HttpClient CreateAuthedClient(string token = "dev")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
        return client;
    }
}

/// <summary>
/// Shares one <see cref="HappyPumiApp"/> across a test class so the host (and its Postgres container) is
/// built once. Use the collection name on test classes: <c>[Collection(HappyPumiCollection.Name)]</c>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class HappyPumiCollection : ICollectionFixture<HappyPumiApp>
{
    public const string Name = "happypumi-api";
}
