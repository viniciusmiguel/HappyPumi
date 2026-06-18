#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HappyPumi.Api.Data;

/// <summary>
/// Design-time factory used only by the EF tooling (<c>dotnet ef migrations</c>) to construct the context
/// without booting the app. The connection string here is a placeholder — migrations are generated from the
/// model, not a live database.
/// </summary>
public sealed class HappyPumiDbContextFactory : IDesignTimeDbContextFactory<HappyPumiDbContext>
{
    public HappyPumiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HappyPumiDbContext>()
            .UseNpgsql("Host=localhost;Database=happypumidb;Username=postgres;Password=postgres")
            .Options;
        return new HappyPumiDbContext(options);
    }
}
