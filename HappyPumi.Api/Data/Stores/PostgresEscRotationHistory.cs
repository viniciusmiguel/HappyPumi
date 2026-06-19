#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEscRotationHistory"/>: rotation events (newest first), payload as jsonb.</summary>
public sealed class PostgresEscRotationHistory(HappyPumiDbContext db) : IEscRotationHistory
{
    public void Record(EnvCoordinates e, SecretRotationEvent rotationEvent)
    {
        db.EnvironmentRotationEvents.Add(new EnvironmentRotationEventRow
        {
            Id = rotationEvent.Id, Org = e.Org, Project = e.Project, Name = e.Name,
            Created = DateTime.UtcNow, Event = rotationEvent,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<SecretRotationEvent> List(EnvCoordinates e)
        => db.EnvironmentRotationEvents.AsNoTracking()
            .Where(r => r.Org == e.Org && r.Project == e.Project && r.Name == e.Name)
            .ToList().OrderByDescending(r => r.Created).Select(r => r.Event).ToList();
}
