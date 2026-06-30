#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IAccessTokenStore"/> (ADR-0005), keyed by "scope/ownerKey". Used by unit tests.</summary>
public sealed class InMemoryAccessTokenStore : IAccessTokenStore
{
    private readonly ConcurrentDictionary<string, List<StoredAccessToken>> _byOwner = new();

    private static string Key(string scope, string ownerKey) => $"{scope}/{ownerKey}";

    private List<StoredAccessToken> Bucket(string scope, string ownerKey)
        => _byOwner.GetOrAdd(Key(scope, ownerKey), _ => new List<StoredAccessToken>());

    public StoredAccessToken Issue(
        string scope, string ownerKey, string name, string description, string createdBy,
        out string plaintext, long expires = 0, bool admin = false, string? roleId = null)
    {
        var (value, hash) = AccessTokenSecret.Generate();
        plaintext = value;
        var token = new StoredAccessToken
        {
            Id = Guid.NewGuid().ToString(), Name = name, Description = description, Scope = scope,
            OwnerKey = ownerKey, HashedValue = hash, CreatedBy = createdBy, Created = DateTime.UtcNow,
            Expires = expires, Admin = admin, RoleId = roleId,
        };
        var list = Bucket(scope, ownerKey);
        lock (list)
            list.Add(token);
        return token;
    }

    public IReadOnlyList<StoredAccessToken> List(string scope, string ownerKey)
    {
        var list = Bucket(scope, ownerKey);
        lock (list)
            return list.OrderByDescending(t => t.Created).ToArray();
    }

    public IReadOnlyList<StoredAccessToken> ListByRole(string org, string roleId)
        => List("org", org).Where(t => t.RoleId == roleId).ToArray();

    public bool Delete(string scope, string ownerKey, string id)
    {
        var list = Bucket(scope, ownerKey);
        lock (list)
            return list.RemoveAll(t => t.Id == id) > 0;
    }
}
