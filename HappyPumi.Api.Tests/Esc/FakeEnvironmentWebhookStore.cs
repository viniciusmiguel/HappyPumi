using System;
using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IEnvironmentWebhookStore"/>: serves webhooks from a map (Get only).</summary>
public sealed class FakeEnvironmentWebhookStore : IEnvironmentWebhookStore
{
    private readonly Dictionary<string, StoredWebhook> _hooks = new();

    private static string Key(EnvCoordinates e, string name) => $"{e.Org}/{e.Project}/{e.Name}/{name}";

    public FakeEnvironmentWebhookStore With(EnvCoordinates env, StoredWebhook webhook)
    {
        _hooks[Key(env, webhook.Name)] = webhook;
        return this;
    }

    public StoredWebhook? Get(EnvCoordinates environment, string name) => _hooks.GetValueOrDefault(Key(environment, name));

    public IReadOnlyList<StoredWebhook> List(EnvCoordinates environment) => throw new NotSupportedException();
    public StoredWebhook? Create(EnvCoordinates environment, StoredWebhook webhook) => throw new NotSupportedException();
    public StoredWebhook? Update(EnvCoordinates environment, string name, StoredWebhook webhook) => throw new NotSupportedException();
    public bool Delete(EnvCoordinates environment, string name) => throw new NotSupportedException();
}
