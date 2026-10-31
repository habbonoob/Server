using Habbo.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Infrastructure;

/// <summary>What the shop's Builders Club memberships hand over. Habbo's catalog lists them with no products (the
/// client knows them by their code), so the dump seeds them empty and the membership each one is comes from here.
/// </summary>
public static partial class DbSeeder
{
    /// <summary>A membership offer (its days and tier), or an upgrade (the furni it adds to the limit).</summary>
    private sealed record SeedBuildersClubMembership(string ProductCode, int Days, string? Tier, int Furni);

    /// <summary>seed/builders_club_memberships.json. An offer is matched by its code, and one that already hands over
    /// a membership keeps whatever it was changed to.</summary>
    private static async Task SeedBuildersClubMembershipsAsync(AppDbContext db, SeedFiles seedFiles, ILogger logger, CancellationToken ct)
    {
        const string file = "builders_club_memberships.json";
        var memberships = await seedFiles.ReadJsonAsync<List<SeedBuildersClubMembership>>(file, ct);
        if (memberships is null || memberships.Count == 0)
            return;

        var codes = memberships.Select(m => m.ProductCode).ToList();
        var offers = await db.CatalogOffers.Include(o => o.Products)
            .Where(o => codes.Contains(o.LocalizationId) && !o.Listings.Any(l => l.Page!.BuildersClub))
            .ToListAsync(ct);
        var added = 0;

        foreach (var offer in offers.Where(o => o.Products.All(p => p.Type != CatalogProductType.BuildersClub)))
        {
            var membership = memberships.First(m => m.ProductCode == offer.LocalizationId);
            offer.Products.Add(membership.Furni > 0
                ? new CatalogProduct { Type = CatalogProductType.BuildersClub, ExtraParam = Habbo.Domain.Subscriptions.SubscriptionProducts.BuildersClubAddon, Amount = membership.Furni }
                : new CatalogProduct
                {
                    Type = CatalogProductType.BuildersClub, ExtraParam = membership.Tier ?? string.Empty, DurationSeconds = membership.Days * SecondsPerDay,
                });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} Builders Club membership(s) from {File}.", added, file);
        }
    }
}
