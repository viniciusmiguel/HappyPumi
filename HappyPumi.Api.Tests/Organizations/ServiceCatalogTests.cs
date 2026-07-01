using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the services catalog endpoints (org-admin PR3) against the real Postgres-backed
/// <see cref="IServiceStore"/>: the service is seeded through the store (resolved from a request scope) and
/// then exercised end-to-end — GET/PATCH metadata, POST/DELETE items, HEAD existence probe, DELETE (204 then
/// 404). Items round-trip through the <c>"itemType:itemName"</c> encoding. Unique org per test for isolation.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ServiceCatalogTests(HappyPumiApp app)
{
    private const string Owner = "user/alice";

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task GetReturnsSeededService()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "handles invoices");

        var response = await client.GetFromJsonAsync<GetServiceResponse>(Path(org, "billing"));

        Assert.NotNull(response);
        Assert.Equal("billing", response!.Service.Name);
        Assert.Equal(org, response.Service.OrganizationName);
        Assert.Equal("handles invoices", response.Service.Description);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task GetUnknownServiceIs404()
    {
        using var client = app.CreateClient();
        using var missing = await client.GetAsync(Path(NewOrg(), "ghost"));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PatchUpdatesDescription()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "old");

        using var patched = await client.PatchAsJsonAsync(Path(org, "billing"), new { description = "new" });
        patched.EnsureSuccessStatusCode();
        var service = await patched.Content.ReadFromJsonAsync<Service>();
        Assert.Equal("new", service!.Description);

        var reread = await client.GetFromJsonAsync<GetServiceResponse>(Path(org, "billing"));
        Assert.Equal("new", reread!.Service.Description);
    }

    [Fact]
    public async Task PatchUnknownServiceIs404()
    {
        using var client = app.CreateClient();
        using var patched = await client.PatchAsJsonAsync(Path(NewOrg(), "ghost"), new { description = "x" });
        Assert.Equal(HttpStatusCode.NotFound, patched.StatusCode);
    }

    [Fact]
    public async Task AddItemsThenRemoveItemRoundTrips()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "");

        using var added = await client.PostAsJsonAsync(Path(org, "billing") + "/items", new
        {
            items = new[] { new { type = "stack", name = "dev" }, new { type = "environment", name = "prod" } },
        });
        added.EnsureSuccessStatusCode();

        var afterAdd = await client.GetFromJsonAsync<GetServiceResponse>(Path(org, "billing"));
        Assert.Equal(2, afterAdd!.Items.Count);
        Assert.Contains(afterAdd.Items, i => i.Type == "stack" && i.Name == "dev");
        Assert.Equal(1, afterAdd.Service.ItemCountSummary["environment"]);

        using var removed = await client.DeleteAsync(Path(org, "billing") + "/items/stack/dev");
        removed.EnsureSuccessStatusCode();
        var afterRemove = await removed.Content.ReadFromJsonAsync<GetServiceResponse>();
        Assert.DoesNotContain(afterRemove!.Items, i => i.Type == "stack");
        Assert.Single(afterRemove.Items);
    }

    [Fact]
    public async Task RemoveUnknownItemIs404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "");

        using var removed = await client.DeleteAsync(Path(org, "billing") + "/items/stack/absent");
        Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);
    }

    [Fact]
    public async Task HeadReflectsExistence()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "");

        using var present = await client.SendAsync(Head(Path(org, "billing")));
        Assert.Equal(HttpStatusCode.OK, present.StatusCode);

        using var absent = await client.SendAsync(Head(Path(org, "ghost")));
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
    }

    [Fact]
    public async Task DeleteRemovesServiceThenGetIs404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, "billing", "Billing", "");

        using var deleted = await client.DeleteAsync(Path(org, "billing"));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var gone = await client.GetAsync(Path(org, "billing"));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private static HttpRequestMessage Head(string url) => new(HttpMethod.Head, url);

    private static string Path(string org, string service) => $"/api/orgs/{org}/services/{Owner}/{service}";

    private void Seed(string org, string name, string displayName, string description)
    {
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IServiceStore>();
        store.Create(org, name, displayName, description);
    }
}
