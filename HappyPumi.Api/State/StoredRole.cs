#nullable enable

using System;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>A custom org RBAC role (the body of the /api/orgs/{org}/roles surface, ADR-0007).</summary>
public sealed class StoredRole
{
    public required string Id { get; init; }
    public required string OrgId { get; init; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
    public bool IsOrgDefault { get; set; }

    // Mutable descriptor fields mirrored from PermissionDescriptorBase.
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ResourceType { get; set; }
    public string? UxPurpose { get; set; }
    public PermissionDescriptor? Details { get; set; }

    /// <summary>Copies the editable descriptor fields from a create/update request body.</summary>
    public void Apply(PermissionDescriptorBase descriptor)
    {
        Name = descriptor.Name;
        Description = descriptor.Description;
        ResourceType = descriptor.ResourceType;
        UxPurpose = descriptor.UxPurpose;
        Details = descriptor.Details;
    }
}
