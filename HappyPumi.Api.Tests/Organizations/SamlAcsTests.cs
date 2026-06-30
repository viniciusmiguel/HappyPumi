using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// The crux of PR5: the SP-side ACS endpoint and the real XML-DSig <see cref="SamlAssertionValidator"/>. A
/// SAML response is signed in-test with a locally generated certificate; the org's SAML config is seeded with
/// that certificate's public DER. A correctly signed assertion yields 200 + a freshly minted user access
/// token; a tampered assertion (broken signature) yields 401. A direct validator unit test covers the valid
/// path and the wrong-certificate (signature mismatch) path.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class SamlAcsTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    private void SeedConfig(string org, X509Certificate2 cert, params string[] admins)
    {
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISamlConfigStore>();
        store.Upsert(new StoredSamlConfig
        {
            Org = org, Enabled = true, Certificate = SamlTestFixtures.Base64Der(cert),
            IdpMetadataXml = "<seeded/>", Admins = admins.ToList(),
        });
    }

    private static HttpContent Form(string samlResponse)
        => new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("SAMLResponse", samlResponse) });

    [Fact]
    public async Task SignedAssertionEstablishesSessionWithToken()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        using var cert = SamlTestFixtures.GenerateSigningCert();
        SeedConfig(org, cert, "alice@example.com");
        var signed = SamlTestFixtures.SignedResponseXml(cert, "alice@example.com", "alice@example.com");

        using var resp = await client.PostAsync(
            $"/api/orgs/{org}/saml/acs", Form(SamlTestFixtures.Base64(signed)));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("pul-", json.GetProperty("accessToken").GetString());
        Assert.Equal("alice@example.com", json.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("alice@example.com", json.GetProperty("user").GetProperty("email").GetString());
        Assert.True(json.GetProperty("samlAdmin").GetBoolean());
    }

    [Fact]
    public async Task TamperedAssertionIsRejectedWith401()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        using var cert = SamlTestFixtures.GenerateSigningCert();
        SeedConfig(org, cert);
        var signed = SamlTestFixtures.SignedResponseXml(cert, "alice@example.com", "alice@example.com");
        var tampered = signed.Replace("alice", "mallory"); // breaks the signed digest

        using var resp = await client.PostAsync(
            $"/api/orgs/{org}/saml/acs", Form(SamlTestFixtures.Base64(tampered)));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task AcsIs400WhenSamlNotEnabled()
    {
        using var client = app.CreateAuthedClient();
        using var resp = await client.PostAsync(
            $"/api/orgs/{NewOrg()}/saml/acs", Form("ignored"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public void ValidatorAcceptsCorrectlySignedAssertion()
    {
        using var cert = SamlTestFixtures.GenerateSigningCert();
        var signed = SamlTestFixtures.SignedResponseXml(cert, "bob@example.com", "bob@example.com");

        var result = new SamlAssertionValidator().Validate(signed, PublicOnly(cert));

        Assert.True(result.Valid, result.Error);
        Assert.Equal("bob@example.com", result.NameId);
        Assert.Equal("bob@example.com", result.Email);
    }

    [Fact]
    public void ValidatorRejectsSignatureFromADifferentCert()
    {
        using var signingCert = SamlTestFixtures.GenerateSigningCert();
        using var otherCert = SamlTestFixtures.GenerateSigningCert();
        var signed = SamlTestFixtures.SignedResponseXml(signingCert, "bob@example.com", "bob@example.com");

        var result = new SamlAssertionValidator().Validate(signed, PublicOnly(otherCert));

        Assert.False(result.Valid);
        Assert.NotNull(result.Error);
    }

    // The validator only ever has the IdP's public certificate; load it as the endpoint does.
    private static X509Certificate2 PublicOnly(X509Certificate2 cert)
        => X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
}
