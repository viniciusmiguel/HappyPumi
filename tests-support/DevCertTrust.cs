using System.Runtime.CompilerServices;

namespace HappyPumi.TestSupport;

/// <summary>
/// Makes the test process trust the self-signed ASP.NET Core HTTPS dev certificate the same way the Go
/// pulumi CLI is configured: by pointing OpenSSL's <c>SSL_CERT_DIR</c> at the dev-certs trust store
/// (which .NET on Linux also consults). Runs automatically at assembly load, before any TLS handshake.
///
/// Idempotent and best-effort: no-op on non-Linux, when HOME is unset, when the trust dir is missing
/// (run <c>dotnet dev-certs https --trust</c> — or <c>make certs</c>), or when already configured.
/// This file is linked into each test project so every test assembly initializes its own process.
/// </summary>
internal static class DevCertTrust
{
    private const string SystemCertDir = "/usr/lib/ssl/certs";

    [ModuleInitializer]
    internal static void Configure()
    {
        if (!OperatingSystem.IsLinux())
            return;

        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
            return;

        var trustDir = Path.Combine(home, ".aspnet", "dev-certs", "trust");
        if (!Directory.Exists(trustDir))
            return;

        var existing = Environment.GetEnvironmentVariable("SSL_CERT_DIR");
        if (!string.IsNullOrEmpty(existing) && existing.Contains(trustDir, StringComparison.Ordinal))
            return;

        var combined = string.IsNullOrEmpty(existing)
            ? $"{trustDir}:{SystemCertDir}"
            : $"{trustDir}:{existing}";
        Environment.SetEnvironmentVariable("SSL_CERT_DIR", combined);
    }

    /// <summary>The colon-separated SSL_CERT_DIR value used to trust the dev cert (for child processes).</summary>
    public static string? CertDir => Environment.GetEnvironmentVariable("SSL_CERT_DIR");
}
