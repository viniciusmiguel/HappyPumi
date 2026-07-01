#nullable enable

using System;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Translates between the <see cref="TemplateSource"/> wire contract and the <see cref="StoredTemplateSource"/>
/// persistence shape, and runs the deterministic (no-network) source-URL validation used on create/update
/// (templates PR1). Validation is kept pure so component/unit tests are stable.
/// </summary>
internal static class TemplateSourceMapper
{
    /// <summary>Maps a stored source to its <see cref="TemplateSource"/> output contract.</summary>
    public static TemplateSource ToContract(StoredTemplateSource s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        SourceUrl = s.SourceUrl,
        DestinationUrl = s.DestinationUrl,
        Destination = s.DestinationUrl is null ? null : new TemplateDestination { Url = s.DestinationUrl },
        IsValid = s.IsValid,
        Error = s.Error,
    };

    /// <summary>Reads a create/update request's fields into a stored source and re-validates the URL.</summary>
    public static void ApplyInput(StoredTemplateSource s, UpsertOrgTemplateSourceRequest body)
    {
        s.Name = body.Name;
        s.SourceUrl = body.SourceUrl;
        s.DestinationUrl = body.Destination?.Url ?? body.DestinationUrl;
        var (isValid, error) = Validate(body.SourceUrl);
        s.IsValid = isValid;
        s.Error = error;
    }

    /// <summary>
    /// A well-formed source URL is an absolute http(s) URL. No network fetch is performed — validity is a pure
    /// function of the string so tests are deterministic.
    /// </summary>
    public static (bool IsValid, string? Error) Validate(string sourceUrl)
    {
        var wellFormed = Uri.TryCreate(sourceUrl, UriKind.Absolute, out var u)
            && (u!.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);
        return wellFormed
            ? (true, null)
            : (false, $"sourceURL must be an absolute http(s) URL, got '{sourceUrl}'");
    }
}
