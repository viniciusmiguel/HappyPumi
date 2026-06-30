#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IChangeGateStore"/> (PR1, ADR-0005): change gates keyed by id and scoped by
/// Org. The approver-eligibility entries and action types round-trip through jsonb columns.
/// </summary>
public sealed class PostgresChangeGateStore(HappyPumiDbContext db) : IChangeGateStore
{
    public StoredChangeGate Create(StoredChangeGate gate)
    {
        db.ChangeGates.Add(ToRow(gate));
        db.SaveChanges();
        return gate;
    }

    public IReadOnlyList<StoredChangeGate> List(string org)
        => db.ChangeGates.AsNoTracking().Where(g => g.Org == org)
            .OrderByDescending(g => g.Created).ToList().Select(Map).ToList();

    public StoredChangeGate? Get(string org, string id)
    {
        var row = db.ChangeGates.AsNoTracking().FirstOrDefault(g => g.Org == org && g.Id == id);
        return row is null ? null : Map(row);
    }

    public StoredChangeGate? Update(string org, string id, Action<StoredChangeGate> mutate)
    {
        var row = db.ChangeGates.FirstOrDefault(g => g.Org == org && g.Id == id);
        if (row is null)
            return null;
        var gate = Map(row);
        mutate(gate);
        Apply(row, gate);
        db.SaveChanges();
        return gate;
    }

    public bool Delete(string org, string id)
    {
        var row = db.ChangeGates.FirstOrDefault(g => g.Org == org && g.Id == id);
        if (row is null)
            return false;
        db.ChangeGates.Remove(row);
        db.SaveChanges();
        return true;
    }

    private static ChangeGateRow ToRow(StoredChangeGate g)
    {
        var row = new ChangeGateRow { Id = g.Id, Org = g.Org, Created = g.Created };
        Apply(row, g);
        return row;
    }

    private static void Apply(ChangeGateRow row, StoredChangeGate g)
    {
        row.Name = g.Name;
        row.Enabled = g.Enabled;
        row.RuleType = g.RuleType;
        row.NumApprovalsRequired = g.NumApprovalsRequired;
        row.AllowSelfApproval = g.AllowSelfApproval;
        row.RequireReapprovalOnChange = g.RequireReapprovalOnChange;
        row.EligibleApprovers = g.EligibleApprovers.ToList();
        row.TargetEntityType = g.TargetEntityType;
        row.ActionTypes = g.ActionTypes.ToList();
        row.QualifiedName = g.QualifiedName;
    }

    private static StoredChangeGate Map(ChangeGateRow r) => new()
    {
        Id = r.Id, Org = r.Org, Name = r.Name, Enabled = r.Enabled, RuleType = r.RuleType,
        NumApprovalsRequired = r.NumApprovalsRequired, AllowSelfApproval = r.AllowSelfApproval,
        RequireReapprovalOnChange = r.RequireReapprovalOnChange, EligibleApprovers = r.EligibleApprovers.ToList(),
        TargetEntityType = r.TargetEntityType, ActionTypes = r.ActionTypes.ToList(),
        QualifiedName = r.QualifiedName, Created = r.Created,
    };
}
