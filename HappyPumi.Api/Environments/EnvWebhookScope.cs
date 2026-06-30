#nullable enable

using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Maps the legacy <c>/api/preview/environments/{org}/{envName}</c> webhook route to <see cref="EnvCoordinates"/>
/// and to the shared <see cref="WebhookScope"/> that partitions delivery history. Preview environments predate
/// ESC projects, so an unqualified <c>envName</c> resolves to the <c>default</c> project; a <c>project/name</c>
/// value is split on the first slash to stay compatible with project-qualified names.
/// </summary>
public static class EnvWebhookScope
{
    public static EnvCoordinates Coords(string org, string envName)
    {
        var slash = envName.IndexOf('/');
        return slash < 0
            ? new EnvCoordinates(org, "default", envName)
            : new EnvCoordinates(org, envName[..slash], envName[(slash + 1)..]);
    }

    /// <summary>The delivery scope key (<c>environment:org/project/name</c>) shared by every env-webhook caller.</summary>
    public static WebhookScope For(EnvCoordinates c) => new("environment", $"{c.Org}/{c.Project}/{c.Name}");
}
