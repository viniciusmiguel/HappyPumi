#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted SAML/SSO configuration. Key: Org (one config per org). <see cref="Admins"/> is jsonb (a
/// list of user logins). <see cref="Certificate"/> is the base64 DER of the IdP signing certificate.
/// </summary>
public sealed class SamlConfigRow
{
    public string Org { get; set; } = default!;
    public string IdpMetadataXml { get; set; } = "";
    public string? EntityId { get; set; }
    public string? SsoUrl { get; set; }
    public string? Certificate { get; set; }
    public string? NameIdFormat { get; set; }
    public string? ValidUntil { get; set; }
    public string? ValidationError { get; set; }
    public bool Enabled { get; set; }
    public List<string> Admins { get; set; } = new();
}
