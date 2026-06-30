#nullable enable

using System;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Maps a persisted <see cref="OidcIssuerRow"/> to the wire <see cref="OidcIssuerRegistrationResponse"/>.
/// Timestamps are emitted as ISO 8601 ("o"). Example: <c>OidcIssuerMapper.ToResponse(row)</c>.
/// </summary>
public static class OidcIssuerMapper
{
    public static OidcIssuerRegistrationResponse ToResponse(OidcIssuerRow row) => new()
    {
        Id = row.Id,
        Issuer = row.Url,
        Name = row.Name,
        Url = row.Url,
        Thumbprints = row.Thumbprints,
        MaxExpiration = row.MaxExpiration,
        Created = Iso(row.Created),
        Modified = Iso(row.Modified),
        LastUsed = Iso(row.LastUsed),
    };

    private static string? Iso(DateTime? when) => when?.ToString("o");
}
