using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Habbo.Infrastructure;

/// <summary>Lets <c>dotnet ef</c> create contexts without a running host. Connection string via <c>HABBO_CONNECTIONSTRING</c>.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HABBO_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=habbo;Username=habbo;Password=habbo";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
