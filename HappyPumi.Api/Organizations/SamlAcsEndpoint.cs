#nullable enable

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// SAML Assertion Consumer Service — POST /api/orgs/{orgName}/saml/acs. The SP-side endpoint the IdP POSTs the
/// signed assertion to (form field <c>SAMLResponse</c>, base64). It cryptographically verifies the enveloped
/// XML-DSig signature against the org's configured signing certificate and, on success, establishes a session
/// consistent with ADR-0007 by minting a user-scoped access token for the validated user via
/// <see cref="IAccessTokenStore"/>. Returns 400 when SAML is not enabled and 401 on an invalid assertion.
/// </summary>
public sealed class SamlAcsEndpoint(
    ISamlConfigStore store, ISamlAssertionValidator validator, IAccessTokenStore tokens)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/orgs/{orgName}/saml/acs");
        AllowAnonymous();
        Description(b => b
            .WithTags("Organizations")
            .WithSummary("SamlAssertionConsumerService")
            .WithName("SamlAcs"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var config = store.Get(org);
        if (config is null || !config.Enabled || string.IsNullOrWhiteSpace(config.Certificate))
        {
            AddError("SAML SSO is not enabled for this organization.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        await ValidateAndRespond(org, config, ct);
    }

    private async Task ValidateAndRespond(string org, StoredSamlConfig config, CancellationToken ct)
    {
        var result = Validate(config, await ReadSamlResponse(ct));
        if (!result.Valid || string.IsNullOrWhiteSpace(result.NameId))
        {
            AddError(result.Error ?? "SAML assertion is missing a NameID.", "SAMLResponse");
            await Send.ErrorsAsync(401, ct);
            return;
        }
        await Send.OkAsync(BuildSession(org, config, result), ct);
    }

    private SamlAssertionResult Validate(StoredSamlConfig config, string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return new SamlAssertionResult(false, null, null, "Missing or undecodable 'SAMLResponse' form field.");
        var cert = LoadCertificate(config.Certificate!);
        if (cert is null)
            return new SamlAssertionResult(false, null, null, "Stored IdP certificate is not valid base64 DER.");
        return validator.Validate(xml, cert);
    }

    private object BuildSession(string org, StoredSamlConfig config, SamlAssertionResult result)
    {
        var login = result.NameId!;
        tokens.Issue("user", login, "SAML SSO session",
            $"Established via SAML for org '{org}'.", login, out var plaintext);
        return new
        {
            user = new { name = login, email = result.Email, githubLogin = login },
            accessToken = plaintext,
            samlAdmin = config.Admins.Contains(login),
        };
    }

    private async Task<string> ReadSamlResponse(CancellationToken ct)
    {
        var form = await HttpContext.Request.ReadFormAsync(ct);
        return DecodeBase64(form["SAMLResponse"].ToString());
    }

    private static string DecodeBase64(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return "";
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            return "";
        }
    }

    private static X509Certificate2? LoadCertificate(string base64Der)
    {
        try
        {
            return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64Der));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
