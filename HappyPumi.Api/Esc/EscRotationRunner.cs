#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Environments;
using HappyPumi.Api.State;
using YamlDotNet.Serialization;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Executes secret rotation for an environment: walks the definition for <c>fn::rotate::&lt;name&gt;</c>
/// declarations, runs each rotator, rewrites the node's <c>state</c> (new <c>current</c>, old kept as
/// <c>previous</c>), saves the result as a new revision, and returns a rotation event for the history.
/// </summary>
public sealed class EscRotationRunner(IEnvironmentStore environments, IEscRotatorRegistry rotators, IEscRotationHistory history)
{
    private static readonly ISerializer Yaml = new SerializerBuilder().Build();

    /// <summary>Rotates every <c>fn::rotate</c> declaration in the environment. Null when it does not exist.</summary>
    public async Task<SecretRotationEvent?> RotateAsync(EnvCoordinates coords, string userId, CancellationToken ct)
    {
        var env = environments.Get(coords);
        if (env is null)
            return null;

        var root = EnvironmentEvaluator.ParseRoot(env.Yaml);
        var values = EnvironmentEvaluator.ValuesOf(root);
        var rotations = new List<SecretRotation>();
        await RotateInMap(values, prefix: "", values, rotations, ct);

        var preRevision = env.CurrentRevision;
        var postRevision = preRevision;
        if (rotations.Count > 0)
        {
            root["values"] = values;
            postRevision = environments.UpdateYaml(coords, Yaml.Serialize(root), userId, userId)?.CurrentRevision ?? preRevision;
        }

        var rotationEvent = new SecretRotationEvent
        {
            Id = Guid.NewGuid().ToString("N"),
            Created = DateTime.UtcNow,
            Completed = DateTime.UtcNow,
            EnvironmentId = $"{coords.Org}/{coords.Project}/{coords.Name}",
            PreRotationRevision = preRevision,
            PostRotationRevision = postRevision,
            Rotations = rotations,
            Status = rotations.Any(r => r.Status == "failed") ? "failed" : "succeeded",
            UserId = userId,
        };
        history.Record(coords, rotationEvent);
        return rotationEvent;
    }

    private async Task RotateInMap(Dictionary<string, object?> map, string prefix, Dictionary<string, object?> root,
        List<SecretRotation> rotations, CancellationToken ct)
    {
        foreach (var key in map.Keys.ToList())
        {
            var path = prefix.Length == 0 ? key : $"{prefix}.{key}";
            if (map[key] is not Dictionary<string, object?> node)
                continue;
            if (TryReadRotate(node, out var rotatorName, out var inner))
                rotations.Add(await RotateNode(rotatorName, inner!, path, root, ct));
            else
                await RotateInMap(node, path, root, rotations, ct);
        }
    }

    private async Task<SecretRotation> RotateNode(string rotatorName, Dictionary<string, object?> inner, string path,
        Dictionary<string, object?> root, CancellationToken ct)
    {
        var rotation = new SecretRotation { Id = Guid.NewGuid().ToString("N"), EnvironmentPath = path };
        if (!rotators.TryGet(rotatorName, out var rotator))
            return Fail(rotation, $"Unknown rotator '{rotatorName}'.");

        try
        {
            var inputs = EnvironmentEvaluator.ResolveNode(inner.GetValueOrDefault("inputs"), root) as Dictionary<string, object?>
                         ?? new Dictionary<string, object?>();
            var currentRaw = (inner.GetValueOrDefault("state") as Dictionary<string, object?>)?.GetValueOrDefault("current");
            var current = EnvironmentEvaluator.ResolveNode(currentRaw, root) as IReadOnlyDictionary<string, object?>;

            var newCurrent = await rotator.RotateAsync(inputs, current, ct);
            inner["state"] = new Dictionary<string, object?> { ["current"] = newCurrent, ["previous"] = currentRaw };
            rotation.Status = "succeeded";
            return rotation;
        }
        catch (Exception ex)
        {
            return Fail(rotation, ex.Message);
        }
    }

    private static SecretRotation Fail(SecretRotation rotation, string message)
    {
        rotation.Status = "failed";
        rotation.ErrorMessage = message;
        return rotation;
    }

    private static bool TryReadRotate(Dictionary<string, object?> map, out string name, out Dictionary<string, object?>? inner)
    {
        name = "";
        inner = null;
        if (map.Count != 1)
            return false;
        var key = map.Keys.First();
        if (!key.StartsWith("fn::rotate::"))
            return false;
        name = key["fn::rotate::".Length..];
        inner = map[key] as Dictionary<string, object?>;
        return inner is not null;
    }
}
