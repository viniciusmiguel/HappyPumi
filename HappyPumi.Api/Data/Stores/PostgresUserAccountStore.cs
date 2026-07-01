#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IUserAccountStore"/> (org-admin PR6, ADR-0005): one account row per login.
/// <see cref="Get"/> returns fresh defaults (not persisted) when the row is absent; <see cref="Update"/>
/// creates the row on first write. Mirrors <see cref="PostgresOrgSettingsStore"/> (single-key store).
/// </summary>
public sealed class PostgresUserAccountStore(HappyPumiDbContext db) : IUserAccountStore
{
    public StoredUserAccount Get(string login)
    {
        var row = db.UserAccounts.AsNoTracking().FirstOrDefault(a => a.Login == login);
        return row is null ? new StoredUserAccount { Login = login } : Map(row);
    }

    public StoredUserAccount Update(string login, Action<StoredUserAccount> mutate)
    {
        var row = db.UserAccounts.FirstOrDefault(a => a.Login == login);
        if (row is null)
        {
            row = new UserAccountRow { Login = login, Created = DateTime.UtcNow };
            db.UserAccounts.Add(row);
        }
        var account = Map(row);
        mutate(account);
        Apply(account, row);
        db.SaveChanges();
        return Map(row);
    }

    private static void Apply(StoredUserAccount src, UserAccountRow row)
    {
        row.PendingEmail = src.PendingEmail;
        row.VerifiedEmail = src.VerifiedEmail;
        row.DefaultOrg = src.DefaultOrg;
    }

    private static StoredUserAccount Map(UserAccountRow r) => new()
    {
        Login = r.Login,
        PendingEmail = r.PendingEmail,
        VerifiedEmail = r.VerifiedEmail,
        DefaultOrg = r.DefaultOrg,
        Created = r.Created,
    };
}
