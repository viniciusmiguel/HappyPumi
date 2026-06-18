// Local-dev / test composition root (ADR-0003, ADR-0004). `dotnet run --project HappyPumi.AppHost`
// brings up the whole topology plus the Aspire dashboard (traces/logs/metrics via OTLP — ADR-0006).
var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL (ADR-0005). The generated admin password is stored as a user-secret parameter so it is
// stable across runs; the data volume persists state so stacks/config survive a restart. pgWeb gives
// a lightweight browse UI for local dev.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();
var happyPumiDb = postgres.AddDatabase("happypumidb");

// Dex: a lightweight real OIDC provider for developing/testing RBAC against actual signed JWTs + JWKS
// (ADR-0007). Config (static users/groups, HappyPumi client) lives in dex/config.yaml. Pinned to host
// port 5556 so the issuer URL baked into that config stays stable across runs.
var dex = builder.AddContainer("dex", "ghcr.io/dexidp/dex", "v2.41.1")
    .WithBindMount("dex/config.yaml", "/etc/dex/config.yaml", isReadOnly: true)
    .WithArgs("serve", "/etc/dex/config.yaml")
    .WithHttpEndpoint(port: 5556, targetPort: 5556, name: "http")
    .WithHttpHealthCheck("/healthz");
var dexIssuer = "http://localhost:5556/dex";

// The API talks HTTPS with the self-signed dev cert (ADR-0007); the "https" launch profile exposes both
// https and http. It waits for the database, advertises a /health probe (over http, so the probe needs
// no cert trust), and is handed the Dex issuer so OIDC/RBAC can bind to it as endpoints are secured.
builder.AddProject<Projects.HappyPumi_Api>("api", launchProfileName: "https")
    .WithReference(happyPumiDb)
    .WaitFor(happyPumiDb)
    .WithEnvironment("Authentication__Oidc__Authority", dexIssuer)
    .WithEnvironment("Authentication__Oidc__ClientId", "happypumi")
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.Build().Run();
