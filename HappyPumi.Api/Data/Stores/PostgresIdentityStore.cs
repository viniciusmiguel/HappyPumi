#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IIdentityStore"/> (ADR-0005, ADR-0007).</summary>
public sealed class PostgresIdentityStore(HappyPumiDbContext db) : IIdentityStore
{
    public IReadOnlyCollection<StoredMember> ListMembers(string org)
        => db.Members.AsNoTracking().Where(m => m.Org == org).ToList().Select(ToMember).ToList();

    public StoredMember AddMember(string org, string userLogin, string role)
    {
        var row = db.Members.FirstOrDefault(m => m.Org == org && m.UserLogin == userLogin);
        if (row is null)
        {
            row = new MemberRow { Org = org, UserLogin = userLogin, Role = role, Created = DateTime.UtcNow };
            db.Members.Add(row);
        }
        else
        {
            row.Role = role;
        }
        db.SaveChanges();
        return ToMember(row);
    }

    public StoredMember? UpdateMemberRole(string org, string userLogin, string role)
    {
        var row = db.Members.FirstOrDefault(m => m.Org == org && m.UserLogin == userLogin);
        if (row is null)
            return null;
        row.Role = role;
        db.SaveChanges();
        return ToMember(row);
    }

    public bool RemoveMember(string org, string userLogin)
    {
        var row = db.Members.FirstOrDefault(m => m.Org == org && m.UserLogin == userLogin);
        if (row is null)
            return false;
        db.Members.Remove(row);
        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<StoredMember> ListMembersWithRole(string org, string roleId)
    {
        var role = db.Roles.AsNoTracking().FirstOrDefault(r => r.Org == org && r.Id == roleId);
        if (role is null)
            return Array.Empty<StoredMember>();
        return db.Members.AsNoTracking()
            .Where(m => m.Org == org && (m.Role == role.Id || m.Role == role.Name))
            .ToList().Select(ToMember).ToList();
    }

    public IReadOnlyCollection<StoredRole> ListRoles(string org)
        => db.Roles.AsNoTracking().Where(r => r.Org == org).ToList().Select(ToRole).ToList();

    public StoredRole CreateRole(string org, PermissionDescriptorBase descriptor)
    {
        var role = new StoredRole { Id = Guid.NewGuid().ToString(), OrgId = org };
        role.Apply(descriptor);
        db.Roles.Add(ToRow(role));
        db.SaveChanges();
        return role;
    }

    public StoredRole? GetRole(string org, string roleId)
    {
        var row = db.Roles.AsNoTracking().FirstOrDefault(r => r.Org == org && r.Id == roleId);
        return row is null ? null : ToRole(row);
    }

    public StoredRole? UpdateRole(string org, string roleId, PermissionDescriptorBase descriptor)
    {
        var row = db.Roles.FirstOrDefault(r => r.Org == org && r.Id == roleId);
        if (row is null)
            return null;
        row.Name = descriptor.Name;
        row.Description = descriptor.Description;
        row.ResourceType = descriptor.ResourceType;
        row.UxPurpose = descriptor.UxPurpose;
        row.Details = descriptor.Details;
        row.Modified = DateTime.UtcNow;
        row.Version++;
        db.SaveChanges();
        return ToRole(row);
    }

    public bool DeleteRole(string org, string roleId)
    {
        var row = db.Roles.FirstOrDefault(r => r.Org == org && r.Id == roleId);
        if (row is null)
            return false;
        db.Roles.Remove(row);
        db.TeamRoles.RemoveRange(db.TeamRoles.Where(t => t.Org == org && t.RoleId == roleId));
        db.SaveChanges();
        return true;
    }

    public bool SetDefaultRole(string org, string roleId)
    {
        var roles = db.Roles.Where(r => r.Org == org).ToList();
        if (roles.All(r => r.Id != roleId))
            return false;
        foreach (var role in roles)
            role.IsOrgDefault = role.Id == roleId;
        db.SaveChanges();
        return true;
    }

    public bool AssignTeamRole(string org, string teamName, string roleId)
    {
        if (!db.Roles.Any(r => r.Org == org && r.Id == roleId))
            return false;
        if (!db.TeamRoles.Any(t => t.Org == org && t.TeamName == teamName && t.RoleId == roleId))
        {
            db.TeamRoles.Add(new TeamRoleRow { Org = org, TeamName = teamName, RoleId = roleId });
            db.SaveChanges();
        }
        return true;
    }

    public bool RemoveTeamRole(string org, string teamName, string roleId)
    {
        var row = db.TeamRoles.FirstOrDefault(t => t.Org == org && t.TeamName == teamName && t.RoleId == roleId);
        if (row is null)
            return false;
        db.TeamRoles.Remove(row);
        db.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<StoredTeam> ListTeams(string org)
        => db.Teams.AsNoTracking().Where(t => t.Org == org).ToList().Select(r => ToTeam(r, org)).ToList();

    public StoredTeam? GetTeam(string org, string teamName)
    {
        var row = db.Teams.AsNoTracking().FirstOrDefault(t => t.Org == org && t.Name == teamName);
        return row is null ? null : ToTeam(row, org);
    }

    public StoredTeam? CreateTeam(string org, string name, string displayName, string description, string kind)
    {
        if (db.Teams.Any(t => t.Org == org && t.Name == name))
            return null;
        db.Teams.Add(new TeamRow { Org = org, Name = name, DisplayName = displayName, Description = description, Kind = kind });
        db.SaveChanges();
        return new StoredTeam { Name = name, DisplayName = displayName, Description = description, Kind = kind };
    }

    public StoredTeam? UpdateTeam(string org, string teamName, string? newName, string? displayName, string? description)
    {
        var row = db.Teams.FirstOrDefault(t => t.Org == org && t.Name == teamName);
        if (row is null)
            return null;
        if (displayName is not null) row.DisplayName = displayName;
        if (description is not null) row.Description = description;
        if (!string.IsNullOrWhiteSpace(newName) && newName != teamName)
            return RenameRow(org, row, teamName, newName!);
        db.SaveChanges();
        return ToTeam(row, org);
    }

    /// <summary>Re-keys a team row under <paramref name="newName"/> (the name is part of the PK), carrying its
    /// members and re-pointing its role grants. Returns null when the new name is already taken.</summary>
    private StoredTeam? RenameRow(string org, TeamRow row, string oldName, string newName)
    {
        if (db.Teams.Any(t => t.Org == org && t.Name == newName))
            return null;
        var moved = new TeamRow
        {
            Org = org, Name = newName, DisplayName = row.DisplayName,
            Description = row.Description, Kind = row.Kind, Members = row.Members,
        };
        db.Teams.Remove(row);
        db.Teams.Add(moved);
        foreach (var grant in db.TeamRoles.Where(t => t.Org == org && t.TeamName == oldName).ToList())
        {
            db.TeamRoles.Remove(grant);
            db.TeamRoles.Add(new TeamRoleRow { Org = org, TeamName = newName, RoleId = grant.RoleId });
        }
        db.SaveChanges();
        return ToTeam(moved, org);
    }

    public bool DeleteTeam(string org, string teamName)
    {
        var row = db.Teams.FirstOrDefault(t => t.Org == org && t.Name == teamName);
        if (row is null)
            return false;
        db.TeamRoles.RemoveRange(db.TeamRoles.Where(t => t.Org == org && t.TeamName == teamName));
        db.Teams.Remove(row);
        db.SaveChanges();
        return true;
    }

    public bool AddTeamMember(string org, string teamName, string userLogin)
    {
        var row = db.Teams.FirstOrDefault(t => t.Org == org && t.Name == teamName);
        if (row is null)
            return false;
        if (!row.Members.Contains(userLogin))
            row.Members = new List<string>(row.Members) { userLogin };
        db.SaveChanges();
        return true;
    }

    public bool RemoveTeamMember(string org, string teamName, string userLogin)
    {
        var row = db.Teams.FirstOrDefault(t => t.Org == org && t.Name == teamName);
        if (row is null || !row.Members.Contains(userLogin))
            return false;
        row.Members = row.Members.Where(m => m != userLogin).ToList();
        db.SaveChanges();
        return true;
    }

    private StoredTeam ToTeam(TeamRow r, string org)
    {
        var team = new StoredTeam { Name = r.Name, DisplayName = r.DisplayName, Description = r.Description, Kind = r.Kind };
        team.Members.AddRange(r.Members);
        team.RoleIds.AddRange(db.TeamRoles.AsNoTracking()
            .Where(t => t.Org == org && t.TeamName == r.Name).Select(t => t.RoleId));
        return team;
    }

    private static StoredMember ToMember(MemberRow r)
        => new() { UserLogin = r.UserLogin, Role = r.Role, Created = r.Created };

    private static StoredRole ToRole(RoleRow r) => new()
    {
        Id = r.Id, OrgId = r.Org, Created = r.Created, Modified = r.Modified, Version = r.Version,
        IsOrgDefault = r.IsOrgDefault, Name = r.Name, Description = r.Description,
        ResourceType = r.ResourceType, UxPurpose = r.UxPurpose, Details = r.Details,
    };

    private static RoleRow ToRow(StoredRole r) => new()
    {
        Id = r.Id, Org = r.OrgId, Created = r.Created, Modified = r.Modified, Version = r.Version,
        IsOrgDefault = r.IsOrgDefault, Name = r.Name, Description = r.Description,
        ResourceType = r.ResourceType, UxPurpose = r.UxPurpose, Details = r.Details,
    };
}
