#nullable enable

using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Sets the response headers the esc/pulumi CLI reads when getting or updating an environment: the revision
/// number (<c>Pulumi-ESC-Revision</c>, parsed as an int) and an <c>ETag</c> for optimistic concurrency. The
/// CLI fails to parse the response without these (see pulumi/esc client GetEnvironment).
/// </summary>
public static class EscHeaders
{
    public const string RevisionHeader = "Pulumi-ESC-Revision";

    public static void SetRevision(HttpContext http, long revision)
    {
        http.Response.Headers[RevisionHeader] = revision.ToString();
        http.Response.Headers.ETag = $"\"{revision}\"";
    }
}
