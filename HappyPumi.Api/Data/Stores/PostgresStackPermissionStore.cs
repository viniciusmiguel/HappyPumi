#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IStackPermissionStore"/> (ADR-0005).</summary>
public sealed class PostgresStackPermissionStore(HappyPumiDbContext db) : IStackPermissionStore
{
    private const string UserKind = "user";
    private const string TeamKind = "team";

    public void SetUserPermission(StackCoordinates c, string userName, long permission, bool isCreator = false)
        => Upsert(c, UserKind, userName, permission, isCreator);

    public IReadOnlyList<(string Name, long Permission)> ListUsers(StackCoordinates c)
        => Subjects(c, UserKind).Select(r => (r.SubjectName, r.Permission)).ToList();

    public bool RemoveUser(StackCoordinates c, string userName)
        => Remove(c, UserKind, userName);

    public long? GetUserPermission(StackCoordinates c, string userName)
    {
        var row = Row(c, UserKind, userName);
        return row?.Permission;
    }

    public string? GetStackCreator(StackCoordinates c)
        => Subjects(c, UserKind).Where(r => r.IsCreator).Select(r => r.SubjectName).FirstOrDefault();

    public IReadOnlyList<(string TeamName, long Permission)> ListTeams(StackCoordinates c)
        => Subjects(c, TeamKind).Select(r => (r.SubjectName, r.Permission)).ToList();

    public void SetTeamPermission(StackCoordinates c, string teamName, long permission)
        => Upsert(c, TeamKind, teamName, permission, isCreator: false);

    public bool RemoveTeam(StackCoordinates c, string teamName)
        => Remove(c, TeamKind, teamName);

    private void Upsert(StackCoordinates c, string kind, string name, long permission, bool isCreator)
    {
        var row = Row(c, kind, name);
        if (row is null)
        {
            db.StackPermissions.Add(new StackPermissionRow
            {
                Org = c.Org, Project = c.Project, Stack = c.Stack,
                SubjectKind = kind, SubjectName = name, Permission = permission, IsCreator = isCreator,
            });
        }
        else
        {
            row.Permission = permission;
            row.IsCreator = isCreator;
        }
        db.SaveChanges();
    }

    private bool Remove(StackCoordinates c, string kind, string name)
    {
        var row = Row(c, kind, name);
        if (row is null)
            return false;
        db.StackPermissions.Remove(row);
        db.SaveChanges();
        return true;
    }

    private StackPermissionRow? Row(StackCoordinates c, string kind, string name)
        => db.StackPermissions.FirstOrDefault(r =>
            r.Org == c.Org && r.Project == c.Project && r.Stack == c.Stack &&
            r.SubjectKind == kind && r.SubjectName == name);

    private List<StackPermissionRow> Subjects(StackCoordinates c, string kind)
        => db.StackPermissions.AsNoTracking()
            .Where(r => r.Org == c.Org && r.Project == c.Project && r.Stack == c.Stack && r.SubjectKind == kind)
            .ToList();
}
