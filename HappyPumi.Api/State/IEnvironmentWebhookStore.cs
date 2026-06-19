#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>Persistence seam for environment webhooks (one per (env, name)). Backed by PostgreSQL (ADR-0005).</summary>
public interface IEnvironmentWebhookStore
{
    IReadOnlyList<StoredWebhook> List(EnvCoordinates environment);
    StoredWebhook? Get(EnvCoordinates environment, string name);
    /// <summary>Creates a webhook; null when one with the same name already exists on the environment.</summary>
    StoredWebhook? Create(EnvCoordinates environment, StoredWebhook webhook);
    /// <summary>Replaces a webhook's settings; null when it does not exist.</summary>
    StoredWebhook? Update(EnvCoordinates environment, string name, StoredWebhook webhook);
    bool Delete(EnvCoordinates environment, string name);
}
