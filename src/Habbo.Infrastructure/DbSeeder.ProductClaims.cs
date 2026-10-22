using Habbo.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>The products players may claim once for free.</summary>
public static partial class DbSeeder
{
    private sealed record SeedProductClaims(List<SeedProductClaim>? Claims);

    private sealed record SeedProductClaim(string Id, bool Active, List<string>? Furni);

    /// <summary>seed/product_claims.json. A claim the hotel already has keeps whatever it was changed to.</summary>
    private static async Task SeedProductClaimsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        var file = await seedFiles.ReadJsonAsync<SeedProductClaims>("product_claims.json", ct);
        var known = await db.ProductClaims.Select(c => c.Id).ToListAsync(ct);
        var added = 0;
        foreach (var seed in (file?.Claims ?? []).Where(c => !known.Contains(c.Id)))
        {
            db.ProductClaims.Add(new ProductClaim { Id = seed.Id, Active = seed.Active, Furni = seed.Furni ?? [] });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} product claim(s) from {File}.", added, "product_claims.json");
        }
    }
}
