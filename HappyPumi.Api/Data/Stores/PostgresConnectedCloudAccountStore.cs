#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IConnectedCloudAccountStore"/> (PR6, ADR-0005).</summary>
public sealed class PostgresConnectedCloudAccountStore(HappyPumiDbContext db) : IConnectedCloudAccountStore
{
    public void Upsert(string org, string provider, IReadOnlyList<CloudAccountEntry> accounts, string? credential)
    {
        var row = Row(org, provider);
        if (row is null)
        {
            db.ConnectedCloudAccounts.Add(new ConnectedCloudAccountRow
            {
                Org = org, Provider = provider, Accounts = accounts.ToList(), Credential = credential,
            });
        }
        else
        {
            row.Accounts = accounts.ToList();
            row.Credential = credential;
        }
        db.SaveChanges();
    }

    public IReadOnlyList<CloudAccountEntry> List(string org, string provider)
    {
        var row = Row(org, provider);
        return row is null ? Array.Empty<CloudAccountEntry>() : row.Accounts;
    }

    private ConnectedCloudAccountRow? Row(string org, string provider)
        => db.ConnectedCloudAccounts.FirstOrDefault(r => r.Org == org && r.Provider == provider);
}
