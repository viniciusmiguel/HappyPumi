using FastEndpoints;
using FastEndpoints.Swagger;
using HappyPumi.Api.Auth;
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
bld.Services.AddDbContext<HappyPumiDbContext>(o => o.UseNpgsql(connectionString));

// Persistence seams (ADR-0005), now PostgreSQL-backed. Scoped to share the request's DbContext.
bld.Services.AddScoped<IStackStore, PostgresStackStore>();
bld.Services.AddScoped<IUpdateStore, PostgresUpdateStore>();
bld.Services.AddScoped<IIdentityStore, PostgresIdentityStore>();
bld.Services.AddScoped<IPackageRegistry, PostgresPackageRegistry>();
bld.Services.AddScoped<ITemplateRegistry, PostgresTemplateRegistry>();
bld.Services.AddScoped<IPolicyStore, PostgresPolicyStore>();
bld.Services.AddScoped<IDeploymentStore, PostgresDeploymentStore>();
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
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints()
    .UseSwaggerGen();
await app.RunAsync();

// Public marker so WebApplicationFactory<ApiMarker> can locate this assembly's entry point for
// component tests. We use a dedicated type rather than the generated top-level `Program` because the
// Aspire AppHost also emits a public `Program`, and a test referencing both would be ambiguous.
public sealed class ApiMarker;