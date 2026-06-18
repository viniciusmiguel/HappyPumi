using FastEndpoints;
using FastEndpoints.Swagger;
using HappyPumi.Api.Secrets;
using HappyPumi.Api.State;

var bld = WebApplication.CreateBuilder(args);
bld.AddServiceDefaults(); // OpenTelemetry + health + service discovery + resilience (ADR-0006)
bld.Services
    .AddFastEndpoints()
    .SwaggerDocument();

// Stack state persistence (ADR-0005). In-memory default; swap for a PostgreSQL-backed IStackStore
// without touching endpoints. Singleton so state is shared across requests for the process lifetime.
bld.Services.AddSingleton<IStackStore, InMemoryStackStore>();

// Service-managed secrets crypter for the /encrypt and /decrypt endpoints. Singleton so its
// process-static key is stable across requests (ADR-0007 secrets follow-up: persist a per-stack key).
bld.Services.AddSingleton<IValueCrypter, AesValueCrypter>();

var app = bld.Build();
app.MapDefaultEndpoints(); // /health and /alive
app.UseFastEndpoints()
    .UseSwaggerGen();
await app.RunAsync();

// Public marker so WebApplicationFactory<ApiMarker> can locate this assembly's entry point for
// component tests. We use a dedicated type rather than the generated top-level `Program` because the
// Aspire AppHost also emits a public `Program`, and a test referencing both would be ambiguous.
public sealed class ApiMarker;