#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted SAML/SSO configuration for an org (ENDPOINTS.md settings cluster, PR5). Holds the raw IdP
/// metadata XML plus the fields derived from it (entity id, SSO URL, signing certificate, NameID format,
/// validity) and the list of SAML admins. <see cref="Certificate"/> is the base64 DER of the IdP signing
/// cert and is what <c>ISamlAssertionValidator</c> verifies inbound assertions against.
/// </summary>
public sealed class StoredSamlConfig
{
    public required string Org { get; init; }
    public string IdpMetadataXml { get; set; } = "";
    public string? EntityId { get; set; }
    public string? SsoUrl { get; set; }
    public string? Certificate { get; set; }   // base64 DER of the IdP signing cert
    public string? NameIdFormat { get; set; }
    public string? ValidUntil { get; set; }
    public string? ValidationError { get; set; }
    public bool Enabled { get; set; }
    public List<string> Admins { get; set; } = new();
}

/// <summary>
/// Persistence seam for per-org SAML configuration (ADR-0005). Backed by PostgreSQL in production and an
/// in-memory map in unit tests. Keyed by org.
/// </summary>
public interface ISamlConfigStore
{
    /// <summary>The org's SAML config, or null when none is configured.</summary>
    StoredSamlConfig? Get(string org);

    /// <summary>Creates or replaces the config for <c>config.Org</c>, returning the stored record.</summary>
    StoredSamlConfig Upsert(StoredSamlConfig config);

    /// <summary>The org's SAML admin logins (empty when no config).</summary>
    IReadOnlyList<string> ListAdmins(string org);

    /// <summary>Adds an admin login to the org's config. False when no config exists yet.</summary>
    bool AddAdmin(string org, string userLogin);
}
