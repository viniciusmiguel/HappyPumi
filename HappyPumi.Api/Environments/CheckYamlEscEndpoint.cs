// Implemented by hand (ESC check). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Request for CheckYAML. Implements <see cref="IPlainTextRequest"/> so the YAML body is read raw into
/// <see cref="Content"/> rather than JSON-deserialized — the CLI posts the definition with an
/// application/json content type (which would otherwise make FastEndpoints parse the YAML as JSON).
/// </summary>
public sealed class CheckYamlInput : IPlainTextRequest
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [QueryParam, BindFrom("showSecrets")] public bool? ShowSecrets { get; set; }
    public string Content { get; set; } = default!;
}

/// <summary>CheckYAML — validates a raw YAML definition and returns its evaluated property tree.</summary>
public sealed class CheckYamlEscEndpoint : Endpoint<CheckYamlInput, EnvironmentResponse>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/yaml/check");
        Permissions("environment:read");
        Description(b => b
            .Accepts<CheckYamlInput>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("CheckYAML")
            .WithName("CheckYamlEsc")
        );
    }

    public override async Task HandleAsync(CheckYamlInput req, CancellationToken ct)
    {
        var values = EnvironmentEvaluator.ValuesOf(EnvironmentEvaluator.ParseRoot(req.Content ?? ""));
        var properties = EnvironmentEvaluator.EvaluateValues(values);
        if (req.ShowSecrets != true)
            EscRedactor.Mask(properties);
        await Send.OkAsync(new EnvironmentResponse
        {
            Properties = properties,
            Exprs = EscExprBuilder.Build(values), // the CLI walks exprs to resolve `env get <path>`
            Diagnostics = new List<EnvironmentDiagnostic>(),
        }, ct);
    }
}
