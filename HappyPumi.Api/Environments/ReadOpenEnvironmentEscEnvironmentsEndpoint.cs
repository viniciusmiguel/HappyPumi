// Implemented by hand (ESC open lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ReadOpenEnvironment — returns the resolved property tree for an open session, or a single value when
/// the <c>property</c> query parameter supplies a dot-separated path.
/// </summary>
public sealed class ReadOpenEnvironmentEscEnvironmentsEndpoint(IEscSessionStore sessions)
    : Endpoint<ReadOpenEnvironmentEscEnvironmentsRequest, object>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/open/{openSessionID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadOpenEnvironment")
            .WithName("ReadOpenEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(ReadOpenEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var session = sessions.Get(req.OpenSessionId);
        if (session is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (string.IsNullOrEmpty(req.Property))
        {
            await Send.OkAsync(new EscEnvironment { Properties = session.Properties }, ct);
            return;
        }

        var value = ValueAtPath(session.Properties, req.Property);
        if (value is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(value, ct);
    }

    // Walk the resolved tree by dotted path; each level is a map of EscValue under EscValue.Value.
    private static EscValue? ValueAtPath(Dictionary<string, EscValue> properties, string path)
    {
        var map = properties;
        EscValue? current = null;
        foreach (var segment in path.Split('.'))
        {
            if (map is null || !map.TryGetValue(segment, out current))
                return null;
            map = current.Value as Dictionary<string, EscValue>;
        }
        return current;
    }
}
