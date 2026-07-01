#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps a stored auth policy to its wire DTO. Shared by GetAuthPolicy and UpdateAuthPolicy so a
/// persisted update reads back identically.</summary>
public static class AuthPolicyMapper
{
    public static AuthPolicy ToWire(StoredAuthPolicy policy) => new()
    {
        Id = policy.PolicyId,
        Version = policy.Version,
        Created = policy.Created.ToString("o"),
        Modified = policy.Modified.ToString("o"),
        Policies = policy.Policies,
    };
}
