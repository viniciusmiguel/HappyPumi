using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Locally generated SAML fixtures for the SAML/SSO tests: a self-signed signing certificate, IdP metadata
/// XML embedding it, and a SAML response signed (enveloped XML-DSig) with that certificate's private key —
/// so the tests exercise the real <c>SamlAssertionValidator</c> signature path end to end.
/// </summary>
public static class SamlTestFixtures
{
    /// <summary>A fresh self-signed RSA certificate (private key attached, usable for signing).</summary>
    public static X509Certificate2 GenerateSigningCert()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=happypumi-idp-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    /// <summary>The base64 DER (X509ContentType.Cert) of the certificate's public part.</summary>
    public static string Base64Der(X509Certificate2 cert)
        => Convert.ToBase64String(cert.Export(X509ContentType.Cert));

    /// <summary>IdP metadata (EntityDescriptor) embedding <paramref name="cert"/> as the signing certificate.</summary>
    public static string MetadataXml(X509Certificate2 cert, string entityId, string ssoUrl, string validUntil)
        => $"""
            <EntityDescriptor xmlns="urn:oasis:names:tc:SAML:2.0:metadata" entityID="{entityId}" validUntil="{validUntil}">
              <IDPSSODescriptor protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol">
                <KeyDescriptor use="signing">
                  <KeyInfo xmlns="http://www.w3.org/2000/09/xmldsig#">
                    <X509Data><X509Certificate>{Base64Der(cert)}</X509Certificate></X509Data>
                  </KeyInfo>
                </KeyDescriptor>
                <NameIDFormat>urn:oasis:names:tc:SAML:2.0:nameid-format:emailAddress</NameIDFormat>
                <SingleSignOnService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect" Location="{ssoUrl}"/>
              </IDPSSODescriptor>
            </EntityDescriptor>
            """;

    /// <summary>A SAML response carrying the NameID + email, signed with an enveloped XML-DSig signature.</summary>
    public static string SignedResponseXml(X509Certificate2 cert, string nameId, string email)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(ResponseTemplate(nameId, email));
        var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        signedXml.ComputeSignature();
        doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
        return doc.OuterXml;
    }

    public static string Base64(string xml) => Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));

    private static string ResponseTemplate(string nameId, string email)
        => $"""
            <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" ID="_resp" Version="2.0">
              <saml:Assertion ID="_assert" Version="2.0">
                <saml:Subject>
                  <saml:NameID Format="urn:oasis:names:tc:SAML:2.0:nameid-format:emailAddress">{nameId}</saml:NameID>
                </saml:Subject>
                <saml:AttributeStatement>
                  <saml:Attribute Name="email"><saml:AttributeValue>{email}</saml:AttributeValue></saml:Attribute>
                </saml:AttributeStatement>
              </saml:Assertion>
            </samlp:Response>
            """;
}
