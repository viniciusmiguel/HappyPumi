using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests asserting ESC environment mutations emit audit events (ADR-0010).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscAuditEventsTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-audit";

    [Fact]
    public async Task EnvironmentLifecycleEmitsAuditEventsForEachMutation()
    {
        using var client = app.CreateAuthedClient();
        var name = $"env-{Guid.NewGuid():N}";
        var url = $"/api/esc/environments/{Org}/{Project}/{name}";

        (await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name })).EnsureSuccessStatusCode();
        (await Patch(client, url, "values:\n  greeting: hello\n")).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"{url}/open", new { })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"{url}/rotate", new { })).EnsureSuccessStatusCode();
        (await client.DeleteAsync(url)).EnsureSuccessStatusCode();

        // Each mutation recorded an audit event naming this (uniquely named) environment.
        var audit = await client.GetFromJsonAsync<ResponseAuditLogs>($"/api/orgs/{Org}/auditlogs");
        var mine = audit!.AuditLogEvents.Where(e => e.Description!.Contains(name)).Select(e => e.Event).ToList();
        Assert.Contains("environment.create", mine);
        Assert.Contains("environment.update", mine);
        Assert.Contains("environment.open", mine);
        Assert.Contains("environment.rotate", mine);
        Assert.Contains("environment.delete", mine);
    }

    // UpdateEnvironment reads the raw YAML body; the CLI sends it with an application/json content type.
    private static Task<HttpResponseMessage> Patch(HttpClient client, string url, string yaml)
        => client.PatchAsync(url, new StringContent(yaml, Encoding.UTF8, "application/json"));
}
