#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for stack annotations (ADR-0005): a free-form structured payload attached to a stack
/// and keyed by an annotation <c>kind</c> (e.g. compliance tracking, custom metadata, integration data).
/// The default is in-memory like the other stores; a PostgreSQL implementation drops in behind this
/// interface. All operations are safe for concurrent use.
/// </summary>
public interface IStackAnnotationStore
{
    /// <summary>Returns the stored annotation payload for (stack, kind), or null when none is set.</summary>
    object? Get(StackCoordinates coordinates, string kind);

    /// <summary>Creates or overwrites the annotation payload for (stack, kind).</summary>
    void Set(StackCoordinates coordinates, string kind, object payload);
}
