#nullable enable

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Real XML-DSig <see cref="ISamlAssertionValidator"/> backed by <see cref="SignedXml"/>. Loads the response
/// into an <see cref="XmlDocument"/> with whitespace preserved (canonicalization is whitespace-sensitive),
/// verifies the enveloped <c>ds:Signature</c> against the configured IdP certificate, then reads the subject
/// NameID and the <c>email</c>/<c>mail</c> attribute from the SAML assertion namespace.
/// </summary>
public sealed class SamlAssertionValidator : ISamlAssertionValidator
{
    private const string Assertion = "urn:oasis:names:tc:SAML:2.0:assertion";
    private const string Dsig = "http://www.w3.org/2000/09/xmldsig#";

    public SamlAssertionResult Validate(string samlResponseXml, X509Certificate2 idpCert)
    {
        try
        {
            return ValidateInternal(samlResponseXml, idpCert);
        }
        catch (Exception ex)
        {
            return new SamlAssertionResult(false, null, null, $"SAML validation failed: {ex.Message}");
        }
    }

    private static SamlAssertionResult ValidateInternal(string xml, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        if (!SignatureIsValid(doc, cert, out var error))
            return new SamlAssertionResult(false, null, null, error);
        var nameId = NameId(doc);
        return new SamlAssertionResult(true, nameId, Email(doc, nameId), null);
    }

    private static bool SignatureIsValid(XmlDocument doc, X509Certificate2 cert, out string? error)
    {
        var signatures = doc.GetElementsByTagName("Signature", Dsig);
        if (signatures.Count == 0)
        {
            error = "SAML response has no <ds:Signature> element.";
            return false;
        }
        var signedXml = new SignedXml(doc);
        signedXml.LoadXml((XmlElement)signatures[0]!);
        if (signedXml.CheckSignature(cert, verifySignatureOnly: true))
        {
            error = null;
            return true;
        }
        error = "SAML signature does not verify against the configured IdP certificate.";
        return false;
    }

    private static string? NameId(XmlDocument doc)
    {
        var nodes = doc.GetElementsByTagName("NameID", Assertion);
        return nodes.Count > 0 ? nodes[0]!.InnerText.Trim() : null;
    }

    private static string? Email(XmlDocument doc, string? nameId)
    {
        var attribute = AttributeValue(doc, "email") ?? AttributeValue(doc, "mail");
        if (!string.IsNullOrWhiteSpace(attribute))
            return attribute;
        return nameId is not null && nameId.Contains('@') ? nameId : null;
    }

    private static string? AttributeValue(XmlDocument doc, string name)
    {
        foreach (XmlElement attr in doc.GetElementsByTagName("Attribute", Assertion))
        {
            if (attr.GetAttribute("Name") != name && attr.GetAttribute("FriendlyName") != name)
                continue;
            var values = attr.GetElementsByTagName("AttributeValue", Assertion);
            if (values.Count > 0)
                return values[0]!.InnerText.Trim();
        }
        return null;
    }
}
