#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Builds the untyped deployment values returned by ExportStack.</summary>
public static class DeploymentFactory
{
    /// <summary>Latest deployment schema version the CLI understands (pulumi DeploymentSchemaVersionLatest).</summary>
    private const long SchemaVersion = 3;

    /// <summary>
    /// The empty deployment returned for a never-deployed stack. It must be a valid, deserializable
    /// document: the CLI's UnmarshalUntypedDeployment rejects version 0 ("too old"), so a fresh export
    /// is version 3 with an empty (resourceless) deployment body rather than an absent one.
    /// </summary>
    public static AppUntypedDeployment Empty() => new()
    {
        Version = SchemaVersion,
        Deployment = new Dictionary<string, object?>(),
    };
}
