#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IOrgWebhookStore"/> (ADR-0005), keyed by org slug. Used by unit tests.</summary>
public sealed class InMemoryOrgWebhookStore : IOrgWebhookStore
{
    private readonly ConcurrentDictionary<string, List<WebhookResponse>> _byOrg = new();

    private List<WebhookResponse> Bucket(string org) => _byOrg.GetOrAdd(org, _ => new List<WebhookResponse>());

    public IReadOnlyList<WebhookResponse> List(string org)
    {
        var list = Bucket(org);
        lock (list)
            return list.ToArray();
    }

    public WebhookResponse? Get(string org, string name)
    {
        var list = Bucket(org);
        lock (list)
            return list.FirstOrDefault(w => w.Name == name);
    }

    public WebhookResponse? Create(string org, WebhookResponse webhook)
    {
        var list = Bucket(org);
        lock (list)
        {
            if (list.Any(w => w.Name == webhook.Name))
                return null;
            list.Add(webhook);
            return webhook;
        }
    }

    public WebhookResponse? Update(string org, string name, Webhook patch)
    {
        var list = Bucket(org);
        lock (list)
        {
            var hook = list.FirstOrDefault(w => w.Name == name);
            if (hook is null)
                return null;
            StackWebhookMapper.ApplyPatch(hook, patch);
            return hook;
        }
    }

    public bool Delete(string org, string name)
    {
        var list = Bucket(org);
        lock (list)
            return list.RemoveAll(w => w.Name == name) > 0;
    }
}
