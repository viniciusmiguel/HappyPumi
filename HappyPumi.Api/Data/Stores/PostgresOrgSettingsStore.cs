#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IOrgSettingsStore"/> (ADR-0005): one settings row per org, keyed by Org.
/// <see cref="Get"/> returns fresh defaults (not persisted) when the row is absent; <see cref="Update"/>
/// creates the row on first write.
/// </summary>
public sealed class PostgresOrgSettingsStore(HappyPumiDbContext db) : IOrgSettingsStore
{
    public StoredOrgSettings Get(string org)
    {
        var row = db.OrgSettings.AsNoTracking().FirstOrDefault(s => s.Org == org);
        return row is null ? new StoredOrgSettings { Org = org } : Map(row);
    }

    public StoredOrgSettings Update(string org, Action<StoredOrgSettings> mutate)
    {
        var row = db.OrgSettings.FirstOrDefault(s => s.Org == org);
        if (row is null)
        {
            row = new OrgSettingsRow { Org = org, Created = DateTime.UtcNow };
            db.OrgSettings.Add(row);
        }
        var settings = Map(row);
        mutate(settings);
        Apply(settings, row);
        db.SaveChanges();
        return Map(row);
    }

    private static void Apply(StoredOrgSettings src, OrgSettingsRow row)
    {
        row.MembersCanCreateStacks = src.MembersCanCreateStacks;
        row.MembersCanDeleteStacks = src.MembersCanDeleteStacks;
        row.MembersCanCreateTeams = src.MembersCanCreateTeams;
        row.MembersCanTransferStacks = src.MembersCanTransferStacks;
        row.MembersCanCreateAccounts = src.MembersCanCreateAccounts;
        row.DefaultStackPermission = src.DefaultStackPermission;
        row.DefaultAccountPermission = src.DefaultAccountPermission;
        row.DefaultEnvironmentPermission = src.DefaultEnvironmentPermission;
        row.DefaultRoleId = src.DefaultRoleId;
        row.DefaultDeploymentRoleId = src.DefaultDeploymentRoleId;
        row.DefaultAgentPoolId = src.DefaultAgentPoolId;
        row.PreferredVcs = src.PreferredVcs;
        row.AiEnablement = src.AiEnablement;
        row.NeoEnabled = src.NeoEnabled;
    }

    private static StoredOrgSettings Map(OrgSettingsRow r) => new()
    {
        Org = r.Org,
        MembersCanCreateStacks = r.MembersCanCreateStacks,
        MembersCanDeleteStacks = r.MembersCanDeleteStacks,
        MembersCanCreateTeams = r.MembersCanCreateTeams,
        MembersCanTransferStacks = r.MembersCanTransferStacks,
        MembersCanCreateAccounts = r.MembersCanCreateAccounts,
        DefaultStackPermission = r.DefaultStackPermission,
        DefaultAccountPermission = r.DefaultAccountPermission,
        DefaultEnvironmentPermission = r.DefaultEnvironmentPermission,
        DefaultRoleId = r.DefaultRoleId,
        DefaultDeploymentRoleId = r.DefaultDeploymentRoleId,
        DefaultAgentPoolId = r.DefaultAgentPoolId,
        PreferredVcs = r.PreferredVcs,
        AiEnablement = r.AiEnablement,
        NeoEnabled = r.NeoEnabled,
        Created = r.Created,
    };
}
