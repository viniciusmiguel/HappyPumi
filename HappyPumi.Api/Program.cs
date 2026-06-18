using FastEndpoints;
using FastEndpoints.Swagger;
using HappyPumi.Api.Auth;
using HappyPumi.Api.ConsoleApi;
using HappyPumi.Api.Data;
using HappyPumi.Api.Data.Stores;
using HappyPumi.Api.Secrets;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

var bld = WebApplication.CreateBuilder(args);
bld.AddServiceDefaults(); // OpenTelemetry + health + service discovery + resilience (ADR-0006)
bld.Services
    .AddFastEndpoints()
    .SwaggerDocument();

// The CLI gzip-compresses some request bodies (checkpoint, events/batch, import — httpCallOptions
// GzipCompress). ASP.NET Core does not decode Content-Encoding on requests by default, so without
// this the handlers would receive raw gzip bytes instead of JSON.
bld.Services.AddRequestDecompression();

// PostgreSQL persistence (ADR-0005). Aspire injects ConnectionStrings__happypumidb (WithReference);
// the connection is required — all state now lives in Postgres.
var connectionString = bld.Configuration.GetConnectionString("happypumidb")
    ?? throw new InvalidOperationException(
        "Connection string 'happypumidb' is not configured. Run via `make dev` (Aspire) or set " +
        "ConnectionStrings__happypumidb. HappyPumi persists all state to PostgreSQL (ADR-0005).");
bld.Services.AddDbContext<HappyPumiDbContext>(o => o
    .UseNpgsql(connectionString)
    // jsonb columns carry custom value converters/comparers; EF's model-diff occasionally reports a benign
    // PendingModelChangesWarning for them even when `dotnet ef migrations has-pending-model-changes` confirms
    // the model matches the latest migration. Don't fail startup on that false positive.
    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Persistence seams (ADR-0005), now PostgreSQL-backed. Scoped to share the request's DbContext.
bld.Services.AddScoped<IStackStore, PostgresStackStore>();
bld.Services.AddScoped<IUpdateStore, PostgresUpdateStore>();
bld.Services.AddScoped<IIdentityStore, PostgresIdentityStore>();
bld.Services.AddScoped<IPackageRegistry, PostgresPackageRegistry>();
bld.Services.AddScoped<ITemplateRegistry, PostgresTemplateRegistry>();
bld.Services.AddScoped<IPolicyStore, PostgresPolicyStore>();
bld.Services.AddScoped<IDeploymentStore, PostgresDeploymentStore>();
bld.Services.AddScoped<IDeploymentQueue, PostgresDeploymentQueue>(); // runner work queue (agent poll/dispatch)
bld.Services.AddScoped<IAgentPoolStore, PostgresAgentPoolStore>();   // workflow-runner pools + token validation
bld.Services.AddScoped<UpdateLifecycle>();

// Service-managed secrets crypter for the /encrypt and /decrypt endpoints. Singleton so its
// process-static key is stable across requests (ADR-0007 secrets follow-up: persist a per-stack key).
bld.Services.AddSingleton<IValueCrypter, AesValueCrypter>();

// Authentication + RBAC (ADR-0007). The CLI authenticates with its `token` scheme (PulumiTokenAuthHandler).
// Endpoints opt in to enforcement by dropping AllowAnonymous() and (for org management) requiring the admin
// role. Interactive/console OIDC JWT Bearer against Dex is the follow-up half of this seam.
bld.Services
    .AddAuthentication(PulumiTokenAuthHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PulumiTokenAuthHandler>(
        PulumiTokenAuthHandler.SchemeName, null);

bld.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.OrgAdmin, p => p.RequireRole("admin"));

var app = bld.Build();

// Apply the schema migration on startup so a fresh Postgres is ready to serve (dev + tests). When
// Seed:Enabled is set (local dev via `make dev` and the CLI integration server), load demo data so the
// CLI has real stacks/orgs/registry/policy to query.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HappyPumiDbContext>();
    db.Database.Migrate();
    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
        HappyPumi.Api.Data.DatabaseSeeder.Seed(db);
}

app.MapDefaultEndpoints(); // /health and /alive

// Dev-only permissive CORS so a browser-based console on another origin (e.g. the Pulumi console at
// :3000) can call this API directly. Headers are applied via OnStarting so they survive error responses
// (e.g. a stub endpoint that throws 500) — otherwise the browser sees a CORS failure instead of the body.
if (app.Configuration.GetValue<bool>("AllowCorsAll"))
    app.Use(async (ctx, next) =>
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers.AccessControlAllowOrigin = origin;
                ctx.Response.Headers.AccessControlAllowCredentials = "true";
                ctx.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS";
                var reqHeaders = ctx.Request.Headers.AccessControlRequestHeaders.ToString();
                ctx.Response.Headers.AccessControlAllowHeaders = string.IsNullOrEmpty(reqHeaders) ? "*" : reqHeaders;
                ctx.Response.Headers.Append("Vary", "Origin");
                return Task.CompletedTask;
            });
        if (HttpMethods.IsOptions(ctx.Request.Method))
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }
        await next();
    });

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

// Dev-only wire logger to reverse-engineer the workflow agent/runner callbacks (method, path, content-type → status).
if (app.Configuration.GetValue<bool>("LogRequests"))
    app.Use(async (ctx, next) =>
    {
        await next();
        Console.WriteLine($"WIRE {ctx.Request.Method} {ctx.Request.Path}{ctx.Request.QueryString} " +
                          $"ct={ctx.Request.ContentType ?? "-"} -> {ctx.Response.StatusCode}");
    });

app.UseRequestDecompression(); // decode gzipped request bodies before model binding
if (app.Configuration.GetValue<bool>("MockConsole"))
    app.UseMockConsole();      // dev-only: feed the prebuilt web console mock data for its internal endpoints
app.UseAgentPoolTokenAuth();   // reject agent pool-scoped calls that lack a valid pool token (ADR-0007)
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints()
    .UseSwaggerGen();
await app.RunAsync();

// Public marker so WebApplicationFactory<ApiMarker> can locate this assembly's entry point for
// component tests. We use a dedicated type rather than the generated top-level `Program` because the
// Aspire AppHost also emits a public `Program`, and a test referencing both would be ambiguous.
public sealed class ApiMarker;