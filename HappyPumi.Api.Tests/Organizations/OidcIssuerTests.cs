using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.Tests.Esc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the Settings-cluster PR3 OIDC-issuer endpoints (register / list / get / update / delete
/// / regenerate-thumbprints / auth-policy). They run against the real Postgres-backed issuer store. The
/// regenerate flow swaps the typed <see cref="IOidcThumbprintFetcher"/> client onto a <see cref="StubHttpHandler"/>
/// serving a discovery doc + JWKS, proving the SHA-1 thumbprint derivation end to end. Unique org per test.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OidcIssuerTests(HappyPumiApp app)
{
    private static string NewOrg() => "oidc-" + Guid.NewGuid().ToString("N");

    private static OidcIssuerRegistrationRequest Body(string name, string url, long? maxExpiration = null)
        => new() { Name = name, Url = url, MaxExpiration = maxExpiration };

    [Fact]
    public async Task RegisterListGetRoundTrip()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var registered = await RegisterOk(client, org, Body("github-actions", "https://token.actions.githubusercontent.com", 3600));
        Assert.False(string.IsNullOrWhiteSpace(registered.Id));
        Assert.Equal("https://token.actions.githubusercontent.com", registered.Issuer);
        Assert.Equal(3600, registered.MaxExpiration);

        var list = await client.GetFromJsonAsync<ListOidcIssuersResponse>($"/api/orgs/{org}/oidc/issuers");
        var only = Assert.Single(list!.OidcIssuers);
        Assert.Equal(registered.Id, only.Id);

        var fetched = await client.GetFromJsonAsync<OidcIssuerRegistrationResponse>(
            $"/api/orgs/{org}/oidc/issuers/{registered.Id}");
        Assert.Equal("github-actions", fetched!.Name);
    }

    [Fact]
    public async Task GetUnknownIssuerReturns404()
    {
        using var client = app.CreateClient();
        var resp = await client.GetAsync($"/api/orgs/{NewOrg()}/oidc/issuers/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task RegisterRejectsBlankNameOrUrl()
    {
        using var client = app.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/orgs/{NewOrg()}/oidc/issuers", Body("", "https://issuer"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdatePatchesNameAndMaxExpiration()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuer = await RegisterOk(client, org, Body("old-name", "https://issuer.example", 600));

        using var patch = await client.PatchAsJsonAsync($"/api/orgs/{org}/oidc/issuers/{issuer.Id}",
            new { name = "new-name", maxExpiration = 7200L });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var fetched = await client.GetFromJsonAsync<OidcIssuerRegistrationResponse>(
            $"/api/orgs/{org}/oidc/issuers/{issuer.Id}");
        Assert.Equal("new-name", fetched!.Name);
        Assert.Equal(7200, fetched.MaxExpiration);
        Assert.Equal("https://issuer.example", fetched.Url); // url is immutable
    }

    [Fact]
    public async Task UpdateUnknownIssuerReturns404()
    {
        using var client = app.CreateClient();
        using var patch = await client.PatchAsJsonAsync(
            $"/api/orgs/{NewOrg()}/oidc/issuers/{Guid.NewGuid():N}", new { name = "x" });
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
    }

    [Fact]
    public async Task DeleteThenGetReturns404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuer = await RegisterOk(client, org, Body("doomed", "https://issuer.example"));

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/oidc/issuers/{issuer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var gone = await client.GetAsync($"/api/orgs/{org}/oidc/issuers/{issuer.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        using var deletedAgain = await client.DeleteAsync($"/api/orgs/{org}/oidc/issuers/{issuer.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedAgain.StatusCode);
    }

    [Fact]
    public async Task GetAuthPolicyReturnsDefaultForKnownIssuer()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuer = await RegisterOk(client, org, Body("idp", "https://issuer.example"));

        var policy = await client.GetFromJsonAsync<AuthPolicy>(
            $"/api/orgs/{org}/auth/policies/oidcissuers/{issuer.Id}");
        Assert.Equal(issuer.Id, policy!.Id);
        Assert.Equal(1, policy.Version);
        Assert.NotNull(policy.Policies);
        Assert.Empty(policy.Policies);
    }

    [Fact]
    public async Task GetAuthPolicyUnknownIssuerReturns404()
    {
        using var client = app.CreateClient();
        var resp = await client.GetAsync($"/api/orgs/{NewOrg()}/auth/policies/oidcissuers/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task RegenerateThumbprintsDerivesSha1FromIssuerCertificate()
    {
        var (x5c, expectedThumbprint) = SelfSignedCert();
        var handler = new StubHttpHandler("{\"jwks_uri\":\"https://issuer.example/jwks\"}")
            .ThenRespondWith($"{{\"keys\":[{{\"x5c\":[\"{x5c}\"]}}]}}");
        using var client = FetcherStubbedClient(handler);
        var org = NewOrg();
        var issuer = await RegisterOk(client, org, Body("rotating", "https://issuer.example"));

        using var resp = await client.PostAsync(
            $"/api/orgs/{org}/oidc/issuers/{issuer.Id}/regenerate-thumbprints", EmptyJson());
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var updated = await resp.Content.ReadFromJsonAsync<OidcIssuerRegistrationResponse>();
        Assert.Equal(new[] { expectedThumbprint }, updated!.Thumbprints);
    }

    [Fact]
    public async Task RegenerateThumbprintsReturns400WhenNoThumbprintsDerivable()
    {
        var handler = new StubHttpHandler("{\"jwks_uri\":\"https://issuer.example/jwks\"}")
            .ThenRespondWith("{\"keys\":[]}");
        using var client = FetcherStubbedClient(handler);
        var org = NewOrg();
        var issuer = await RegisterOk(client, org, Body("rotating", "https://issuer.example"));

        using var resp = await client.PostAsync(
            $"/api/orgs/{org}/oidc/issuers/{issuer.Id}/regenerate-thumbprints", EmptyJson());
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public void ThumbprintComputesUppercaseSha1OfCertificate()
    {
        var (x5c, expectedThumbprint) = SelfSignedCert();
        Assert.Equal(expectedThumbprint, OidcThumbprintFetcher.Thumbprint(x5c));
    }

    private static async Task<OidcIssuerRegistrationResponse> RegisterOk(
        HttpClient client, string org, OidcIssuerRegistrationRequest body)
    {
        using var resp = await client.PostAsJsonAsync($"/api/orgs/{org}/oidc/issuers", body);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<OidcIssuerRegistrationResponse>())!;
    }

    private HttpClient FetcherStubbedClient(StubHttpHandler handler)
    {
        var factory = app.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddHttpClient<IOidcThumbprintFetcher, OidcThumbprintFetcher>()
                .ConfigurePrimaryHttpMessageHandler(() => handler)));
        return factory.CreateClient();
    }

    /// <summary>Generates a self-signed cert and returns its base64 DER (for x5c) plus its SHA-1 thumbprint.</summary>
    private static (string X5c, string Thumbprint) SelfSignedCert()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=happypumi-oidc-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        return (Convert.ToBase64String(cert.Export(X509ContentType.Cert)), cert.Thumbprint!);
    }

    private static StringContent EmptyJson() => new("{}", System.Text.Encoding.UTF8, "application/json");
}
