using System;
using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IEnvironmentStore"/>: serves environments from an in-memory map.</summary>
public sealed class FakeEnvironmentStore : IEnvironmentStore
{
    private readonly Dictionary<EnvCoordinates, StoredEnvironment> _envs = new();

    public FakeEnvironmentStore With(EnvCoordinates coords, string yaml)
    {
        _envs[coords] = new StoredEnvironment { Coordinates = coords, Yaml = yaml };
        return this;
    }

    public StoredEnvironment? Get(EnvCoordinates coordinates) => _envs.GetValueOrDefault(coordinates);

    public IReadOnlyList<StoredEnvironment> ListByOrg(string org) => throw new NotSupportedException();
    public StoredEnvironment? Create(EnvCoordinates coordinates, string ownerLogin, string ownerName) => throw new NotSupportedException();
    public StoredEnvironment? UpdateYaml(EnvCoordinates coordinates, string yaml, string editorLogin, string editorName)
    {
        if (!_envs.TryGetValue(coordinates, out var env))
            return null;
        env.Yaml = yaml;
        env.CurrentRevision += 1;
        return env;
    }

    public IReadOnlyList<StoredEnvRevision> ListRevisions(EnvCoordinates coordinates) => throw new NotSupportedException();
    public bool Delete(EnvCoordinates coordinates) => throw new NotSupportedException();
    public StoredEnvironment? Restore(EnvCoordinates coordinates) => throw new NotSupportedException();
    public StoredEnvironment? SetDeletionProtected(EnvCoordinates coordinates, bool deletionProtected) => throw new NotSupportedException();
    public StoredEnvironment? ReassignOwner(EnvCoordinates coordinates, string ownerLogin, string ownerName) => throw new NotSupportedException();
    public StoredEnvironment? SetTag(EnvCoordinates coordinates, string name, string value) => throw new NotSupportedException();
    public bool DeleteTag(EnvCoordinates coordinates, string name) => throw new NotSupportedException();
    public StoredEnvRevision? SetRevisionTag(EnvCoordinates coordinates, string name, long revision) => throw new NotSupportedException();
    public bool DeleteRevisionTag(EnvCoordinates coordinates, string name) => throw new NotSupportedException();
    public StoredEnvRevision? RetractRevision(EnvCoordinates coordinates, long version, string? reason, long? replacement, string byLogin, string byName) => throw new NotSupportedException();
}
