using System.Net.Http.Headers;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Auth;

/// <summary>
/// Verifies endpoints enforce RBAC permissions: anonymous callers are rejected, members get read access,
/// and write actions require the matching grant.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class RbacEnforcementTests(HappyPumiApp app)
{
    private const string Org = "happypumi";

    [Fact]
    public async Task AnonymousRequestToPermissionedEndpointIsRejected()
    {
        using var client = app.CreateClient(); // no token

        using var res = await client.GetAsync($"/api/orgs/{Org}/deployments");

        Assert.True(res.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCanReadDeployments()
    {
        using var client = app.CreateAuthedClient(); // token -> admin -> all permissions

        using var res = await client.GetAsync($"/api/orgs/{Org}/deployments");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task MemberCanReadButCannotCreateEnvironment()
    {
        // The "role:member:<login>" token convention grants the read/list subset only.
        using var client = app.CreateAuthedClient("role:member:carol");

        using var list = await client.GetAsync($"/api/esc/environments/{Org}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode); // environment:list granted

        using var create = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = "p", Name = $"e-{System.Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode); // environment:create NOT granted
    }

    [Fact]
    public async Task PermissionsEndpointReturnsTheGrantSet()
    {
        using var client = app.CreateAuthedClient();

        var perms = await client.GetFromJsonAsync<List<string>>($"/api/console/orgs/{Org}/permissions");

        Assert.Contains("stack:read", perms!);
        Assert.Contains("environment:create", perms);
    }
}
