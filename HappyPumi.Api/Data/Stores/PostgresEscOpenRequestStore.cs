#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEscOpenRequestStore"/>: gated-open access requests, payload as jsonb.</summary>
public sealed class PostgresEscOpenRequestStore(HappyPumiDbContext db) : IEscOpenRequestStore
{
    public EscOpenRequest Create(EnvCoordinates e, long accessDurationSeconds, long grantExpirationSeconds, long baseRevision, string requester)
    {
        var request = new EscOpenRequest(
            Guid.NewGuid().ToString("N"), accessDurationSeconds, grantExpirationSeconds, baseRevision, requester);
        db.EnvironmentOpenRequests.Add(new EnvironmentOpenRequestRow
        {
            Id = request.Id, Org = e.Org, Project = e.Project, Name = e.Name, Request = request,
        });
        db.SaveChanges();
        return request;
    }

    public EscOpenRequest? Get(EnvCoordinates e, string changeRequestId) => Row(e, changeRequestId)?.Request;

    public EscOpenRequest? Update(EnvCoordinates e, string changeRequestId, long accessDurationSeconds, long grantExpirationSeconds)
    {
        var row = Row(e, changeRequestId);
        if (row is null)
            return null;
        row.Request = row.Request with { AccessDurationSeconds = accessDurationSeconds, GrantExpirationSeconds = grantExpirationSeconds };
        db.SaveChanges();
        return row.Request;
    }

    public OpenRequestLocation? Locate(string org, string changeRequestId)
    {
        var row = db.EnvironmentOpenRequests.FirstOrDefault(r => r.Id == changeRequestId && r.Org == org);
        return row is null ? null : new OpenRequestLocation(new EnvCoordinates(row.Org, row.Project, row.Name), row.Request);
    }

    public EscOpenRequest? Replace(EnvCoordinates e, EscOpenRequest request)
    {
        var row = Row(e, request.Id);
        if (row is null)
            return null;
        row.Request = request;
        db.SaveChanges();
        return row.Request;
    }

    // Grants are stored inside the jsonb payload, so filter in memory (few requests per environment).
    public bool HasActiveGrant(EnvCoordinates e, string requester, DateTime nowUtc)
        => db.EnvironmentOpenRequests
            .Where(r => r.Org == e.Org && r.Project == e.Project && r.Name == e.Name)
            .AsEnumerable()
            .Any(r => r.Request.Requester == requester && r.Request.IsGranted(nowUtc));

    private EnvironmentOpenRequestRow? Row(EnvCoordinates e, string id)
        => db.EnvironmentOpenRequests.FirstOrDefault(r => r.Id == id && r.Org == e.Org && r.Project == e.Project && r.Name == e.Name);
}
