#nullable enable

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>The fields HappyPumi derives from an IdP's SAML metadata (<c>EntityDescriptor</c>) XML.</summary>
public sealed record SamlMetadata(
    string? EntityId, string? SsoUrl, string? Certificate, string? NameIdFormat, string? ValidUntil);

/// <summary>
/// Parses SAML IdP metadata (an <c>md:EntityDescriptor</c>/<c>md:IDPSSODescriptor</c>) with System.Xml.Linq to
/// extract the entity id, single-sign-on URL, signing certificate (base64 DER), NameID format and validity.
/// Throws <see cref="FormatException"/> (with the offending XML) when the document is not recognisable SAML
/// metadata; callers capture the message as a validation error rather than failing the request.
/// </summary>
public static class SamlMetadataParser
{
    private static readonly XNamespace Md = "urn:oasis:names:tc:SAML:2.0:metadata";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";

    public static SamlMetadata Parse(string metadataXml)
    {
        var root = Root(metadataXml)
            ?? throw new FormatException($"SAML metadata XML has no root element: '{Excerpt(metadataXml)}'.");
        var idp = root.Descendants(Md + "IDPSSODescriptor").FirstOrDefault()
            ?? throw new FormatException(
                $"SAML metadata is missing an <md:IDPSSODescriptor>: '{Excerpt(metadataXml)}'.");
        return new SamlMetadata(
            EntityId: (string?)root.Attribute("entityID"),
            SsoUrl: (string?)idp.Elements(Md + "SingleSignOnService").FirstOrDefault()?.Attribute("Location"),
            Certificate: SigningCertificate(idp),
            NameIdFormat: (string?)idp.Elements(Md + "NameIDFormat").FirstOrDefault(),
            ValidUntil: (string?)root.Attribute("validUntil"));
    }

    // Wrap malformed-XML failures as FormatException so callers store one validation error path uniformly.
    private static XElement? Root(string metadataXml)
    {
        try
        {
            return XDocument.Parse(metadataXml).Root;
        }
        catch (XmlException ex)
        {
            throw new FormatException($"SAML metadata XML is not well-formed: {ex.Message}. Got: '{Excerpt(metadataXml)}'.", ex);
        }
    }

    // The base64 DER often arrives pretty-printed across lines; strip whitespace so it round-trips as DER.
    private static string? SigningCertificate(XElement idp)
    {
        var raw = idp.Descendants(Ds + "X509Certificate").FirstOrDefault()?.Value;
        return raw is null ? null : new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private static string Excerpt(string xml)
        => xml.Length <= 120 ? xml : xml[..120] + "…";
}
