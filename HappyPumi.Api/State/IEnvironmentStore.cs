#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for ESC environments (ENDPOINTS.md — Pulumi ESC): the definition (YAML) plus its
/// revision history and metadata. Backed by PostgreSQL (ADR-0005).
/// </summary>
public interface IEnvironmentStore
{
    /// <summary>All environments in an org (the ESC Environments list).</summary>
    IReadOnlyList<StoredEnvironment> ListByOrg(string org);
    StoredEnvironment? Get(EnvCoordinates coordinates);
    /// <summary>Creates an environment with an initial empty definition + revision 1. Null when it already exists.</summary>
    StoredEnvironment? Create(EnvCoordinates coordinates, string ownerLogin, string ownerName);
    /// <summary>Replaces the definition, recording a new revision. Returns the updated environment, or null.</summary>
    StoredEnvironment? UpdateYaml(EnvCoordinates coordinates, string yaml, string editorLogin, string editorName);
    IReadOnlyList<StoredEnvRevision> ListRevisions(EnvCoordinates coordinates);
    bool Delete(EnvCoordinates coordinates);
}
