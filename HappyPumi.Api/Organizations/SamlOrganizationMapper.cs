#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>Maps a <see cref="StoredSamlConfig"/> (+ its org) onto the wire <see cref="SamlOrganization"/>.</summary>
public static class SamlOrganizationMapper
{
    /// <summary>
    /// Builds the response for an org. When <paramref name="config"/> is null the org has no SAML config:
    /// returns an empty <c>IdpSsoDescriptor</c> and null SAML fields alongside the populated organization.
    /// </summary>
    public static SamlOrganization ToSamlOrganization(StoredSamlConfig? config, string org)
    {
        var organization = BuildOrganization(org);
        if (config is null)
            return new SamlOrganization { IdpSsoDescriptor = "", Organization = organization };
        return new SamlOrganization
        {
            IdpSsoDescriptor = config.IdpMetadataXml,
            EntityId = config.EntityId,
            SsoUrl = config.SsoUrl,
            NameIdFormat = config.NameIdFormat,
            ValidUntil = config.ValidUntil,
            ValidationError = config.ValidationError,
            Organization = organization,
        };
    }

    private static Organization BuildOrganization(string org) => new()
    {
        Name = org, GithubLogin = org, AvatarUrl = "", Repos = new List<PulumiRepository>(),
    };
}
