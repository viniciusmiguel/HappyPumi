using FastEndpoints;
using FastEndpoints.Swagger;
using HappyPumi.Api.Secrets;
using HappyPumi.Api.State;

var bld = WebApplication.CreateBuilder(args);
bld.AddServiceDefaults(); // OpenTelemetry + health + service discovery + resilience (ADR-0006)
bld.Services
    .AddFastEndpoints()
    .SwaggerDocument();

// The CLI gzip-compresses some request bodies (checkpoint, events/batch, import — httpCallOptions
// GzipCompress). ASP.NET Core does not decode Content-Encoding on requests by default, so without
// this the handlers would receive raw gzip bytes instead of JSON.
bld.Services.AddRequestDecompression();

// Stack state persistence (ADR-0005). In-memory default; swap for a PostgreSQL-backed IStackStore
// without touching endpoints. Singleton so state is shared across requests for the process lifetime.
bld.Services.AddSingleton<IStackStore, InMemoryStackStore>();

// Service-managed secrets crypter for the /encrypt and /decrypt endpoints. Singleton so its
// process-static key is stable across requests (ADR-0007 secrets follow-up: persist a per-stack key).
bld.Services.AddSingleton<IValueCrypter, AesValueCrypter>();

// Update lifecycle (the up/preview/refresh/destroy state engine). The store is a singleton (shared
// state); the lifecycle coordinator is stateless and reconciles updates with the stack store.
bld.Services.AddSingleton<IUpdateStore, InMemoryUpdateStore>();
bld.Services.AddScoped<UpdateLifecycle>();

// Per-org IDP model: members, roles, team-role assignments (ADR-0007). In-memory default; endpoints
// stay anonymous for now — JWT/RBAC enforcement is a follow-up (the model lands first).
bld.Services.AddSingleton<IIdentityStore, InMemoryIdentityStore>();

// Package/template registry (ENDPOINTS.md 4). In-memory default (ADR-0005).
bld.Services.AddSingleton<IPackageRegistry, InMemoryPackageRegistry>();
bld.Services.AddSingleton<ITemplateRegistry, InMemoryTemplateRegistry>();

// CrossGuard policy: groups + versioned policy packs (ENDPOINTS.md 5). In-memory default (ADR-0005).
bld.Services.AddSingleton<IPolicyStore, InMemoryPolicyStore>();

// Managed deployments: settings, deployments, schedules, webhooks (ENDPOINTS.md 6). In-memory (ADR-0005).
bld.Services.AddSingleton<IDeploymentStore, InMemoryDeploymentStore>();

var app = bld.Build();
app.MapDefaultEndpoints(); // /health and /alive

// The pulumi CLI sends `Content-Type: application/json` on bodyless GET/HEAD/DELETE requests. For an
// endpoint that has a request DTO (e.g. GetStack), FastEndpoints would then try to JSON-deserialize the
// empty body and fail with a 400 serializer error. These verbs carry no body in this API, so drop the
// content type and let the DTO bind purely from route/query.
app.Use(async (ctx, next) =>
{
    var method = ctx.Request.Method;
    var bodyless = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsDelete(method);
    if (bodyless && (ctx.Request.ContentLength is null or 0))
        ctx.Request.ContentType = null;
    await next();
});

app.UseRequestDecompression(); // decode gzipped request bodies before model binding
app.UseFastEndpoints()
    .UseSwaggerGen();
await app.RunAsync();

// Public marker so WebApplicationFactory<ApiMarker> can locate this assembly's entry point for
// component tests. We use a dedicated type rather than the generated top-level `Program` because the
// Aspire AppHost also emits a public `Program`, and a test referencing both would be ambiguous.
public sealed class ApiMarker;