using System;
using System.Net.Http;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for project-template resolution (templates PR1). A template version is seeded into the
/// registry's Postgres store, then GET <c>/api/orgs/{org}/template?name=...</c> resolves and maps it, and
/// <c>/template/readme</c> returns its description. An unknown selector is a 404 (template) / empty (readme).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ProjectTemplateTests(HappyPumiApp app)
{
    private const string Readme = "# Widget template\nProvisions a widget.";

    /// <summary>Seeds one published template version directly into the registry store; returns its name.</summary>
    private string SeedTemplate()
    {
        var name = $"tmpl{Guid.NewGuid():N}";
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPumiDbContext>();
        db.Templates.Add(new TemplateVersionRow
        {
            Source = "private", Publisher = "acme", Name = name, Version = "1.0.0",
            UpdatedAt = DateTime.UtcNow, Language = "go", Description = Readme, Published = true,
        });
        db.SaveChanges();
        return name;
    }

    [Fact]
    public async Task ResolvesSeededTemplateByName()
    {
        using var client = app.CreateClient();
        var name = SeedTemplate();

        var template = await client.GetFromJsonAsync<Template>($"/api/orgs/acme/template?name={name}");

        Assert.NotNull(template);
        Assert.Equal(name, template!.Name);
        Assert.Equal("go", template.Language);
    }

    [Fact]
    public async Task ReadmeReturnsTemplateDescription()
    {
        using var client = app.CreateClient();
        var name = SeedTemplate();

        var readme = await client.GetStringAsync($"/api/orgs/acme/template/readme?name={name}");

        Assert.Equal(Readme, readme);
    }

    [Fact]
    public async Task UnknownTemplateIsNotFound()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"/api/orgs/acme/template?name=missing-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfigurationReturnsSourceDestination()
    {
        using var client = app.CreateClient();
        var org = $"org-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync($"/api/orgs/{org}/templates/sources", new UpsertOrgTemplateSourceRequest
        {
            Name = "team", SourceUrl = "https://example.com/t.git", DestinationUrl = "https://dest.example.com",
        });

        var config = await client.GetFromJsonAsync<GetTemplateConfigurationResponse>(
            $"/api/orgs/{org}/template/configuration?name=team");

        Assert.NotNull(config);
        Assert.Equal("https://dest.example.com", config!.Destination!.Url);
    }
}
