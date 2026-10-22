using Habbo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>What each rank may do.</summary>
public static partial class DbSeeder
{
    /// <summary>One row of seed/permissions.json.</summary>
    private sealed record SeedPermission(string Name, SecurityLevel MinimumRank, string Description);

    /// <summary>seed/permissions.json. A permission the hotel already has keeps the rank it was given, but takes the
    /// seed's description, which says what the code does; a new one comes in at the seed's rank.</summary>
    private static async Task SeedPermissionsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "permissions.json";
        var permissions = await seedFiles.ReadJsonAsync<List<SeedPermission>>(file, ct);
        if (permissions is null)
            return;

        var known = await db.Permissions.ToDictionaryAsync(p => p.Name, ct);
        var added = 0;
        var described = 0;
        foreach (var permission in permissions)
        {
            if (known.TryGetValue(permission.Name, out var existing))
            {
                if (existing.Description != permission.Description)
                {
                    existing.Description = permission.Description;
                    described++;
                }

                continue;
            }

            db.Permissions.Add(new Permission
            {
                Name = permission.Name, MinimumRank = permission.MinimumRank, Description = permission.Description,
            });
            added++;
        }

        if (added > 0 || described > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Added} permission(s) and refreshed {Described} description(s) from {File}.", added, described, file);
        }
    }
}
