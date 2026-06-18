#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>An organization membership: a user login mapped to an RBAC role (ADR-0007).</summary>
public sealed class StoredMember
{
    public required string UserLogin { get; init; }

    /// <summary>The member's org role (e.g. <c>admin</c>, <c>member</c>); mutable via UpdateOrganizationMember.</summary>
    public required string Role { get; set; }

    public DateTime Created { get; init; } = DateTime.UtcNow;
}
