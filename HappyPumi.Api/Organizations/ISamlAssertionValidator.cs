#nullable enable

using System.Security.Cryptography.X509Certificates;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>Outcome of verifying a SAML response: validity plus the extracted NameID/email (or an error).</summary>
public sealed record SamlAssertionResult(bool Valid, string? NameId, string? Email, string? Error);

/// <summary>
/// Verifies the enveloped XML-DSig signature on a SAML response/assertion against an IdP certificate and
/// extracts the subject NameID + email attribute. Implementations NEVER throw — any failure (no signature,
/// bad signature, malformed XML) returns <c>Valid=false</c> with the reason in <see cref="SamlAssertionResult.Error"/>.
/// </summary>
public interface ISamlAssertionValidator
{
    SamlAssertionResult Validate(string samlResponseXml, X509Certificate2 idpCert);
}
