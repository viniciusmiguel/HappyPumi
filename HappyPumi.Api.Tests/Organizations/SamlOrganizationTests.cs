using System;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the Settings-cluster PR5 SAML/SSO spec endpoints against the real Postgres-backed
/// store: PATCH /saml with valid IdP metadata is reflected by GET; malformed metadata is stored with a
/// ValidationError (not a 500); and the admin POST/GET round-trip surfaces the user as a UserInfo. Unique
/// org per test so they stay independent.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class SamlOrganizationTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task PatchValidMetadataIsReflectedByGet()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        using var cert = SamlTestFixtures.GenerateSigningCert();
        var metadata = SamlTestFixtures.MetadataXml(
            cert, "https://idp.example.com/entity", "https://idp.example.com/sso", "2030-01-01T00:00:00Z");

        using var patch = await client.PatchAsJsonAsync($"/api/orgs/{org}/saml",
            new { newIdpSsoDescriptor = metadata });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var saml = await client.GetFromJsonAsync<SamlOrganization>($"/api/orgs/{org}/saml");
        Assert.Equal("https://idp.example.com/entity", saml!.EntityId);
        Assert.Equal("https://idp.example.com/sso", saml.SsoUrl);
        Assert.Equal("2030-01-01T00:00:00Z", saml.ValidUntil);
        Assert.Null(saml.ValidationError);
        Assert.Equal(org, saml.Organization.Name);
    }

    [Fact]
    public async Task GetUnconfiguredOrgReturnsEmptyDescriptor()
    {
        using var client = app.CreateAuthedClient();
        var saml = await client.GetFromJsonAsync<SamlOrganization>($"/api/orgs/{NewOrg()}/saml");

        Assert.NotNull(saml);
        Assert.Equal("", saml!.IdpSsoDescriptor);
        Assert.Null(saml.SsoUrl);
        Assert.NotNull(saml.Organization);
    }

    [Fact]
    public async Task PatchMalformedMetadataStoresValidationErrorNot500()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        using var patch = await client.PatchAsJsonAsync($"/api/orgs/{org}/saml",
            new { newIdpSsoDescriptor = "<EntityDescriptor><not-closed>" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var saml = await patch.Content.ReadFromJsonAsync<SamlOrganization>();
        Assert.NotNull(saml!.ValidationError);
        Assert.Null(saml.SsoUrl);
    }

    [Fact]
    public async Task AddAdminThenListShowsUserInfo()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        using var cert = SamlTestFixtures.GenerateSigningCert();
        var metadata = SamlTestFixtures.MetadataXml(
            cert, "https://idp/entity", "https://idp/sso", "2030-01-01T00:00:00Z");
        await client.PatchAsJsonAsync($"/api/orgs/{org}/saml", new { newIdpSsoDescriptor = metadata });

        using var add = await client.PostAsync($"/api/orgs/{org}/saml/admins/alice", EmptyJson());
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var admins = await client.GetFromJsonAsync<ListSamlOrganizationAdminsResponse>(
            $"/api/orgs/{org}/saml/admins");
        Assert.Contains(admins!.SamlAdmins, u => u.GithubLogin == "alice" && u.Name == "alice");
    }

    [Fact]
    public async Task AddAdminWithoutConfigIs404()
    {
        using var client = app.CreateAuthedClient();
        using var add = await client.PostAsync($"/api/orgs/{NewOrg()}/saml/admins/alice", EmptyJson());
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
    }

    private static StringContent EmptyJson() => new("{}", System.Text.Encoding.UTF8, "application/json");
}
