#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for stack state (ADR-0005). The default implementation is in-memory so the IaC
/// workflow runs offline in tests and local dev; a PostgreSQL-backed implementation can be dropped in
/// behind this interface without touching the endpoints. All operations are safe for concurrent use.
/// </summary>
public interface IStackStore
{
    /// <summary>True when at least one stack exists under <paramref name="org"/>/<paramref name="project"/>.</summary>
    bool ProjectExists(string org, string project);

    /// <summary>Returns the stored stack, or null when it does not exist.</summary>
    StoredStack? Find(StackCoordinates coordinates);

    /// <summary>Creates the stack. Returns false (creating nothing) when one already exists at those coordinates.</summary>
    bool TryCreate(StoredStack stack);

    /// <summary>Removes the stack. Returns false when no stack exists at those coordinates.</summary>
    bool Delete(StackCoordinates coordinates);

    /// <summary>Replaces the stack's service-managed config. Returns the updated stack, or null when it does not exist.</summary>
    StoredStack? SetConfig(StackCoordinates coordinates, AppStackConfig config);

    /// <summary>Clears the stack's service-managed config. Returns false when the stack does not exist.</summary>
    bool ClearConfig(StackCoordinates coordinates);
}
