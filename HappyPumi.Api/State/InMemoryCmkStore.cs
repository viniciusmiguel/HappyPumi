#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="ICmkStore"/> (ADR-0005), keyed by org. Used by unit tests.</summary>
public sealed class InMemoryCmkStore : ICmkStore
{
    private readonly ConcurrentDictionary<string, List<StoredCmk>> _keys = new();
    private readonly ConcurrentDictionary<string, List<StoredKeyMigration>> _migrations = new();

    private List<StoredCmk> Keys(string org) => _keys.GetOrAdd(org, _ => new List<StoredCmk>());

    private List<StoredKeyMigration> Migrations(string org) => _migrations.GetOrAdd(org, _ => new List<StoredKeyMigration>());

    public StoredCmk Create(string org, string name, string keyType, string? keyArn, string? roleArn)
    {
        var key = new StoredCmk
        {
            Id = Guid.NewGuid().ToString(), Org = org, Name = name, KeyType = keyType,
            KeyArn = keyArn, RoleArn = roleArn, IsDefault = true, Enabled = true,
        };
        var list = Keys(org);
        lock (list)
        {
            Demote(list);
            list.Add(key);
        }
        RecordMigration(org);
        return key;
    }

    public IReadOnlyList<StoredCmk> List(string org)
    {
        var list = Keys(org);
        lock (list)
            return list.OrderByDescending(k => k.Created).ToArray();
    }

    public StoredCmk? Get(string org, string id)
    {
        var list = Keys(org);
        lock (list)
            return list.FirstOrDefault(k => k.Id == id);
    }

    public bool SetDefault(string org, string id)
    {
        var list = Keys(org);
        lock (list)
        {
            var key = list.FirstOrDefault(k => k.Id == id);
            if (key is null)
                return false;
            Demote(list);
            key.IsDefault = true;
            key.Enabled = true;
        }
        RecordMigration(org);
        return true;
    }

    public bool Disable(string org, string id)
    {
        var list = Keys(org);
        lock (list)
        {
            var key = list.FirstOrDefault(k => k.Id == id);
            if (key is null)
                return false;
            key.Enabled = false;
            key.IsDefault = false;
            return true;
        }
    }

    public int DisableAll(string org)
    {
        var list = Keys(org);
        lock (list)
        {
            var disabled = list.Count(k => k.Enabled);
            foreach (var key in list)
            {
                key.Enabled = false;
                key.IsDefault = false;
            }
            return disabled;
        }
    }

    public IReadOnlyList<StoredKeyMigration> ListMigrations(string org)
    {
        var list = Migrations(org);
        lock (list)
            return list.OrderByDescending(m => m.Created).ToArray();
    }

    public int RetryMigrations(string org)
    {
        var list = Migrations(org);
        lock (list)
        {
            var failed = list.Where(m => m.State == "failed").ToList();
            foreach (var migration in failed)
                migration.State = "completed";
            return failed.Count;
        }
    }

    private static void Demote(List<StoredCmk> list)
    {
        foreach (var key in list)
            key.IsDefault = false;
    }

    private void RecordMigration(string org)
    {
        var list = Migrations(org);
        lock (list)
            list.Add(new StoredKeyMigration { Id = Guid.NewGuid().ToString(), Org = org });
    }
}
