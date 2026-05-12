using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace PulumiApiGenerator.Generators;

/// <summary>
/// Generates FastEndpoints endpoints + per-endpoint request DTOs.
///
/// One file per operation, grouped into folders by the operation's first OpenAPI tag.
/// Each operation yields:
///   - <c>{OpName}Request.cs</c>  (only when there is at least one parameter or body)
///   - <c>{OpName}Endpoint.cs</c> (always)
///
/// Endpoint base class is chosen by what the operation actually needs:
///   - request + response:    <c>Endpoint&lt;TReq, TResp&gt;</c>
///   - request only:          <c>Endpoint&lt;TReq&gt;</c>
///   - response only:         <c>EndpointWithoutRequest&lt;TResp&gt;</c>
///   - neither:               <c>EndpointWithoutRequest</c>
/// </summary>
public sealed class EndpointGenerator
{
    private readonly OpenApiDocument _doc;
    private readonly string _rootNs;
    private readonly TypeMapper _mapper;

    public EndpointGenerator(OpenApiDocument doc, string rootNs)
    {
        _doc = doc;
        _rootNs = rootNs;
        _mapper = new TypeMapper(doc);
    }

    public void Generate(string outDir)
    {
        Directory.CreateDirectory(outDir);
        if (_doc.Paths is null) return;

        // Disambiguate duplicate operationIds (rare, but possible)
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (path, pathItem) in _doc.Paths)
        {
            foreach (var (method, op) in pathItem.Operations)
            {
                var tag = op.Tags?.FirstOrDefault()?.Name ?? "Default";
                var tagFolder = Naming.TypeName(tag);
                var dir = Path.Combine(outDir, tagFolder);
                Directory.CreateDirectory(dir);

                var baseName = !string.IsNullOrEmpty(op.OperationId)
                    ? Naming.TypeName(op.OperationId)
                    : SynthesizeOpName(method, path);

                // Ensure uniqueness across the whole spec
                var opName = baseName;
                int suffix = 2;
                while (!usedNames.Add(opName)) opName = $"{baseName}{suffix++}";

                var endpointNs = $"{_rootNs}.Endpoints.{tagFolder}";
                var contractsNs = $"{_rootNs}.Contracts";

                // Combine path-level and operation-level parameters (operation overrides)
                var allParams = MergeParameters(pathItem.Parameters, op.Parameters);

                var requestInfo = BuildRequestInfo(allParams, op.RequestBody);

                if (requestInfo.HasRequest)
                {
                    var reqCode = WriteRequest(endpointNs, contractsNs, opName, path, op, requestInfo);
                    File.WriteAllText(Path.Combine(dir, $"{opName}Request.cs"), reqCode);
                }

                var responseType = ResolveResponseType(op.Responses);
                var endpointCode = WriteEndpoint(
                    endpointNs, contractsNs, opName, method, path, op,
                    requestInfo.HasRequest, responseType, tag);
                File.WriteAllText(Path.Combine(dir, $"{opName}Endpoint.cs"), endpointCode);
            }
        }
    }

    // ---------- request DTO ----------

    private sealed record RequestInfo(
        List<OpenApiParameter> PathParams,
        List<OpenApiParameter> QueryParams,
        List<OpenApiParameter> HeaderParams,
        string? BodyTypeExpr,
        string? BodyDescription)
    {
        public bool HasRequest =>
            PathParams.Count > 0 || QueryParams.Count > 0 ||
            HeaderParams.Count > 0 || BodyTypeExpr != null;
    }

    private RequestInfo BuildRequestInfo(IList<OpenApiParameter> allParams, OpenApiRequestBody? body)
    {
        var pathP   = allParams.Where(p => p.In == ParameterLocation.Path  ).ToList();
        var queryP  = allParams.Where(p => p.In == ParameterLocation.Query ).ToList();
        var headerP = allParams.Where(p => p.In == ParameterLocation.Header).ToList();

        string? bodyType = null;
        string? bodyDesc = null;
        if (body?.Content is { Count: > 0 })
        {
            // Prefer application/json; fall back to whatever's there.
            var media = body.Content.FirstOrDefault(kv => kv.Key.Contains("json", StringComparison.OrdinalIgnoreCase)).Value
                        ?? body.Content.Values.FirstOrDefault();
            if (media?.Schema != null)
            {
                bodyType = _mapper.Map(media.Schema, isNullable: false);
                bodyDesc = body.Description;
            }
        }
        return new RequestInfo(pathP, queryP, headerP, bodyType, bodyDesc);
    }

    private string WriteRequest(string ns, string contractsNs, string opName,
                                string path, OpenApiOperation op, RequestInfo info)
    {
        var w = new CodeWriter();
        Header(w, ns, contractsNs);
        w.XmlDoc($"Request for <c>{opName}</c> ({path}).{(string.IsNullOrEmpty(op.Summary) ? "" : " " + op.Summary)}");
        using (w.Block($"public sealed class {opName}Request"))
        {
            bool first = true;

            foreach (var p in info.PathParams)   first = EmitParam(w, p, ParamSource.Route,  first, opName);
            foreach (var p in info.QueryParams)  first = EmitParam(w, p, ParamSource.Query,  first, opName);
            foreach (var p in info.HeaderParams) first = EmitParam(w, p, ParamSource.Header, first, opName);

            if (info.BodyTypeExpr is { } bt)
            {
                if (!first) w.Line();
                w.XmlDoc(info.BodyDescription ?? "Request body.");
                w.Line("[FromBody]");
                var init = TypeMapper.IsReferenceTypeExpression(bt) ? " = default!;" : "";
                w.Line($"public {bt} Body {{ get; set; }}{init}");
            }
        }
        return w.ToString();
    }

    private enum ParamSource { Route, Query, Header }

    private bool EmitParam(CodeWriter w, OpenApiParameter p, ParamSource source, bool first, string containingType)
    {
        if (!first) w.Line();
        var propName = Naming.PropertyName(p.Name, containingTypeName: containingType + "Request");
        var typeExpr = _mapper.Map(p.Schema, isNullable: !p.Required);

        w.XmlDoc(p.Description);

        switch (source)
        {
            case ParamSource.Header:
                w.Line($"[FromHeader(\"{Naming.EscapeStringLiteral(p.Name)}\")]");
                break;
            case ParamSource.Query:
                w.Line("[QueryParam]");
                if (!string.Equals(propName, p.Name, StringComparison.Ordinal))
                    w.Line($"[BindFrom(\"{Naming.EscapeStringLiteral(p.Name)}\")]");
                break;
            case ParamSource.Route:
                // FastEndpoints will bind from the route segment by property name.
                // BindFrom keeps it explicit when names diverge after PascalCase'ing.
                if (!string.Equals(propName, p.Name, StringComparison.Ordinal))
                    w.Line($"[BindFrom(\"{Naming.EscapeStringLiteral(p.Name)}\")]");
                break;
        }

        var init = p.Required && TypeMapper.IsReferenceTypeExpression(typeExpr) ? " = default!;" : "";
        w.Line($"public {typeExpr} {propName} {{ get; set; }}{init}");
        return false;
    }

    // ---------- endpoint class ----------

    private string WriteEndpoint(string ns, string contractsNs, string opName,
                                 OperationType method, string path, OpenApiOperation op,
                                 bool hasRequest, string? responseType, string tagName)
    {
        var w = new CodeWriter();
        Header(w, ns, contractsNs);

        string baseType = (hasRequest, responseType) switch
        {
            (true,  not null) => $"Endpoint<{opName}Request, {responseType}>",
            (true,  null    ) => $"Endpoint<{opName}Request>",
            (false, not null) => $"EndpointWithoutRequest<{responseType}>",
            (false, null    ) => "EndpointWithoutRequest",
        };

        w.XmlDoc(op.Summary ?? op.Description ?? $"{method.ToString().ToUpperInvariant()} {path}");

        using (w.Block($"public sealed class {opName}Endpoint : {baseType}"))
        {
            // ---- Configure() ----
            using (w.Block("public override void Configure()"))
            {
                foreach (var line in VerbLines(method, path)) w.Line(line);
                w.Line("AllowAnonymous(); // TODO: replace with your auth policy (e.g. Roles(...), Policies(...))");
                w.Line("Description(b => b");
                w.Line($"    .WithTags(\"{Naming.EscapeStringLiteral(tagName)}\")");
                if (!string.IsNullOrWhiteSpace(op.Summary))
                    w.Line($"    .WithSummary(\"{Naming.EscapeStringLiteral(op.Summary)}\")");
                if (!string.IsNullOrWhiteSpace(op.Description))
                    w.Line($"    .WithDescription(\"{Naming.EscapeStringLiteral(op.Description)}\")");
                w.Line($"    .WithName(\"{opName}\")");
                w.Line(");");
            }

            w.Line();

            // ---- HandleAsync() ----
            var sig = hasRequest
                ? $"public override Task HandleAsync({opName}Request req, CancellationToken ct)"
                : "public override Task HandleAsync(CancellationToken ct)";

            using (w.Block(sig))
            {
                w.Line($"// TODO: implement {opName}");
                w.Line($"// HTTP: {method.ToString().ToUpperInvariant()} {path}");
                if (responseType is not null)
                    w.Line($"// Should produce: {responseType}");
                w.Line($"throw new NotImplementedException(\"Endpoint {opName} not implemented.\");");
            }
        }
        return w.ToString();
    }

    private static IEnumerable<string> VerbLines(OperationType m, string path)
    {
        // Use the FastEndpoints single-call helpers where available — they're the
        // most idiomatic form. For verbs without a helper (currently TRACE) fall
        // back to the explicit Verbs() + Routes() pair, which works for any verb.
        var path_ = Naming.EscapeStringLiteral(path);
        switch (m)
        {
            case OperationType.Get:     yield return $"Get(\"{path_}\");";     yield break;
            case OperationType.Post:    yield return $"Post(\"{path_}\");";    yield break;
            case OperationType.Put:     yield return $"Put(\"{path_}\");";     yield break;
            case OperationType.Delete:  yield return $"Delete(\"{path_}\");";  yield break;
            case OperationType.Patch:   yield return $"Patch(\"{path_}\");";   yield break;
            case OperationType.Head:    yield return $"Head(\"{path_}\");";    yield break;
            case OperationType.Options: yield return $"Options(\"{path_}\");"; yield break;
            default:
                // OperationType.Trace and any future value: use the generic form.
                yield return $"Verbs(Http.{m.ToString().ToUpperInvariant()});";
                yield return $"Routes(\"{path_}\");";
                yield break;
        }
    }

    // ---------- helpers ----------

    private string? ResolveResponseType(OpenApiResponses? responses)
    {
        if (responses is null) return null;

        // Prefer 2xx, then default
        var pick =
            responses.FirstOrDefault(r => r.Key.StartsWith("2", StringComparison.Ordinal)).Value
            ?? (responses.TryGetValue("default", out var d) ? d : null);

        if (pick?.Content is null || pick.Content.Count == 0) return null;

        var media = pick.Content.FirstOrDefault(kv => kv.Key.Contains("json", StringComparison.OrdinalIgnoreCase)).Value
                    ?? pick.Content.Values.FirstOrDefault();
        if (media?.Schema is null) return null;

        return _mapper.Map(media.Schema, isNullable: false);
    }

    private static List<OpenApiParameter> MergeParameters(
        IList<OpenApiParameter> pathLevel, IList<OpenApiParameter> opLevel)
    {
        // Per OpenAPI: operation-level params override path-level params of the same name + location.
        var merged = new List<OpenApiParameter>();
        merged.AddRange(opLevel);
        foreach (var p in pathLevel)
        {
            if (!opLevel.Any(q => q.Name == p.Name && q.In == p.In))
                merged.Add(p);
        }
        return merged;
    }

    private static string SynthesizeOpName(OperationType method, string path)
    {
        var withoutBraces = Regex.Replace(path, @"[{}]", "");
        var parts = withoutBraces.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(parts.Select(Naming.PascalCase));
        return Naming.PascalCase(method.ToString()) + name;
    }

    private static void Header(CodeWriter w, string ns, string contractsNs)
    {
        w.Line("// <auto-generated />");
        w.Line("// This file was generated by PulumiApiGenerator. Do not edit by hand.");
        w.Line("#nullable enable");
        w.Line();
        w.Line("using System;");
        w.Line("using System.Collections.Generic;");
        w.Line("using System.Threading;");
        w.Line("using System.Threading.Tasks;");
        w.Line("using FastEndpoints;");
        w.Line($"using {contractsNs};");
        w.Line();
        w.Line($"namespace {ns};");
        w.Line();
    }
}
