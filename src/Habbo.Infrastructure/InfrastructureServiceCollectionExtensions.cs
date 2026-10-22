using Habbo.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Habbo.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers persistence: the EF Core context factory and password hashing.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'.");

        services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        return services;
    }
}
