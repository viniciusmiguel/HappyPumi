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
    /// <summary>Soft-deletes the environment (restorable via <see cref="Restore"/>). False when it does not exist.</summary>
    bool Delete(EnvCoordinates coordinates);
    /// <summary>Restores a soft-deleted environment. Returns it, or null when no deleted environment matches.</summary>
    StoredEnvironment? Restore(EnvCoordinates coordinates);

    /// <summary>Toggles deletion protection. Returns the updated environment, or null when it does not exist.</summary>
    StoredEnvironment? SetDeletionProtected(EnvCoordinates coordinates, bool deletionProtected);
    /// <summary>Reassigns the owner. Returns the updated environment, or null when it does not exist.</summary>
    StoredEnvironment? ReassignOwner(EnvCoordinates coordinates, string ownerLogin, string ownerName);
    /// <summary>Sets (creates or replaces) a single environment tag. Returns the updated environment, or null.</summary>
    StoredEnvironment? SetTag(EnvCoordinates coordinates, string name, string value);
    /// <summary>Removes an environment tag. Returns false when the environment or tag does not exist.</summary>
    bool DeleteTag(EnvCoordinates coordinates, string name);

    /// <summary>
    /// Points a revision tag at a revision (a tag name is unique across revisions, so it is moved if it
    /// already exists). Returns the target revision, or null when that revision number does not exist.
    /// </summary>
    StoredEnvRevision? SetRevisionTag(EnvCoordinates coordinates, string name, long revision);
    /// <summary>Removes a revision tag from whichever revision holds it. False when no revision has it.</summary>
    bool DeleteRevisionTag(EnvCoordinates coordinates, string name);

    /// <summary>
    /// Marks a revision retracted (withdrawn). Returns the updated revision, or null when that revision
    /// number does not exist.
    /// </summary>
    StoredEnvRevision? RetractRevision(EnvCoordinates coordinates, long version, string? reason, long? replacement,
        string byLogin, string byName);
}
