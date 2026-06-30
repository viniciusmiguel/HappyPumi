#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IAccessTokenStore"/> (ADR-0005): token records keyed by id, scoped by (Scope, OwnerKey).</summary>
public sealed class PostgresAccessTokenStore(HappyPumiDbContext db) : IAccessTokenStore
{
    public StoredAccessToken Issue(
        string scope, string ownerKey, string name, string description, string createdBy,
        out string plaintext, long expires = 0, bool admin = false, string? roleId = null)
    {
        var (value, hash) = AccessTokenSecret.Generate();
        plaintext = value;
        var row = new AccessTokenRow
        {
            Id = Guid.NewGuid().ToString(), Scope = scope, OwnerKey = ownerKey, Name = name,
            Description = description, HashedValue = hash, CreatedBy = createdBy, Created = DateTime.UtcNow,
            Expires = expires, Admin = admin, RoleId = roleId,
        };
        db.AccessTokens.Add(row);
        db.SaveChanges();
        return Map(row);
    }

    public IReadOnlyList<StoredAccessToken> List(string scope, string ownerKey)
        => db.AccessTokens.AsNoTracking().Where(t => t.Scope == scope && t.OwnerKey == ownerKey)
            .OrderByDescending(t => t.Created).ToList().Select(Map).ToList();

    public IReadOnlyList<StoredAccessToken> ListByRole(string org, string roleId)
        => db.AccessTokens.AsNoTracking().Where(t => t.Scope == "org" && t.OwnerKey == org && t.RoleId == roleId)
            .OrderByDescending(t => t.Created).ToList().Select(Map).ToList();

    public bool Delete(string scope, string ownerKey, string id)
    {
        var row = db.AccessTokens.FirstOrDefault(t => t.Scope == scope && t.OwnerKey == ownerKey && t.Id == id);
        if (row is null)
            return false;
        db.AccessTokens.Remove(row);
        db.SaveChanges();
        return true;
    }

    private static StoredAccessToken Map(AccessTokenRow r) => new()
    {
        Id = r.Id, Name = r.Name, Description = r.Description, Scope = r.Scope, OwnerKey = r.OwnerKey,
        HashedValue = r.HashedValue, CreatedBy = r.CreatedBy, Created = r.Created, LastUsed = r.LastUsed,
        Expires = r.Expires, Admin = r.Admin, RoleId = r.RoleId,
    };
}
